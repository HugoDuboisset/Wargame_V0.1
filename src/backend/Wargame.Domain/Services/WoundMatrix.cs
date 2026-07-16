using Wargame.Domain.Enums;

namespace Wargame.Domain.Services;

/// <summary>
/// Service contenant les tables de résolution des blessures.
/// Définit le score à obtenir sur 1D10 pour blesser selon le Calibre/Catégorie de l'arme et l'Armure de la cible.
/// </summary>
public static class WoundMatrix
{
    // Valeur spéciale signifiant qu'il est impossible de blesser
    public const int Impossible = 11;

    /// <summary>
    /// Retourne le score cible (Target Number) sur 1D10 pour blesser avec une arme de tir.
    /// </summary>
    public static int GetRangedTargetNumber(RangedWeaponCaliber caliber, ArmorClass armor)
    {
        return caliber switch
        {
            RangedWeaponCaliber.SmallCaliber => armor switch
            {
                ArmorClass.Unarmored => 3,
                ArmorClass.Light => 7,
                ArmorClass.Heavy => 9,
                ArmorClass.LightVehicle => Impossible,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            RangedWeaponCaliber.MediumCaliber => armor switch
            {
                ArmorClass.Unarmored => 3,
                ArmorClass.Light => 4,
                ArmorClass.Heavy => 7,
                ArmorClass.LightVehicle => 9,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            RangedWeaponCaliber.PiercingCaliber => armor switch
            {
                ArmorClass.Unarmored => 4,
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 5,
                ArmorClass.LightVehicle => 7,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            RangedWeaponCaliber.HeavyCaliber => armor switch
            {
                ArmorClass.Unarmored => 2,
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 4,
                ArmorClass.LightVehicle => 6,
                ArmorClass.HeavyVehicle => 9,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            RangedWeaponCaliber.AntiTank => armor switch
            {
                ArmorClass.Unarmored => 5,
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 3,
                ArmorClass.LightVehicle => 3,
                ArmorClass.HeavyVehicle => 5,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(caliber))
        };
    }

    /// <summary>
    /// Retourne le score cible (Target Number) sur 1D10 pour blesser avec une arme de corps à corps.
    /// </summary>
    public static int GetMeleeTargetNumber(MeleeWeaponCategory category, ArmorClass armor)
    {
        return category switch
        {
            MeleeWeaponCategory.Light => armor switch
            {
                ArmorClass.Unarmored => 4,
                ArmorClass.Light => 5,
                ArmorClass.Heavy => 8,
                ArmorClass.LightVehicle => Impossible,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            MeleeWeaponCategory.Medium => armor switch
            {
                ArmorClass.Unarmored => 3,
                ArmorClass.Light => 4,
                ArmorClass.Heavy => 6,
                ArmorClass.LightVehicle => Impossible,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            MeleeWeaponCategory.Heavy => armor switch
            {
                ArmorClass.Unarmored => 3,
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 5,
                ArmorClass.LightVehicle => 8,
                ArmorClass.HeavyVehicle => Impossible,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            MeleeWeaponCategory.Thermal => armor switch
            {
                ArmorClass.Unarmored => 4,
                ArmorClass.Light => 3,
                ArmorClass.Heavy => 3,
                ArmorClass.LightVehicle => 4,
                ArmorClass.HeavyVehicle => 6,
                _ => throw new ArgumentOutOfRangeException(nameof(armor))
            },
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };
    }
}
