using FluentValidation;
using MediatR;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Enums;

namespace Wargame.Application.Commands.Movement;

/// <summary>
/// Commande pour déclarer explicitement qu'une unité reste immobile ce tour.
/// Équivalent du bouton "Rester Immobile" côté client.
/// </summary>
public record DeclareStationaryCommand(Guid GameMatchId, Guid UnitId) : IRequest;

public class DeclareStationaryCommandValidator : AbstractValidator<DeclareStationaryCommand>
{
    public DeclareStationaryCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
    }
}

public class DeclareStationaryCommandHandler : IRequestHandler<DeclareStationaryCommand>
{
    private readonly IGameMatchRepository _repository;

    public DeclareStationaryCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeclareStationaryCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        var unit = match.Units.FirstOrDefault(u => u.Id == request.UnitId);
        if (unit == null)
            throw new InvalidOperationException("Unité introuvable dans cette partie.");

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        // Enregistre explicitement l'immobilité (aucun déplacement de figurine)
        unit.SetMovement(MovementType.None);

        await _repository.SaveAsync(match, cancellationToken);
    }
}
