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
/// socle-à-socle, la charge réussit. Les figurines se déplacent figurine par figurine,
/// sans superposition. La consolidation (2") est une action séparée.
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
    private readonly AssaultMovementService _movementService;
    private readonly IDiceRoller _diceRoller;
    private readonly UnitCohesionService _cohesionService;

    public ChargeUnitCommandHandler(
        IGameMatchRepository repository,
        AssaultValidationService validationService,
        AssaultMovementService movementService,
        IDiceRoller diceRoller,
        UnitCohesionService cohesionService)
    {
        _repository = repository;
        _validationService = validationService;
        _movementService = movementService;
        _diceRoller = diceRoller;
        _cohesionService = cohesionService;
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

        // 3. Charge réussie : déplacement figurine par figurine (sans superposition)
        var positions = _movementService.CalculateChargePositions(
            chargingUnit.Figures, targetUnit.Figures, chargeDistance);

        var simulatedDict = positions.ToDictionary(k => k.Figure.Id, v => v.NewPosition);
        if (!_cohesionService.IsInCohesion(chargingUnit.Figures.Where(f => f.IsAlive).ToList(), simulatedDict))
        {
            throw new InvalidOperationException("La charge est impossible car le mouvement briserait la cohésion de l'unité.");
        }

        foreach (var (figure, newPosition) in positions)
            figure.MoveTo(newPosition);

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

    private static bool CanReachContact(
        Wargame.Domain.Entities.Unit chargingUnit,
        Wargame.Domain.Entities.Unit targetUnit,
        double chargeDistance)
    {
        var aliveFigures = chargingUnit.Figures.Where(f => f.IsAlive).ToList();
        var aliveTargets = targetUnit.Figures.Where(f => f.IsAlive).ToList();

        return aliveFigures.Any(attacker =>
            aliveTargets.Any(target =>
                attacker.GetEdgeDistanceTo(target) <= chargeDistance
            )
        );
    }
}
