using FluentValidation;
using MediatR;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;

namespace Wargame.Application.Commands.GameMatch;

/// <summary>
/// Commande pour initialiser une nouvelle partie.
/// </summary>
public record CreateGameMatchCommand(string Player1Name, string Player2Name, double BoardWidth, double BoardHeight) : IRequest<Guid>;

public class CreateGameMatchCommandValidator : AbstractValidator<CreateGameMatchCommand>
{
    public CreateGameMatchCommandValidator()
    {
        RuleFor(x => x.Player1Name).NotEmpty().WithMessage("Le nom du joueur 1 est requis.");
        RuleFor(x => x.Player2Name).NotEmpty().WithMessage("Le nom du joueur 2 est requis.");
        RuleFor(x => x.BoardWidth).GreaterThan(0).WithMessage("La largeur du plateau doit être supérieure à 0.");
        RuleFor(x => x.BoardHeight).GreaterThan(0).WithMessage("La hauteur du plateau doit être supérieure à 0.");
    }
}

public class CreateGameMatchCommandHandler : IRequestHandler<CreateGameMatchCommand, Guid>
{
    private readonly IGameMatchRepository _repository;

    public CreateGameMatchCommandHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateGameMatchCommand request, CancellationToken cancellationToken)
    {
        var player1 = new Player(Guid.NewGuid(), request.Player1Name);
        var player2 = new Player(Guid.NewGuid(), request.Player2Name);
        var board = new Board(Guid.NewGuid(), request.BoardWidth, request.BoardHeight);

        var gameMatch = new Wargame.Domain.Entities.GameMatch(
            Guid.NewGuid(),
            new List<Player> { player1, player2 },
            board
        );

        await _repository.SaveAsync(gameMatch, cancellationToken);

        return gameMatch.Id;
    }
}
