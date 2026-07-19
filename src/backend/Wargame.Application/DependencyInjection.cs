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
        services.AddSingleton<Wargame.Domain.Services.IDiceRoller, Wargame.Domain.Services.StandardDiceRoller>();

        // Stratégies de traits d'armes
        services.AddTransient<Wargame.Domain.Services.Traits.IWeaponTraitStrategy, Wargame.Domain.Services.Traits.SuppressionTraitStrategy>();
        services.AddTransient<Wargame.Domain.Services.Traits.IWeaponTraitStrategy, Wargame.Domain.Services.Traits.IncendiaryTraitStrategy>();

        // Spécifications de validation de tir
        services.AddTransient<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec, Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec>();
        services.AddTransient<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec, Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec>();
        services.AddTransient<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec, Wargame.Domain.Specifications.Shooting.RangeSpec>();
        services.AddTransient<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec, Wargame.Domain.Specifications.Shooting.LineOfSightSpec>();

        services.AddTransient<Wargame.Domain.Services.ShootingValidationService>();
        services.AddTransient<Wargame.Domain.Services.ShootingResolutionService>();
        services.AddTransient<Wargame.Domain.Services.DamageResolutionService>();
        services.AddTransient<Wargame.Domain.Services.MoraleResolutionService>();
        services.AddTransient<Wargame.Domain.Services.AssaultValidationService>();
        services.AddTransient<Wargame.Domain.Services.AssaultMovementService>();
        services.AddTransient<Wargame.Domain.Services.AssaultResolutionService>();
        services.AddTransient<Wargame.Domain.Services.ActionResolutionService>();

        return services;
    }
}
