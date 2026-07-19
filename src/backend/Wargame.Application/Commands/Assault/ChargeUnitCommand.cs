using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Assault.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Application.Commands.Assault;

/// <summary>
/// Commande pour déclarer une charge (Phase d'Assaut).
/// L'unité lance 1D6 + Mouvement. Si la distance permet d'atteindre le contact
/// socle-à-socle, la charge réussit. Sinon, l'unité ne se déplace pas.
/// </summary>
public record ChargeUnitCommand(
    Guid GameMatchId,
    Guid ChargingUnitId,
    Guid TargetUnitId
) : IRequest<ChargeResultDto>;

public class ChargeUnitCommandValidator : AbstractValidator<ChargeUnitCommand>
{
    public ChargeUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.ChargingUnitId).NotEmpty();
        RuleFor(x => x.TargetUnitId).NotEmpty();
    }
}

public class ChargeUnitCommandHandler : IRequestHandler<ChargeUnitCommand, ChargeResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly AssaultValidationService _validationService;
    private readonly IDiceRoller _diceRoller;

    public ChargeUnitCommandHandler(
        IGameMatchRepository repository,
        AssaultValidationService validationService,
        IDiceRoller diceRoller)
    {
        _repository = repository;
        _validationService = validationService;
        _diceRoller = diceRoller;
    }

    public async Task<ChargeResultDto> Handle(ChargeUnitCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var chargingUnit = match.Units.FirstOrDefault(u => u.Id == request.ChargingUnitId);
        if (chargingUnit == null)
            throw new InvalidOperationException("Unité chargeante introuvable.");

        if (chargingUnit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        var targetUnit = match.Units.FirstOrDefault(u => u.Id == request.TargetUnitId);
        if (targetUnit == null)
            throw new InvalidOperationException("Unité cible introuvable.");

        // Validation des règles de charge
        _validationService.ValidateCharge(chargingUnit, targetUnit);

        // 1. Jet de charge : 1D6 + Mouvement de base
        int chargeRoll = _diceRoller.RollD6();
        double chargeDistance = chargeRoll + chargingUnit.BaseProfile.Movement;

        // 2. La charge est réussie si au moins une figurine peut atteindre le contact socle-à-socle
        bool chargeSucceeded = CanReachContact(chargingUnit, targetUnit, chargeDistance);

        if (!chargeSucceeded)
        {
            // Charge échouée : l'unité ne se déplace pas
            await _repository.SaveAsync(match, cancellationToken);
            return new ChargeResultDto(
                ChargingSucceeded: false,
                ChargeRoll: chargeRoll,
                ChargeDistance: chargeDistance
            );
        }

        // 3. Charge réussie : déplacement des figurines
        MoveFiguresTowardTarget(chargingUnit, targetUnit, chargeDistance);

        // 4. Engagement mutuel
        chargingUnit.EngageWith(targetUnit.Id);
        targetUnit.EngageWith(chargingUnit.Id);

        // 5. Statut Charging (pour le bonus d'initiative en phase de mêlée)
        chargingUnit.ApplyStatusEffect(StatusEffect.Charging);
        chargingUnit.RegisterCharged();

        await _repository.SaveAsync(match, cancellationToken);

        return new ChargeResultDto(
            ChargingSucceeded: true,
            ChargeRoll: chargeRoll,
            ChargeDistance: chargeDistance
        );
    }

    /// <summary>
    /// Vérifie si au moins une figurine de l'unité chargeante peut atteindre le contact
    /// socle-à-socle avec au moins une figurine de l'unité cible, en se déplaçant de chargeDistance.
    /// </summary>
    private static bool CanReachContact(Wargame.Domain.Entities.Unit chargingUnit, Wargame.Domain.Entities.Unit targetUnit, double chargeDistance)
    {
        var aliveFigures = chargingUnit.Figures.Where(f => f.IsAlive).ToList();
        var aliveTargets = targetUnit.Figures.Where(f => f.IsAlive).ToList();

        return aliveFigures.Any(attacker =>
            aliveTargets.Any(target =>
                attacker.GetEdgeDistanceTo(target) <= chargeDistance
            )
        );
    }

    /// <summary>
    /// Déplace toutes les figurines de l'unité chargeante vers la cible, dans la limite de chargeDistance.
    /// Chaque figurine se déplace au maximum de chargeDistance, en visant la figurine cible la plus proche.
    /// Note : la consolidation (2" supplémentaire pour maximiser les contacts) est simplifiée ici.
    /// </summary>
    private static void MoveFiguresTowardTarget(Wargame.Domain.Entities.Unit chargingUnit, Wargame.Domain.Entities.Unit targetUnit, double chargeDistance)
    {
        var aliveTargets = targetUnit.Figures.Where(f => f.IsAlive).ToList();

        foreach (var attacker in chargingUnit.Figures.Where(f => f.IsAlive))
        {
            // Trouver la figurine cible la plus proche
            var closestTarget = aliveTargets
                .OrderBy(t => attacker.GetEdgeDistanceTo(t))
                .FirstOrDefault();

            if (closestTarget == null) continue;

            double distance = attacker.GetEdgeDistanceTo(closestTarget);
            if (distance > chargeDistance) continue;

            // Calculer la nouvelle position : se rapprocher le plus possible dans la direction de la cible
            double dx = closestTarget.Position.X - attacker.Position.X;
            double dy = closestTarget.Position.Y - attacker.Position.Y;
            double totalDist = Math.Sqrt(dx * dx + dy * dy);

            if (totalDist <= 0) continue;

            // Déplacement = distance vers le contact (bord à bord), capped par chargeDistance
            // On ne dépasse pas le point de contact (socle-à-socle)
            double moveAmount = Math.Min(distance, chargeDistance);
            double ratio = moveAmount / totalDist;

            double newX = attacker.Position.X + dx * ratio;
            double newY = attacker.Position.Y + dy * ratio;

            attacker.MoveTo(new Domain.ValueObjects.Position(newX, newY));
        }
    }
}
