using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using DomainUnit = Wargame.Domain.Entities.Unit;

namespace Wargame.Application.Commands.Movement;

/// <summary>
/// Commande pour fuir (flee) lorsqu'une unité a raté son test de moral ou est démoralisée.
/// Le mouvement est automatique vers le bord de table le plus proche.
/// </summary>
public record FleeUnitCommand(
    Guid GameMatchId,
    Guid UnitId
) : IRequest<FleeResultDto>;

public class FleeUnitCommandValidator : AbstractValidator<FleeUnitCommand>
{
    public FleeUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
    }
}

public class FleeUnitCommandHandler : IRequestHandler<FleeUnitCommand, FleeResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly WithdrawalResolutionService _withdrawalResolutionService;

    public FleeUnitCommandHandler(
        IGameMatchRepository repository,
        WithdrawalResolutionService withdrawalResolutionService)
    {
        _repository = repository;
        _withdrawalResolutionService = withdrawalResolutionService;
    }

    public async Task<FleeResultDto> Handle(FleeUnitCommand request, CancellationToken cancellationToken)
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
            throw new InvalidOperationException("L'unité n'est pas engagée.");

        if (!unit.ActiveStatusEffects.HasFlag(StatusEffect.Routing))
            throw new InvalidOperationException("L'unité n'est pas en fuite (Routing).");

        // 1. Détermination du mouvement physique à réaliser
        double m = unit.GetEffectiveMovement();
        var figureMoves = CalculateFleeMoves(unit, match.Board, m);

        // 2. Test d'action risquée et attaques d'opportunité via le Domain Service
        var withdrawalResult = _withdrawalResolutionService.ResolveWithdrawal(unit, match);

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
        {
            // L'unité a été entièrement détruite par les attaques d'opportunité
            await _repository.SaveAsync(match, cancellationToken);
            return new FleeResultDto(withdrawalResult.RiskyActionFailed, withdrawalResult.OpportunityWoundsLost, withdrawalResult.OpportunityFiguresLost, false);
        }

        // On nettoie la liste des mouvements au cas où des figurines seraient mortes lors de l'opportunité
        figureMoves = figureMoves.Where(fm => unit.Figures.Any(f => f.Id == fm.FigureId && f.IsAlive)).ToList();

        // 3. Effectuer le mouvement physique
        bool destroyedByFleeing = false;
        
        // Vérifier si n'importe quelle figurine sort du plateau
        foreach (var fm in figureMoves)
        {
            if (!match.Board.IsWithinBounds(fm.NewPosition))
            {
                destroyedByFleeing = true;
                break;
            }
        }

        if (destroyedByFleeing)
        {
            unit.Destroy(); // Sortie de table = détruite
        }
        else
        {
            unit.Move(figureMoves, MovementType.Normal);
        }

        await _repository.SaveAsync(match, cancellationToken);

        return new FleeResultDto(withdrawalResult.RiskyActionFailed, withdrawalResult.OpportunityWoundsLost, withdrawalResult.OpportunityFiguresLost, destroyedByFleeing);
    }

    private List<FigureMove> CalculateFleeMoves(DomainUnit unit, Board board, double movement)
    {
        var aliveFigures = unit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return new List<FigureMove>();

        double minX = aliveFigures.Min(f => f.Position.X);
        double maxX = aliveFigures.Max(f => f.Position.X);
        double minY = aliveFigures.Min(f => f.Position.Y);
        double maxY = aliveFigures.Max(f => f.Position.Y);

        double distLeft = minX;
        double distRight = board.Width - maxX;
        double distBottom = minY;
        double distTop = board.Height - maxY;

        double minDist = Math.Min(Math.Min(distLeft, distRight), Math.Min(distBottom, distTop));

        double dirX, dirY;
        if (Math.Abs(minDist - distLeft) < 0.01) { dirX = -1; dirY = 0; }
        else if (Math.Abs(minDist - distRight) < 0.01) { dirX = 1; dirY = 0; }
        else if (Math.Abs(minDist - distBottom) < 0.01) { dirX = 0; dirY = -1; }
        else { dirX = 0; dirY = 1; }

        var moves = new List<FigureMove>();
        foreach (var f in aliveFigures)
        {
            var newPos = new Position(
                f.Position.X + dirX * movement,
                f.Position.Y + dirY * movement
            );
            moves.Add(new FigureMove(f.Id, newPos));
        }

        return moves;
    }
}
