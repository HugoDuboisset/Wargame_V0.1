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

    /// <summary>Largeur du terrain (axe X local) en millimètres.</summary>
    public int WidthMm { get; private set; }

    /// <summary>Longueur/Profondeur du terrain (axe Y local) en millimètres.</summary>
    public int LengthMm { get; private set; }

    /// <summary>Angle de rotation en degrés (0-359), dans le sens horaire par rapport au nord.</summary>
    public int RotationDegrees { get; private set; }

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
                   int widthMm, int lengthMm, int rotationDegrees,
                   TerrainGeometry geometry, CoverLevel coverLevel) : base(id)
    {
        Name = name;
        Position = position;
        WidthMm = widthMm;
        LengthMm = lengthMm;
        RotationDegrees = rotationDegrees;
        Geometry = geometry;
        CoverLevel = coverLevel;
    }
}
