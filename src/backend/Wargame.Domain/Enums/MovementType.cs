namespace Wargame.Domain.Enums;

/// <summary>
/// Type de mouvement effectué par une unité lors de sa phase de mouvement.
/// Conditionne les actions disponibles dans les phases suivantes (Tir, Assaut).
/// </summary>
public enum MovementType
{
    /// <summary>
    /// L'unité reste immobile. Peut tirer avec tous types d'armes, y compris encombrantes.
    /// Un véhicule peut pivoter.
    /// </summary>
    None,

    /// <summary>
    /// Mouvement normal (jusqu'à M pouces). Peut tirer sauf avec armes encombrantes.
    /// </summary>
    Normal,

    /// <summary>
    /// Sprint (jusqu'à 2×M pouces). Ne peut tirer qu'avec des armes Maniables (-2 au tir).
    /// Interdit de charger.
    /// </summary>
    Sprint,

    /// <summary>
    /// Désengagement d'un corps à corps (nécessite un jet d'action risquée).
    /// Mouvement normal, mais l'unité ne doit plus être en contact avec l'ennemi.
    /// </summary>
    Disengage
}
