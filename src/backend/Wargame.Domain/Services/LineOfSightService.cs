using Wargame.Domain.Entities;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine chargé de valider les lignes de vue entre les figurines.
/// Utilise une géométrie simplifiée (rectangles orientés pour les terrains) et
/// vérifie si les socles des figurines sont visibles en traçant plusieurs lignes de vue.
/// </summary>
public class LineOfSightService
{
    /// <summary>
    /// Vérifie si la cible est visible depuis le tireur.
    /// Un tir est possible si au moins UNE ligne tirée depuis un bord du socle du tireur
    /// jusqu'à un bord du socle de la cible n'est pas bloquée par un terrain opaque.
    /// </summary>
    public static bool IsVisible(Figure shooter, Figure target, IEnumerable<Terrain> opaqueTerrains)
    {
        var terrains = opaqueTerrains.ToList();
        if (!terrains.Any())
            return true;

        var rays = GenerateSightRays(shooter, target);

        // Si au moins un rayon traverse sans encombre les terrains opaques, la cible est visible.
        foreach (var ray in rays)
        {
            if (!IsRayBlocked(ray, terrains))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Génère un faisceau de rayons entre les deux figurines.
    /// - Pour un socle circulaire : on utilise le centre + les tangentes gauche/droite.
    /// - Pour un socle rectangulaire : on utilise les 4 coins.
    /// On trace des rayons depuis chaque point d'origine du tireur vers chaque point de la cible.
    /// </summary>
    private static IReadOnlyList<(Position Start, Position End)> GenerateSightRays(Figure shooter, Figure target)
    {
        var rays = new List<(Position Start, Position End)>();

        var shooterOrigins = GetKeyPoints(shooter, target.Position);
        var targetOrigins = GetKeyPoints(target, shooter.Position);

        foreach (var start in shooterOrigins)
            foreach (var end in targetOrigins)
                rays.Add((start, end));

        return rays;
    }

    /// <summary>
    /// Retourne les points clés d'une figurine pour les lignes de vue.
    /// - Socle circulaire : centre + tangentes gauche/droite par rapport à la direction vers la cible.
    /// - Socle rectangulaire : les 4 coins du socle orienté.
    /// </summary>
    private static IReadOnlyList<Position> GetKeyPoints(Figure fig, Position targetPos)
    {
        var shapeOrigins = fig.BaseShape.GetSightRaysOrigins(fig.Position, fig.OrientationDegrees);

        // Si le socle est rectangulaire (déjà les 4 coins), on les retourne directement
        if (shapeOrigins.Count > 1)
            return shapeOrigins;

        // Socle circulaire : on ajoute les tangentes gauche et droite
        var centre = fig.Position;
        var points = new List<Position> { centre };

        double dx = targetPos.X - centre.X;
        double dy = targetPos.Y - centre.Y;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return points; // figurines superposées, juste le centre

        // Calcul du rayon en pouces à partir du CircularBase
        double radiusInches = 0;
        if (fig.BaseShape is Wargame.Domain.ValueObjects.Geometry.Bases.CircularBase circle)
            radiusInches = circle.RadiusInches;

        double length = Math.Sqrt(dx * dx + dy * dy);
        double perpX = -dy / length;
        double perpY = dx / length;

        points.Add(new Position(centre.X + perpX * radiusInches, centre.Y + perpY * radiusInches));
        points.Add(new Position(centre.X - perpX * radiusInches, centre.Y - perpY * radiusInches));

        return points;
    }

    private static bool IsRayBlocked((Position Start, Position End) ray, List<Terrain> terrains)
    {
        foreach (var terrain in terrains)
        {
            if (Intersects(ray.Start, ray.End, terrain))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Vérifie l'intersection entre un segment (Start, End) et un Terrain.
    /// </summary>
    public static bool Intersects(Position start, Position end, Terrain terrain)
    {
        return terrain.Shape.Intersects(start, end);
    }
}
