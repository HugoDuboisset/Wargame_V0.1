using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Assault.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Application.Commands.Assault;

/// <summary>
/// Commande pour la consolidation post-charge (Phase d'Assaut, après le déplacement de charge).
/// Chaque figurine peut se déplacer de 2" maximum pour maximiser les contacts socle-à-socle.
/// 
/// Cette commande est déclenchée par le joueur (bouton de consolidation automatique,
/// ou en laissant le joueur déplacer manuellement ses figurines de 2" sur l'interface).
/// 
/// La consolidation applique l'algorithme automatique : figurine par figurine, sans superposition.
/// </summary>
public record ConsolidateChargeCommand(
    Guid GameMatchId,
    Guid ChargingUnitId,
    Guid TargetUnitId
) : IRequest<ConsolidateChargeResultDto>;

public class ConsolidateChargeCommandValidator : AbstractValidator<ConsolidateChargeCommand>
{
    public ConsolidateChargeCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.ChargingUnitId).NotEmpty();
        RuleFor(x => x.TargetUnitId).NotEmpty();
    }
}

public class ConsolidateChargeCommandHandler : IRequestHandler<ConsolidateChargeCommand, ConsolidateChargeResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly AssaultMovementService _movementService;

    public ConsolidateChargeCommandHandler(
        IGameMatchRepository repository,
        AssaultMovementService movementService)
    {
        _repository = repository;
        _movementService = movementService;
    }

    public async Task<ConsolidateChargeResultDto> Handle(
        ConsolidateChargeCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var chargingUnit = match.Units.FirstOrDefault(u => u.Id == request.ChargingUnitId);
        if (chargingUnit == null)
            throw new InvalidOperationException("Unité chargeante introuvable.");

        var targetUnit = match.Units.FirstOrDefault(u => u.Id == request.TargetUnitId);
        if (targetUnit == null)
            throw new InvalidOperationException("Unité cible introuvable.");

        // Vérifier que les deux unités sont bien engagées l'une avec l'autre
        if (!chargingUnit.EngagedWithUnitIds.Contains(targetUnit.Id))
            throw new InvalidOperationException("L'unité chargeante n'est pas engagée avec la cible.");

        // Vérifier que l'unité a bien chargé ce tour (la consolidation ne suit qu'une charge)
        if (!chargingUnit.HasCharged)
            throw new InvalidOperationException("La consolidation ne peut avoir lieu qu'après une charge ce tour.");

        // Calculer les positions de consolidation (2" max, figurine par figurine, sans superposition)
        var positions = _movementService.CalculateConsolidationPositions(
            chargingUnit.Figures, targetUnit.Figures);

        int figuresMoved = 0;
        foreach (var (figure, newPosition) in positions)
        {
            // Ne compter comme "bougé" que si la position a réellement changé
            if (figure.Position.DistanceTo(newPosition) > 0.001)
            {
                figure.MoveTo(newPosition);
                figuresMoved++;
            }
        }

        await _repository.SaveAsync(match, cancellationToken);

        return new ConsolidateChargeResultDto(FiguresMoved: figuresMoved);
    }
}
