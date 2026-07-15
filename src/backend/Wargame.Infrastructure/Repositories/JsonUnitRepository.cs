using Microsoft.Extensions.Options;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Infrastructure.Configuration;

namespace Wargame.Infrastructure.Repositories;

public class JsonUnitRepository : JsonRepositoryBase<Unit>, IUnitRepository
{
    public JsonUnitRepository(IOptions<JsonRepositoryOptions> options) 
        : base(options, "units.json")
    {
    }

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var units = await LoadAllAsync(cancellationToken);
        return units.FirstOrDefault(u => u.Id == id);
    }

    public async Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await LoadAllAsync(cancellationToken);
    }

    public async Task SaveAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        var units = await LoadAllAsync(cancellationToken);
        
        var index = units.FindIndex(u => u.Id == unit.Id);
        if (index >= 0)
        {
            units[index] = unit;
        }
        else
        {
            units.Add(unit);
        }

        await SaveAllAsync(units, cancellationToken);
    }
}
