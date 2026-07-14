namespace Wargame.Domain.Enums;

/// <summary>
/// Classe d'armure (CA) portée par une unité.
/// Détermine la résistance aux blessures (utilisée dans les matrices de blessure).
/// </summary>
public enum ArmorClass
{
    Unarmored = 0,
    Light = 1,
    Heavy = 2,
    LightVehicle = 3,
    HeavyVehicle = 4
}
