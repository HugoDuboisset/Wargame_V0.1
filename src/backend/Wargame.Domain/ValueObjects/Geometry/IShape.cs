using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry;

/// <summary>
/// Représente une forme géométrique 2D sur le plateau de jeu (ex: pour les terrains).
/// </summary>
public interface IShape
{
    /// <summary>
    /// Vérifie si une position donnée se trouve à l'intérieur de la forme.
    /// </summary>
    bool Contains(Position position);

    /// <summary>
    /// Vérifie si un segment de ligne (défini par un point de départ et d'arrivée) croise ou traverse la forme.
    /// Utile pour vérifier les lignes de vue (LoS).
    /// </summary>
    bool Intersects(Position start, Position end);
}
