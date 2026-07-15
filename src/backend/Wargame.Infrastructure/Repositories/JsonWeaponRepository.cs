using Microsoft.Extensions.Options;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Infrastructure.Configuration;

namespace Wargame.Infrastructure.Repositories;

public class JsonWeaponRepository : JsonRepositoryBase<Weapon>, IWeaponRepository
{
    public JsonWeaponRepository(IOptions<JsonRepositoryOptions> options) 
        : base(options, "weapons.json")
    {
    }

    public async Task<Weapon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var weapons = await LoadAllAsync(cancellationToken);
        return weapons.FirstOrDefault(w => w.Id == id);
    }

    public async Task<IEnumerable<Weapon>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await LoadAllAsync(cancellationToken);
    }
}
