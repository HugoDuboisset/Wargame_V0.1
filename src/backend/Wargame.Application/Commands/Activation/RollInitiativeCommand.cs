using FluentValidation;
using MediatR;
using Wargame.Application.Interfaces.Repositories;

namespace Wargame.Application.Commands.Activation;

/// <summary>
/// Commande pour lancer l'initiative et déterminer le premier joueur du tour.
/// </summary>
public record RollInitiativeCommand(Guid GameMatchId) : IRequest;

public class RollInitiativeCommandValidator : AbstractValidator<RollInitiativeCommand>
{
    public RollInitiativeCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty().WithMessage("L'ID de la partie est requis.");
    }
}

public class RollInitiativeCommandHandler : IRequestHandler<RollInitiativeCommand>
{
    private readonly IGameMatchRepository _repository;

    public RollInitiativeCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(RollInitiativeCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException($"Partie introuvable (ID: {request.GameMatchId})."); // TODO: Utiliser un Result pattern au lieu de l'exception métier

        // Désigne le premier joueur (aléatoirement selon les règles actuelles de GameMatch)
        match.DetermineFirstPlayer();

        await _repository.SaveAsync(match, cancellationToken);
    }
}
