namespace Wargame.Domain.Enums;

/// <summary>
/// Type géométrique d'un terrain, définissant comment il affecte les lignes de vue et le couvert.
/// Implémenté comme Flags : un terrain peut cumuler plusieurs géométries
/// (ex: une forêt dense = Occupation | Interference).
/// </summary>
[Flags]
public enum TerrainGeometry
{
    None = 0,

    /// <summary>
    /// Zone d'occupation (Couvert interne) :
    /// La ligne de vue peut la traverser librement, mais les figurines à l'intérieur bénéficient du couvert.
    /// Ex: cratères, tranchées, décombres au sol, forêt.
    /// </summary>
    Occupation = 1 << 0,

    /// <summary>
    /// Zone d'interférence (Couvert traversant) :
    /// Si la ligne de vue traverse cette zone, la cible bénéficie du couvert même si elle n'est pas dans la zone.
    /// Ex: hautes herbes, grillage, fumée, bâtiment effondré.
    /// </summary>
    Interference = 1 << 1,

    /// <summary>
    /// Zone opaque (Couvert bloquant) :
    /// Bloque totalement la ligne de vue. Le tir est impossible à travers.
    /// Ex: mur de béton armé, bâtiment intact, conteneur, épave de char.
    /// </summary>
    Opaque = 1 << 2
}
