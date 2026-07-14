using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// Représente la table de jeu physique.
/// Gère les dimensions du plateau, les zones de déploiement et la disposition des terrains.
/// Entité propre permettant d'ajouter de futurs traits de plateau (objectifs, zones spéciales, etc.).
/// </summary>
public class Board : Entity
{
    private readonly List<Terrain> _terrains = [];

    /// <summary>Largeur du plateau en pouces (axe X). Minimum recommandé : 50".</summary>
    public double Width { get; private set; }

    /// <summary>Profondeur du plateau en pouces (axe Y). Minimum recommandé : 40".</summary>
    public double Height { get; private set; }

    /// <summary>
    /// Profondeur de la zone de déploiement de chaque joueur, en pouces.
    /// Mesurée depuis le bord de table respectif de chaque joueur.
    /// Valeur par défaut : 10" (règle scénario Annihilation).
    /// </summary>
    public double DeploymentZoneDepth { get; private set; }

    /// <summary>Terrains disposés sur ce plateau.</summary>
    public IReadOnlyList<Terrain> Terrains => _terrains.AsReadOnly();

    public Board(Guid id, double width, double height, double deploymentZoneDepth = 10.0) : base(id)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Board width must be positive.");
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Board height must be positive.");
        if (deploymentZoneDepth <= 0) throw new ArgumentOutOfRangeException(nameof(deploymentZoneDepth), "Deployment zone depth must be positive.");

        Width = width;
        Height = height;
        DeploymentZoneDepth = deploymentZoneDepth;
    }

    /// <summary>Ajoute un élément de terrain au plateau.</summary>
    public void AddTerrain(Terrain terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        _terrains.Add(terrain);
    }

    /// <summary>Retire un élément de terrain du plateau par son identifiant.</summary>
    public void RemoveTerrain(Guid terrainId)
    {
        _terrains.RemoveAll(t => t.Id == terrainId);
    }

    /// <summary>
    /// Retourne la zone de déploiement du joueur 1 (bord Y = 0 à DeploymentZoneDepth).
    /// </summary>
    public (double MinY, double MaxY) GetPlayer1DeploymentZone() =>
        (0, DeploymentZoneDepth);

    /// <summary>
    /// Retourne la zone de déploiement du joueur 2 (bord Y = Height - DeploymentZoneDepth à Height).
    /// </summary>
    public (double MinY, double MaxY) GetPlayer2DeploymentZone() =>
        (Height - DeploymentZoneDepth, Height);

    /// <summary>
    /// Vérifie si une position donnée est dans les limites du plateau.
    /// </summary>
    public bool IsWithinBounds(Position position) =>
        position.X >= 0 && position.X <= Width &&
        position.Y >= 0 && position.Y <= Height;
}
