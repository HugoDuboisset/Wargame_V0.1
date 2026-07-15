using Microsoft.Extensions.DependencyInjection;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Infrastructure.Configuration;
using Wargame.Infrastructure.Repositories;

namespace Wargame.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, Action<JsonRepositoryOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddScoped<IGameMatchRepository, JsonGameMatchRepository>();
        services.AddScoped<IUnitRepository, JsonUnitRepository>();
        services.AddScoped<IWeaponRepository, JsonWeaponRepository>();

        return services;
    }
}
