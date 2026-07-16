using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services.Traits;

/// <summary>
/// Interface pour l'application des effets spécifiques d'un trait d'arme lors de la résolution des dégâts.
/// </summary>
public interface IWeaponTraitStrategy
{
    /// <summary>
    /// Le trait géré par cette stratégie.
    /// </summary>
    WeaponTrait TargetTrait { get; }

    /// <summary>
    /// Applique l'effet du trait sur l'unité cible suite à une touche réussie.
    /// </summary>
    void ApplyEffect(Unit targetUnit, Hit hit);
}
