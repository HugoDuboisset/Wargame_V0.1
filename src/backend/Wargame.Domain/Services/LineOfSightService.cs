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
    /// Vérifie l'intersection entre un segment (Start, End) et un Rectangle orienté (Terrain).
    /// </summary>
    public static bool Intersects(Position start, Position end, Terrain terrain)
    {
        // 1. Convertir les dimensions du terrain de millimètres en pouces
        double widthInches = terrain.WidthMm / 25.4;
        double lengthInches = terrain.LengthMm / 25.4;

        // 2. Transférer le segment dans l'espace local du terrain
        // a) Translation pour mettre le terrain au centre (0,0)
        double startX = start.X - terrain.Position.X;
        double startY = start.Y - terrain.Position.Y;
        
        double endX = end.X - terrain.Position.X;
        double endY = end.Y - terrain.Position.Y;

        // b) Rotation inverse
        double angleRad = -terrain.RotationDegrees * Math.PI / 180.0;
        double cosA = Math.Cos(angleRad);
        double sinA = Math.Sin(angleRad);

        double localStartX = startX * cosA - startY * sinA;
        double localStartY = startX * sinA + startY * cosA;

        double localEndX = endX * cosA - endY * sinA;
        double localEndY = endX * sinA + endY * cosA;

        // 3. Algorithme d'intersection d'un segment avec un rectangle AABB (Axis-Aligned Bounding Box)
        // Les limites du rectangle centré sur 0
        double minX = -widthInches / 2.0;
        double maxX = widthInches / 2.0;
        double minY = -lengthInches / 2.0;
        double maxY = lengthInches / 2.0;

        // Utilisation de l'algorithme de Cohen-Sutherland ou similaire simplifié
        return LineIntersectsAABB(localStartX, localStartY, localEndX, localEndY, minX, minY, maxX, maxY);
    }

    private static bool LineIntersectsAABB(double x1, double y1, double x2, double y2, double minX, double minY, double maxX, double maxY)
    {
        // Rejeter si le segment entier est hors d'une limite
        if (x1 < minX && x2 < minX) return false;
        if (x1 > maxX && x2 > maxX) return false;
        if (y1 < minY && y2 < minY) return false;
        if (y1 > maxY && y2 > maxY) return false;

        // Tester si un point est à l'intérieur
        if ((x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY) ||
            (x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY))
            return true;

        // Tester l'intersection avec les segments du rectangle
        if (LineIntersectsLine(x1, y1, x2, y2, minX, minY, maxX, minY)) return true; // Bas
        if (LineIntersectsLine(x1, y1, x2, y2, minX, maxY, maxX, maxY)) return true; // Haut
        if (LineIntersectsLine(x1, y1, x2, y2, minX, minY, minX, maxY)) return true; // Gauche
        if (LineIntersectsLine(x1, y1, x2, y2, maxX, minY, maxX, maxY)) return true; // Droite

        return false;
    }

    private static bool LineIntersectsLine(double p0_x, double p0_y, double p1_x, double p1_y, 
                                           double p2_x, double p2_y, double p3_x, double p3_y)
    {
        double s1_x, s1_y, s2_x, s2_y;
        s1_x = p1_x - p0_x;     
        s1_y = p1_y - p0_y;
        s2_x = p3_x - p2_x;     
        s2_y = p3_y - p2_y;

        double s, t;
        double denom = -s2_x * s1_y + s1_x * s2_y;
        if (Math.Abs(denom) < 0.0001) return false; // Parallèles

        s = (-s1_y * (p0_x - p2_x) + s1_x * (p0_y - p2_y)) / denom;
        t = ( s2_x * (p0_y - p2_y) - s2_y * (p0_x - p2_x)) / denom;

        return s >= 0 && s <= 1 && t >= 0 && t <= 1;
    }
}
