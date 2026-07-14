using Wargame.Domain.Enums;

namespace Wargame.Domain.ValueObjects;

/// <summary>
/// Caractéristiques offensives d'une arme.
/// Immuable : les traits et stats ne changent pas — c'est la résolution de jeu qui les interprète.
/// </summary>
public record WeaponProfile(
    WeaponType Type,
    double Range,
    int Attacks,
    int Damage,
    RangedWeaponCaliber? RangedCaliber,
    MeleeWeaponCategory? MeleeCategory,
    WeaponTrait Traits,
    int ExplosiveHits = 0
);
