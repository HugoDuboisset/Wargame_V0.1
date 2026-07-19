using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

namespace Wargame.Application.Commands.Movement;

/// <summary>
/// Commande pour désengager volontairement une unité.
/// Requiert des positions cibles valides pour chaque figurine.
/// </summary>
public record DisengageUnitCommand(
    Guid GameMatchId,
    Guid UnitId,
    List<FigureMoveDto> FigureMoves
) : IRequest<DisengageResultDto>;

public class DisengageUnitCommandValidator : AbstractValidator<DisengageUnitCommand>
{
    public DisengageUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.FigureMoves).NotEmpty().WithMessage("Pour un désengagement volontaire, les coordonnées cibles doivent être fournies.");
    }
}

public class DisengageUnitCommandHandler : IRequestHandler<DisengageUnitCommand, DisengageResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly WithdrawalResolutionService _withdrawalResolutionService;
    private readonly UnitCohesionService _cohesionService;

    public DisengageUnitCommandHandler(
        IGameMatchRepository repository,
        WithdrawalResolutionService withdrawalResolutionService,
        UnitCohesionService cohesionService)
    {
        _repository = repository;
        _withdrawalResolutionService = withdrawalResolutionService;
        _cohesionService = cohesionService;
    }

    public async Task<DisengageResultDto> Handle(DisengageUnitCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var unit = match.Units.FirstOrDefault(u => u.Id == request.UnitId);
        if (unit == null)
            throw new InvalidOperationException("Unité introuvable dans cette partie.");

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        if (!unit.IsEngaged())
            throw new InvalidOperationException("L'unité n'est pas engagée, utilisez un mouvement normal.");

        // 1. Détermination du mouvement physique à réaliser
        double m = unit.GetEffectiveMovement();
        var figureMoves = new List<FigureMove>();

        foreach (var dto in request.FigureMoves)
        {
            var figure = unit.Figures.FirstOrDefault(f => f.Id == dto.FigureId);
            if (figure == null)
                throw new InvalidOperationException($"La figurine {dto.FigureId} n'appartient pas à cette unité.");

            var newPos = new Position(dto.X, dto.Y);
            var dist = figure.GetEdgeDistanceToPosition(newPos, figure.BaseShape, figure.OrientationDegrees);
            
            if (dist > m)
                throw new InvalidOperationException($"La figurine {figure.Id} dépasse la distance maximale de {m}\".");

            figureMoves.Add(new FigureMove(dto.FigureId, newPos));
        }

        // Validation de la cohésion avant de subir les éventuelles attaques d'opportunité
        var cohesionErrors = unit.ValidateCohesion(figureMoves, _cohesionService);
        if (cohesionErrors.Any())
            throw new InvalidOperationException($"Le désengagement brise la cohésion de l'unité : {string.Join(" | ", cohesionErrors)}");

        // 2. Test d'action risquée et attaques d'opportunité via le Domain Service
        var withdrawalResult = _withdrawalResolutionService.ResolveWithdrawal(unit, match);

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
        {
            // L'unité a été entièrement détruite par les attaques d'opportunité
            await _repository.SaveAsync(match, cancellationToken);
            return new DisengageResultDto(false, withdrawalResult.RiskyActionFailed, withdrawalResult.OpportunityWoundsLost, withdrawalResult.OpportunityFiguresLost, false);
        }

        // On nettoie la liste des mouvements au cas où des figurines seraient mortes lors de l'opportunité
        figureMoves = figureMoves.Where(fm => unit.Figures.Any(f => f.Id == fm.FigureId && f.IsAlive)).ToList();

        // 3. Effectuer le mouvement physique
        unit.Move(figureMoves, MovementType.Normal);

        await _repository.SaveAsync(match, cancellationToken);

        return new DisengageResultDto(false, withdrawalResult.RiskyActionFailed, withdrawalResult.OpportunityWoundsLost, withdrawalResult.OpportunityFiguresLost, false);
    }
}
