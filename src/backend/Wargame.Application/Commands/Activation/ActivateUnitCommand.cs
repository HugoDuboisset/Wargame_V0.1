using FluentValidation;
using MediatR;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Enums;

namespace Wargame.Application.Commands.Activation;

/// <summary>
/// Commande pour marquer une unité comme active pendant le tour.
/// </summary>
public record ActivateUnitCommand(Guid GameMatchId, Guid UnitId) : IRequest;

public class ActivateUnitCommandValidator : AbstractValidator<ActivateUnitCommand>
{
    public ActivateUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
    }
}

public class ActivateUnitCommandHandler : IRequestHandler<ActivateUnitCommand>
{
    private readonly IGameMatchRepository _repository;

    public ActivateUnitCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ActivateUnitCommand request, CancellationToken cancellationToken)
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

        if (unit.ActivationStatus != ActivationStatus.Waiting)
            throw new InvalidOperationException("L'unité a déjà été activée ce tour.");

        // On vérifie que c'est bien à un joueur de jouer, 
        // et qu'il possède bien cette unité (logique à enrichir selon le besoin métier exact)
        if (match.ActivePlayerId != null)
        {
            var activePlayer = match.Players.FirstOrDefault(p => p.Id == match.ActivePlayerId);
            if (activePlayer != null && !activePlayer.UnitIds.Contains(unit.Id))
            {
                throw new InvalidOperationException("L'unité n'appartient pas au joueur actif.");
            }
        }

        unit.SetActivationStatus(ActivationStatus.Active);

        await _repository.SaveAsync(match, cancellationToken);
    }
}
