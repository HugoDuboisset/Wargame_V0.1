using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Shooting.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Application.Commands.Shooting;

/// <summary>
/// Commande pour résoudre la phase de tir d'une unité contre une ou plusieurs unités cibles.
/// Chaque figurine choisit son arme et sa cible via FigureShootDto.
/// 
/// NOTE : La vérification de Ligne de Vue (LoS) est temporairement simplifiée (toujours visible).
/// Elle sera ajoutée dans un commit dédié après l'ajout des dimensions aux terrains.
/// </summary>
public record ShootUnitCommand(
    Guid GameMatchId,
    Guid ShootingUnitId,
    List<FigureShootDto> FigureShots
) : IRequest<ShootingResultDto>;

public class ShootUnitCommandValidator : AbstractValidator<ShootUnitCommand>
{
    public ShootUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.ShootingUnitId).NotEmpty();
        RuleFor(x => x.FigureShots).NotNull().NotEmpty()
            .WithMessage("Au moins une figurine doit participer au tir.");
    }
}

public class ShootUnitCommandHandler : IRequestHandler<ShootUnitCommand, ShootingResultDto>
{
    private readonly IGameMatchRepository _repository;

    public ShootUnitCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<ShootingResultDto> Handle(ShootUnitCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var shootingUnit = match.Units.FirstOrDefault(u => u.Id == request.ShootingUnitId);
        if (shootingUnit == null)
            throw new InvalidOperationException("Unité tireur introuvable.");

        if (shootingUnit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        if (shootingUnit.HasFired)
            throw new InvalidOperationException("L'unité a déjà tiré ce tour.");

        var opaqueTerrains = match.Board.Terrains.Where(t => t.IsOpaque).ToList();

        // Validation de chaque tir de figurine
        foreach (var shot in request.FigureShots)
        {
            var figure = shootingUnit.Figures.FirstOrDefault(f => f.Id == shot.FigureId);
            if (figure == null)
                throw new InvalidOperationException($"La figurine {shot.FigureId} n'appartient pas à l'unité tireur.");

            if (!figure.IsAlive)
                throw new InvalidOperationException($"La figurine {shot.FigureId} est hors de combat.");

            var weapon = figure.RangedWeapons.FirstOrDefault(w => w.Id == shot.WeaponId);
            if (weapon == null)
                throw new InvalidOperationException($"L'arme {shot.WeaponId} n'est pas une arme à distance portée par la figurine {shot.FigureId}.");

            var targetUnit = match.Units.FirstOrDefault(u => u.Id == shot.TargetUnitId);
            if (targetUnit == null)
                throw new InvalidOperationException($"L'unité cible {shot.TargetUnitId} est introuvable.");

            if (targetUnit.Id == shootingUnit.Id)
                throw new InvalidOperationException("Une unité ne peut pas se tirer dessus.");

            if (targetUnit.LifecycleStatus != UnitLifecycleStatus.Alive)
                throw new InvalidOperationException("L'unité cible est déjà hors de combat.");

            // Vérification : tir en étant engagé
            ValidateEngagementConstraints(shootingUnit, targetUnit, weapon);

            // Vérification : contraintes de mouvement sur le choix d'arme
            ValidateMovementConstraints(shootingUnit, weapon);

            // Vérification de la portée (distance bord à bord minimum entre les deux unités)
            ValidateRange(figure, shootingUnit, targetUnit, weapon);

            // Vérification de la Ligne de Vue (LoS)
            ValidateLineOfSight(figure, targetUnit, weapon, opaqueTerrains);
        }

        // Pour l'infanterie : une seule arme par figurine (déjà garanti par le DTO)
        // Pour les véhicules : peuvent tirer avec toutes leurs armes (pas de contrainte supplémentaire)

        // TODO Commit 3 : résoudre les jets pour Toucher
        // TODO Commit 4 : résoudre les jets de Blessure et retirer les pertes
        // TODO Commit 5 : déclencher le test de Moral si nécessaire

        shootingUnit.RegisterFired();
        await _repository.SaveAsync(match, cancellationToken);

        // Résultat provisoire (sera enrichi dans les commits suivants)
        return new ShootingResultDto(0, 0, 0, false, false, false);
    }

    // =====================================================================
    //  VALIDATIONS PRIVÉES
    // =====================================================================

    private static void ValidateEngagementConstraints(Wargame.Domain.Entities.Unit shootingUnit, Wargame.Domain.Entities.Unit targetUnit, Weapon weapon)
    {
        if (!shootingUnit.IsEngaged()) return;

        bool isPistol = weapon.HasTrait(WeaponTrait.Pistol);
        bool isVehicle = shootingUnit.Type == UnitType.Vehicle;

        if (isVehicle) return; // Les véhicules peuvent toujours tirer, même engagés

        if (!isPistol)
            throw new InvalidOperationException(
                "L'unité est engagée au corps à corps. Seules les armes avec le trait Pistolet peuvent tirer dans cet état.");

        // Arme Pistol : peut uniquement cibler l'unité avec laquelle elle est engagée
        bool targetIsEngaged = shootingUnit.EngagedWithUnitIds.Contains(targetUnit.Id);
        if (!targetIsEngaged)
            throw new InvalidOperationException(
                "Une arme Pistolet engagée au corps à corps ne peut cibler que l'unité avec laquelle elle est engagée.");
    }

    private static void ValidateMovementConstraints(Wargame.Domain.Entities.Unit shootingUnit, Weapon weapon)
    {
        bool isVehicle = shootingUnit.Type == UnitType.Vehicle;
        if (isVehicle) return; // Les véhicules ignorent les contraintes de mouvement sur les armes

        switch (shootingUnit.MovementThisTurn)
        {
            case MovementType.Sprint:
                if (!weapon.HasTrait(WeaponTrait.Handy))
                    throw new InvalidOperationException(
                        $"L'unité a effectué un sprint. Seules les armes Maniables peuvent tirer. L'arme '{weapon.Name}' n'a pas ce trait.");
                break;

            case MovementType.Normal:
                if (weapon.HasTrait(WeaponTrait.Cumbersome))
                    throw new InvalidOperationException(
                        $"L'arme '{weapon.Name}' est Encombrante et nécessite que l'unité soit restée Immobile pour tirer.");
                break;

            case MovementType.None:
                // Immobile : toutes les armes peuvent tirer, pas de restriction
                break;
        }
    }

    private static void ValidateRange(Figure shootingFigure, Wargame.Domain.Entities.Unit shootingUnit, Wargame.Domain.Entities.Unit targetUnit, Weapon weapon)
    {
        // La portée est vérifiée de la figurine tireur à la figurine cible la plus proche (bord à bord)
        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        double minDistance = aliveFigures
            .Select(targetFig => shootingFigure.GetEdgeDistanceTo(targetFig))
            .Min();

        if (minDistance > weapon.Profile.Range)
            throw new InvalidOperationException(
                $"La cible est hors de portée. Distance bord à bord : {minDistance:F2}\", portée maximale de l'arme '{weapon.Name}' : {weapon.Profile.Range}\".");
    }

    private static void ValidateLineOfSight(Figure shootingFigure, Wargame.Domain.Entities.Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        if (weapon.HasTrait(WeaponTrait.IndirectFire))
            return; // Les armes à tir indirect ignorent la ligne de vue

        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        // Le tir est possible si AU MOINS UNE figurine de l'unité cible est visible par le tireur
        bool isAnyVisible = aliveFigures.Any(targetFig => LineOfSightService.IsVisible(shootingFigure, targetFig, opaqueTerrains));

        if (!isAnyVisible)
        {
            throw new InvalidOperationException(
                $"Aucune figurine de l'unité cible n'est en ligne de vue de la figurine tireur (arme '{weapon.Name}' ne possède pas IndirectFire).");
        }
    }
}
