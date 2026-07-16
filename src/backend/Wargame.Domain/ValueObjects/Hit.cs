using Wargame.Domain.Enums;

namespace Wargame.Domain.ValueObjects;

/// <summary>
/// Représente une touche réussie lors de la phase de tir.
/// Ces informations seront utilisées lors du jet de blessure.
/// </summary>
public record Hit(
    RangedWeaponCaliber Caliber,
    int Damage,
    WeaponTrait Traits
);
