using MediatR;
using Wargame.Application.Interfaces.Repositories;

namespace Wargame.Application.Queries.GameMatch;

/// <summary>
/// Requête pour récupérer l'état complet d'une partie (Query).
/// </summary>
public record GetGameMatchQuery(Guid GameMatchId) : IRequest<GameMatchResponse?>;

public class GetGameMatchQueryHandler : IRequestHandler<GetGameMatchQuery, GameMatchResponse?>
{
    private readonly IGameMatchRepository _repository;

    public GetGameMatchQueryHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<GameMatchResponse?> Handle(GetGameMatchQuery request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null) return null;

        return new GameMatchResponse(
            match.Id,
            match.Status.ToString(),
            match.CurrentTurn,
            match.ActivePlayerId,
            match.Players.Select(p => new PlayerDto(
                p.Id,
                p.Name,
                p.VictoryPoints,
                p.UnitIds.ToList()
            )).ToList(),
            match.Units.Select(u => new UnitDto(
                u.Id,
                u.Name,
                u.Type.ToString(),
                u.LifecycleStatus.ToString(),
                u.ActivationStatus.ToString(),
                u.GetAliveCount(),
                u.HasFired,
                u.HasCharged
            )).ToList()
        );
    }
}
