using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.ValueObjects.Geometry.Bases;

/// <summary>
/// Socle rectangulaire orienté (utilisé par les véhicules).
/// Défini par une longueur (axe avant/arrière) et une largeur (axe latéral), en millimètres.
/// Nécessite une orientation en degrés pour tous les calculs de distance et de ligne de vue.
/// Convention : OrientationDegrees=0 signifie que la face avant du véhicule regarde vers la droite (axe X+).
/// </summary>
public class RectangularBase : IBaseShape
{
    /// <summary>Longueur du socle en millimètres (axe avant/arrière).</summary>
    public double LengthMm { get; }

    /// <summary>Largeur du socle en millimètres (axe latéral).</summary>
    public double WidthMm { get; }

    public double LengthInches => LengthMm / 25.4;
    public double WidthInches => WidthMm / 25.4;

    public RectangularBase(double lengthMm, double widthMm)
    {
        if (lengthMm <= 0) throw new ArgumentOutOfRangeException(nameof(lengthMm));
        if (widthMm <= 0) throw new ArgumentOutOfRangeException(nameof(widthMm));
        LengthMm = lengthMm;
        WidthMm = widthMm;
    }

    public double GetShortestDistanceTo(
        Position myPosition, double myOrientationDegrees,
        IBaseShape otherShape, Position otherPosition, double otherOrientationDegrees)
    {
        return otherShape switch
        {
            CircularBase otherCircle => GetDistanceToCircle(myPosition, myOrientationDegrees, otherCircle, otherPosition),
            RectangularBase otherRect => GetDistanceToRect(myPosition, myOrientationDegrees, otherRect, otherPosition, otherOrientationDegrees),
            _ => throw new NotSupportedException($"Type de socle non supporté : {otherShape.GetType().Name}")
        };
    }

    /// <summary>
    /// Pour un socle rectangulaire, les rayons de ligne de vue partent des 4 coins.
    /// </summary>
    public IReadOnlyList<Position> GetSightRaysOrigins(Position position, double orientationDegrees)
    {
        return GetCorners(position, orientationDegrees);
    }

    /// <summary>
    /// Calcule les 4 coins du rectangle, projetés depuis le centre avec l'orientation donnée.
    /// Convention : axe 0° = droite (X+), positif = sens antihoraire.
    /// </summary>
    public IReadOnlyList<Position> GetCorners(Position centre, double orientationDegrees)
    {
        double radians = orientationDegrees * Math.PI / 180.0;
        double cosA = Math.Cos(radians);
        double sinA = Math.Sin(radians);

        double halfL = LengthInches / 2.0;
        double halfW = WidthInches / 2.0;

        // Vecteurs unitaires de la longueur (avant) et de la largeur (côté)
        double fwdX = cosA;
        double fwdY = sinA;
        double sideX = -sinA;
        double sideY = cosA;

        return
        [
            new Position(centre.X + halfL * fwdX + halfW * sideX, centre.Y + halfL * fwdY + halfW * sideY),  // Avant-Gauche
            new Position(centre.X + halfL * fwdX - halfW * sideX, centre.Y + halfL * fwdY - halfW * sideY),  // Avant-Droit
            new Position(centre.X - halfL * fwdX + halfW * sideX, centre.Y - halfL * fwdY + halfW * sideY),  // Arrière-Gauche
            new Position(centre.X - halfL * fwdX - halfW * sideX, centre.Y - halfL * fwdY - halfW * sideY),  // Arrière-Droit
        ];
    }

    /// <summary>
    /// Distance depuis le bord du rectangle à un point externe.
    /// Retourne une valeur positive (distance extérieure), nulle (sur le bord) ou négative (à l'intérieur).
    /// Utilise la projection sur les axes locaux du rectangle.
    /// </summary>
    public double GetDistanceFromCentreTo(Position myPosition, double orientationDegrees, Position targetPoint)
    {
        double dx = targetPoint.X - myPosition.X;
        double dy = targetPoint.Y - myPosition.Y;

        double radians = orientationDegrees * Math.PI / 180.0;
        double cosA = Math.Cos(radians);
        double sinA = Math.Sin(radians);

        // Projeter le vecteur dans le repère local du rectangle
        double localX = dx * cosA + dy * sinA;    // Composante axiale (avant/arrière)
        double localY = -dx * sinA + dy * cosA;   // Composante latérale

        double halfL = LengthInches / 2.0;
        double halfW = WidthInches / 2.0;

        // Distance dans le repère local au bord du rectangle (formule "distance point-rectangle 2D")
        double clamped_x = Math.Clamp(localX, -halfL, halfL);
        double clamped_y = Math.Clamp(localY, -halfW, halfW);

        double distX = localX - clamped_x;
        double distY = localY - clamped_y;

        return Math.Sqrt(distX * distX + distY * distY);
    }

    private double GetDistanceToCircle(Position myPos, double myOri, CircularBase circle, Position circlePos)
    {
        double distEdgeToCenter = GetDistanceFromCentreTo(myPos, myOri, circlePos);
        return distEdgeToCenter - circle.RadiusInches;
    }

    private double GetDistanceToRect(Position myPos, double myOri, RectangularBase other, Position otherPos, double otherOri)
    {
        // Méthode SAT simplifiée : distance de chaque coin de l'un par rapport à l'autre rectangle
        // On prend le minimum de toutes les distances (en valeur signée, négatif = chevauchement)
        double minDist = double.MaxValue;

        foreach (var corner in GetCorners(myPos, myOri))
        {
            double d = other.GetDistanceFromCentreTo(otherPos, otherOri, corner);
            if (d < minDist) minDist = d;
        }

        foreach (var corner in other.GetCorners(otherPos, otherOri))
        {
            double d = GetDistanceFromCentreTo(myPos, myOri, corner);
            if (d < minDist) minDist = d;
        }

        return minDist;
    }
}
