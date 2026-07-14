namespace Wargame.Domain.Enums;

/// <summary>
/// Niveau de protection offert par un terrain.
/// Applique un malus de -1 à -3 aux jets de touche adverses au tir,
/// et un bonus de +1 à +3 à l'initiative du défenseur lors d'un assaut
/// (si le terrain est une zone d'Occupation).
/// </summary>
public enum CoverLevel
{
    /// <summary>Aucun couvert (terrain opaque ou non protecteur).</summary>
    None = 0,

    /// <summary>
    /// Couvert léger (-1 au Tir, +1 en Initiative défensive).
    /// Ex: cratère peu profond, hautes herbes, barbelés.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Couvert intermédiaire (-2 au Tir, +2 en Initiative défensive).
    /// Ex: tranchée, épave de voiture, muret, bâtiment effondré.
    /// </summary>
    Intermediate = 2,

    /// <summary>
    /// Couvert lourd (-3 au Tir, +3 en Initiative défensive).
    /// Ex: tranchée fortifiée, bunker, nid de mitrailleuse, maison debout.
    /// </summary>
    Heavy = 3
}
