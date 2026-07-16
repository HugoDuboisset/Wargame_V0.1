using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Wargame.Application.Behaviors;

namespace Wargame.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Configuration de MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            
            // Ajout de notre Behavior de validation dans le pipeline
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Enregistrement de tous les validateurs (AbstractValidator) du projet
        services.AddValidatorsFromAssembly(assembly);

        // Services du Domaine
        services.AddTransient<Wargame.Domain.Services.ShootingValidationService>();
        services.AddTransient<Wargame.Domain.Services.ShootingResolutionService>();
        services.AddTransient<Wargame.Domain.Services.DamageResolutionService>();

        return services;
    }
}
