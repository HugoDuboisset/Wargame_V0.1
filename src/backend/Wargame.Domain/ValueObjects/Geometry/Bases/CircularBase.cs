using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry.Bases;


/// <summary>
/// Socle circulaire (utilisé par l'infanterie et la plupart des figurines).
/// Défini par un rayon en millimètres.
/// L'orientation n'a pas d'impact sur l'encombrement d'un socle rond.
/// </summary>
public class CircularBase : IBaseShape
{
    /// <summary>Rayon du socle en millimètres.</summary>
    public double RadiusMm { get; }

    public CircularBase(double radiusMm)
    {
        if (radiusMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusMm), "Le rayon du socle doit être positif.");
        RadiusMm = radiusMm;
    }

    /// <summary>
    /// Rayon en pouces (inches), unité de mesure utilisée sur la table de jeu.
    /// </summary>
    public double RadiusInches => RadiusMm / 25.4;

    public double GetShortestDistanceTo(
        Position myPosition, double myOrientationDegrees,
        IBaseShape otherShape, Position otherPosition, double otherOrientationDegrees)
    {
        return otherShape switch
        {
            CircularBase otherCircle => GetDistanceToCircle(myPosition, otherCircle, otherPosition),
            RectangularBase otherRect => GetDistanceToRectangle(myPosition, otherRect, otherPosition, otherOrientationDegrees),
            _ => throw new NotSupportedException($"Type de socle non supporté : {otherShape.GetType().Name}")
        };
    }

    /// <summary>
    /// Pour un socle rond, les points clés pour les rayons de ligne de vue sont :
    /// - Le centre
    /// - Les deux tangentes gauche et droite (perpendiculaires à la direction du tir)
    /// Ces tangentes seront calculées dynamiquement dans LineOfSightService selon la cible.
    /// Ici on retourne juste le centre ; le service de LoS gère les tangentes.
    /// </summary>
    public IReadOnlyList<Position> GetSightRaysOrigins(Position position, double orientationDegrees)
    {
        return [position];
    }

    private double GetDistanceToCircle(Position myPos, CircularBase other, Position otherPos)
    {
        double centreToCentre = myPos.DistanceTo(otherPos);
        return centreToCentre - RadiusInches - other.RadiusInches;
    }

    private double GetDistanceToRectangle(Position myPos, RectangularBase rect, Position rectPos, double rectOrientationDegrees)
    {
        // Distance entre le centre du cercle et le point le plus proche du rectangle
        double distCentreToEdge = rect.GetDistanceFromCentreTo(rectPos, rectOrientationDegrees, myPos);
        return distCentreToEdge - RadiusInches;
    }
}
