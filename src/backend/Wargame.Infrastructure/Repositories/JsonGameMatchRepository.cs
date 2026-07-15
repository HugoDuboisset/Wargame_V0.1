using Microsoft.Extensions.Options;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Infrastructure.Configuration;

namespace Wargame.Infrastructure.Repositories;

public class JsonGameMatchRepository : JsonRepositoryBase<GameMatch>, IGameMatchRepository
{
    public JsonGameMatchRepository(IOptions<JsonRepositoryOptions> options) 
        : base(options, "games.json")
    {
    }

    public async Task<GameMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var games = await LoadAllAsync(cancellationToken);
        return games.FirstOrDefault(g => g.Id == id);
    }

    public async Task SaveAsync(GameMatch gameMatch, CancellationToken cancellationToken = default)
    {
        var games = await LoadAllAsync(cancellationToken);
        
        var index = games.FindIndex(g => g.Id == gameMatch.Id);
        if (index >= 0)
        {
            games[index] = gameMatch;
        }
        else
        {
            games.Add(gameMatch);
        }

        await SaveAllAsync(games, cancellationToken);
    }
}
