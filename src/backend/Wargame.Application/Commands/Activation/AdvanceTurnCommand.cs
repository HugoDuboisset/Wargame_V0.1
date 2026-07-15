using FluentValidation;
using MediatR;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Enums;

namespace Wargame.Application.Commands.Activation;

/// <summary>
/// Commande pour clôturer le tour en cours et passer au suivant.
/// </summary>
public record AdvanceTurnCommand(Guid GameMatchId) : IRequest;

public class AdvanceTurnCommandValidator : AbstractValidator<AdvanceTurnCommand>
{
    public AdvanceTurnCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
    }
}

public class AdvanceTurnCommandHandler : IRequestHandler<AdvanceTurnCommand>
{
    private readonly IGameMatchRepository _repository;

    public AdvanceTurnCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(AdvanceTurnCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        match.AdvanceToNextTurn();

        await _repository.SaveAsync(match, cancellationToken);
    }
}
