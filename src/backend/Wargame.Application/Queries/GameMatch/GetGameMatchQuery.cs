using MediatR;
using Wargame.Application.Interfaces.Repositories;

namespace Wargame.Application.Queries.GameMatch;

/// <summary>
/// Requête pour récupérer l'état complet d'une partie (Query).
/// </summary>
public record GetGameMatchQuery(Guid GameMatchId) : IRequest<Wargame.Domain.Entities.GameMatch?>;

public class GetGameMatchQueryHandler : IRequestHandler<GetGameMatchQuery, Wargame.Domain.Entities.GameMatch?>
{
    private readonly IGameMatchRepository _repository;

    public GetGameMatchQueryHandler(IGameMatchRepository repository)
    {
        _repository = repository;
    }

    public async Task<Wargame.Domain.Entities.GameMatch?> Handle(GetGameMatchQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
    }
}
