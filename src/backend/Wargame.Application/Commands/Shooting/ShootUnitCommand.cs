using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Shooting.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

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
    private readonly ShootingResolutionService _shootingResolutionService;
    private readonly ShootingValidationService _shootingValidationService;
    private readonly DamageResolutionService _damageResolutionService;
    private readonly MoraleResolutionService _moraleResolutionService;

    public ShootUnitCommandHandler(
        IGameMatchRepository repository, 
        ShootingResolutionService shootingResolutionService,
        ShootingValidationService shootingValidationService,
        DamageResolutionService damageResolutionService,
        MoraleResolutionService moraleResolutionService)
    {
        _repository = repository;
        _shootingResolutionService = shootingResolutionService;
        _shootingValidationService = shootingValidationService;
        _damageResolutionService = damageResolutionService;
        _moraleResolutionService = moraleResolutionService;
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
        var hitsByTarget = new Dictionary<Wargame.Domain.Entities.Unit, List<Hit>>();

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

            // Vérification de la validité du tir (Mouvement, Engagement, Portée, LoS)
            _shootingValidationService.ValidateShot(figure, shootingUnit, targetUnit, weapon, opaqueTerrains);

            // Résolution du tir (génération des touches)
            var hits = _shootingResolutionService.ResolveShot(figure, shootingUnit, targetUnit, weapon, match.Board.Terrains.ToList());
            if (!hitsByTarget.ContainsKey(targetUnit))
                hitsByTarget[targetUnit] = [];
            hitsByTarget[targetUnit].AddRange(hits);
        }

        // Pour l'infanterie : une seule arme par figurine (déjà garanti par le DTO)
        // Pour les véhicules : peuvent tirer avec toutes leurs armes (pas de contrainte supplémentaire)

        int totalHits = 0;
        int totalWounds = 0;
        int figuresDestroyed = 0;
        var targetResults = new List<TargetShootingResultDto>();

        // Résolution des blessures, application des dégâts et tests de moral pour chaque unité cible
        foreach (var kvp in hitsByTarget)
        {
            var targetUnit = kvp.Key;
            var hits = kvp.Value;
            totalHits += hits.Count;

            var (wounds, destroyed) = _damageResolutionService.ResolveWoundsAndApplyDamage(
                hits, shootingUnit, targetUnit, opaqueTerrains);

            totalWounds += wounds;
            figuresDestroyed += destroyed;

            bool moraleTriggered = false;
            bool moralePassed = false;

            // Déclencher un test de moral si l'unité a subi des pertes et est descendue à <= 50% de sa force initiale
            if (destroyed > 0 && targetUnit.HasLostHalfOrMore())
            {
                moraleTriggered = true;
                moralePassed = _moraleResolutionService.ResolveMoraleTest(targetUnit);
            }

            targetResults.Add(new TargetShootingResultDto(
                TargetUnitId: targetUnit.Id,
                Hits: hits.Count,
                Wounds: wounds,
                FiguresDestroyed: destroyed,
                MoraleTestTriggered: moraleTriggered,
                MoraleTestPassed: moralePassed,
                TargetPinnedDown: targetUnit.IsPinnedDown()
            ));
        }

        shootingUnit.RegisterFired();
        await _repository.SaveAsync(match, cancellationToken);

        // Résultat final
        return new ShootingResultDto(
            TotalHits: totalHits, 
            TotalWounds: totalWounds, 
            FiguresDestroyed: figuresDestroyed, 
            TargetResults: targetResults
        );
    }

}
