using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Application.Commands.Movement;

/// <summary>
/// Commande pour déplacer les figurines d'une unité. 
/// Le client envoie les positions finales de toutes les figurines déplacées.
/// </summary>
public record MoveUnitCommand(
    Guid GameMatchId,
    Guid UnitId,
    MovementType MovementType,
    List<FigureMoveDto> FigureMoves
) : IRequest;

public class MoveUnitCommandValidator : AbstractValidator<MoveUnitCommand>
{
    public MoveUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.MovementType)
            .Must(t => t == MovementType.Normal || t == MovementType.Sprint)
            .WithMessage("Seuls les types Normal et Sprint sont acceptés via cette commande.");
        RuleFor(x => x.FigureMoves).NotNull().NotEmpty()
            .WithMessage("Au moins une figurine doit être déplacée.");
    }
}

public class MoveUnitCommandHandler : IRequestHandler<MoveUnitCommand>
{
    private readonly IGameMatchRepository _repository;
    private readonly Wargame.Domain.Services.UnitCohesionService _cohesionService;

    public MoveUnitCommandHandler(IGameMatchRepository repository, Wargame.Domain.Services.UnitCohesionService cohesionService)
    {
        _repository = repository;
        _cohesionService = cohesionService;
    }

    public async Task Handle(MoveUnitCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable."); // TODO: Remplacer par Result pattern

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var unit = match.Units.FirstOrDefault(u => u.Id == request.UnitId);
        if (unit == null)
            throw new InvalidOperationException("Unité introuvable dans cette partie.");

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        if (!unit.CanMove())
        {
            if (unit.IsEngaged())
                throw new InvalidOperationException("L'unité est engagée au corps à corps et ne peut pas se déplacer (utilisez le Désengagement).");
            throw new InvalidOperationException("L'unité est clouée au sol et ne peut pas se déplacer.");
        }

        double maxDistance = request.MovementType == MovementType.Sprint
            ? unit.GetEffectiveMovement() * 2
            : unit.GetEffectiveMovement();

        // Construction des déplacements domaine (avec validation distance bord à bord)
        var figureMoves = new List<FigureMove>();

        foreach (var dto in request.FigureMoves)
        {
            var figure = unit.Figures.FirstOrDefault(f => f.Id == dto.FigureId);
            if (figure == null)
                throw new InvalidOperationException($"La figurine {dto.FigureId} n'appartient pas à cette unité.");

            var newPosition = new Position(dto.X, dto.Y);

            // Distance réelle (centre à centre) parcourue
            double moveDistance = figure.Position.DistanceTo(newPosition);
            double totalCost = moveDistance;

            if (figure.BaseShape is Wargame.Domain.ValueObjects.Geometry.Bases.RectangularBase)
            {
                double targetOrientation = dto.TargetOrientationDegrees ?? figure.OrientationDegrees;
                var dx = newPosition.X - figure.Position.X;
                var dy = newPosition.Y - figure.Position.Y;
                
                double movementAngleDegrees = figure.OrientationDegrees;
                if (moveDistance > 0.01) // S'il y a translation réelle
                {
                    movementAngleDegrees = Math.Atan2(dy, dx) * 180 / Math.PI;
                    if (movementAngleDegrees < 0) movementAngleDegrees += 360;
                }
                
                double diffFwd = Math.Abs(NormalizeAngle(movementAngleDegrees - figure.OrientationDegrees));
                double diffRev = Math.Abs(NormalizeAngle(movementAngleDegrees - (figure.OrientationDegrees + 180)));
                
                // Le véhicule s'aligne (soit en marche avant, soit en marche arrière)
                double firstPivotAngle = Math.Min(diffFwd, diffRev); 
                double currentAngleAfterFirstPivot = diffFwd < diffRev ? movementAngleDegrees : movementAngleDegrees + 180;
                
                // Puis s'aligne vers sa rotation cible finale
                double secondPivotAngle = Math.Abs(NormalizeAngle(targetOrientation - currentAngleAfterFirstPivot));
                
                if (moveDistance <= 0.01)
                {
                    // Si aucune translation, c'est un simple pivot de la position de départ à l'arrivée
                    firstPivotAngle = Math.Abs(NormalizeAngle(targetOrientation - figure.OrientationDegrees));
                    secondPivotAngle = 0;
                }

                // Coût : Chaque pivot de 90° (arrondi supérieur) coûte 1/3 du mouvement de base de l'unité
                double baseUnitMovement = unit.BaseProfile.Movement;
                double costPer90 = Math.Ceiling(baseUnitMovement / 3.0);
                
                int chunks1 = (int)Math.Ceiling(firstPivotAngle / 90.0);
                int chunks2 = (int)Math.Ceiling(secondPivotAngle / 90.0);
                
                totalCost = moveDistance + (chunks1 * costPer90) + (chunks2 * costPer90);
            }

            if (totalCost > maxDistance)
                throw new InvalidOperationException(
                    $"La figurine {figure.Id} dépasse la distance maximale : {totalCost:F2}\" coût total, {maxDistance}\" autorisés ({request.MovementType}).");


            // Validation : aucune figurine ennemie à moins de 1" bord à bord
            var enemyFigures = match.Units
                .Where(u => u.Id != unit.Id && u.LifecycleStatus == UnitLifecycleStatus.Alive)
                .SelectMany(u => u.Figures.Where(f => f.IsAlive));

            foreach (var enemyFigure in enemyFigures)
            {
                var distToEnemy = enemyFigure.GetEdgeDistanceToPosition(newPosition, figure.BaseShape, figure.OrientationDegrees);
                if (distToEnemy < 1.0)
                    throw new InvalidOperationException(
                        $"La figurine {figure.Id} se trouve à moins de 1\" d'une figurine ennemie après déplacement. Seule la charge permet d'approcher l'ennemi.");
            }

            figureMoves.Add(new FigureMove(dto.FigureId, newPosition, dto.TargetOrientationDegrees));
        }

        // Validation de la cohésion après déplacement (délégué au domaine)
        var cohesionErrors = unit.ValidateCohesion(figureMoves, _cohesionService);
        if (cohesionErrors.Any())
            throw new InvalidOperationException(
                $"Le déplacement brise la cohésion de l'unité : {string.Join(" | ", cohesionErrors)}");

        // Application du mouvement
        unit.Move(figureMoves, request.MovementType);
        await _repository.SaveAsync(match, cancellationToken);
    }

    private double NormalizeAngle(double a)
    {
        a = a % 360;
        if (a <= -180) a += 360;
        if (a > 180) a -= 360;
        return a;
    }
}
