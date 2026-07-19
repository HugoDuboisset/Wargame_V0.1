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
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Génère un faisceau de 5 rayons entre les deux figurines pour approximer la vue bord à bord :
    /// - Centre à Centre
    /// - Bord Gauche à Bord Gauche
    /// - Bord Droit à Bord Droit
    /// - Bord Gauche à Bord Droit
    /// - Bord Droit à Bord Gauche
    /// </summary>
    private static IReadOnlyList<(Position Start, Position End)> GenerateSightRays(Figure shooter, Figure target)
    {
        var rays = new List<(Position Start, Position End)>();

        var centerA = shooter.Position;
        var centerB = target.Position;

        rays.Add((centerA, centerB));

        double dx = centerB.X - centerA.X;
        double dy = centerB.Y - centerA.Y;
        
        // Si les figurines sont parfaitement superposées (ne devrait pas arriver), on retourne juste le centre
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return rays;

        // Calcul du vecteur perpendiculaire
        double length = Math.Sqrt(dx * dx + dy * dy);
        double perpX = -dy / length;
        double perpY = dx / length;

        double radiusA = (shooter.BaseSizeMm / 2.0) / 25.4; // en pouces
        double radiusB = (target.BaseSizeMm / 2.0) / 25.4;

        var leftA = new Position(centerA.X + perpX * radiusA, centerA.Y + perpY * radiusA);
        var rightA = new Position(centerA.X - perpX * radiusA, centerA.Y - perpY * radiusA);

        var leftB = new Position(centerB.X + perpX * radiusB, centerB.Y + perpY * radiusB);
        var rightB = new Position(centerB.X - perpX * radiusB, centerB.Y - perpY * radiusB);

        rays.Add((leftA, leftB));
        rays.Add((rightA, rightB));
        rays.Add((leftA, rightB));
        rays.Add((rightA, leftB));

        return rays;
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
