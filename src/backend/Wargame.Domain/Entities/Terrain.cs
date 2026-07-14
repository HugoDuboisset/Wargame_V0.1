using Wargame.Domain.Enums;
using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// Représente un élément de décor sur la table de jeu.
/// Affecte les lignes de vue et offre protection et avantages tactiques.
/// Le bonus d'assaut est stocké ici pour éviter des calculs à rallonge lors de la résolution.
/// </summary>
public class Terrain : Entity
{
    public string Name { get; private set; }

    /// <summary>Position du terrain sur la table (en pouces).</summary>
    public Position Position { get; private set; }

    /// <summary>
    /// Type(s) géométrique(s) du terrain (Flags cumulables).
    /// Ex: TerrainGeometry.Occupation | TerrainGeometry.Interference pour une forêt dense.
    /// </summary>
    public TerrainGeometry Geometry { get; private set; }

    /// <summary>Niveau de protection offert par ce terrain.</summary>
    public CoverLevel CoverLevel { get; private set; }

    /// <summary>
    /// Malus appliqué aux jets de touche au tir ciblant une unité bénéficiant de ce couvert.
    /// Valeur négative : -1 (Léger), -2 (Intermédiaire), -3 (Lourd).
    /// </summary>
    public int CoverPenalty => -(int)CoverLevel;

    /// <summary>
    /// Bonus d'Initiative accordé à l'unité défendant dans ce terrain quand elle subit un assaut.
    /// S'applique uniquement si la géométrie est Occupation (les unités à l'intérieur bénéficient du couvert).
    /// Valeur : 0, +1, +2 ou +3 selon le niveau de couvert.
    /// </summary>
    public int AssaultInitiativeBonus =>
        Geometry.HasFlag(TerrainGeometry.Occupation) ? (int)CoverLevel : 0;

    /// <summary>True si ce terrain bloque totalement les lignes de vue.</summary>
    public bool IsOpaque => Geometry.HasFlag(TerrainGeometry.Opaque);

    public Terrain(Guid id, string name, Position position,
                   TerrainGeometry geometry, CoverLevel coverLevel) : base(id)
    {
        Name = name;
        Position = position;
        Geometry = geometry;
        CoverLevel = coverLevel;
    }
}
