using System.Text.Json.Serialization;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry.Bases;

/// <summary>
/// Définit la forme géométrique du socle d'une figurine.
/// Utilisé pour les calculs d'encombrement, de ligne de vue et de distance.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(CircularBase), "circle")]
[JsonDerivedType(typeof(RectangularBase), "rectangle")]
public interface IBaseShape
{
    /// <summary>
    /// Calcule la distance bord à bord entre ce socle et un autre socle.
    /// Retourne une valeur négative ou nulle si les socles sont en contact ou se chevauchent.
    /// </summary>
    double GetShortestDistanceTo(
        Position myPosition, double myOrientationDegrees,
        IBaseShape otherShape, Position otherPosition, double otherOrientationDegrees);

    /// <summary>
    /// Retourne les points clés du socle (ex: les 4 coins d'un rectangle) pour tracer les lignes de vue.
    /// </summary>
    IReadOnlyList<Position> GetSightRaysOrigins(Position position, double orientationDegrees);
}
