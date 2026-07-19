using Wargame.Domain.Entities;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable du calcul des positions lors des mouvements d'assaut.
/// Gère la charge (déplacement principal) et la consolidation (2" supplémentaires).
/// 
/// Règle fondamentale : les figurines ne doivent pas se superposer. Le placement se fait
/// figurine par figurine, en tenant compte des positions déjà occupées.
/// </summary>
public class AssaultMovementService
{
    private const double AngleStepDegrees = 10.0;
    private const double ConsolidationDistance = 2.0;

    /// <summary>
    /// Calcule les nouvelles positions pour les figurines chargeantes.
    /// Chaque figurine est déplacée vers la figurine ennemie la plus proche, dans la limite de chargeDistance.
    /// Les figurines sont traitées dans l'ordre de proximité à la cible.
    /// Aucune superposition n'est permise (ni entre attaquants, ni avec les défenseurs).
    /// </summary>
    /// <returns>
    /// Liste de tuples (Figure, NewPosition) pour toutes les figurines pouvant se déplacer.
    /// Les figurines ne pouvant pas atteindre la zone de charge ne sont pas incluses.
    /// </returns>
    public IReadOnlyList<(Figure Figure, Position NewPosition)> CalculateChargePositions(
        IReadOnlyList<Figure> attackers,
        IReadOnlyList<Figure> defenders,
        double chargeDistance)
    {
        var aliveAttackers = attackers.Where(f => f.IsAlive).ToList();
        var aliveDefenders = defenders.Where(f => f.IsAlive).ToList();

        if (!aliveAttackers.Any() || !aliveDefenders.Any())
            return [];

        // Les figurines sont traitées dans l'ordre de proximité à l'ennemi le plus proche (les plus proches en premier)
        var sortedAttackers = aliveAttackers
            .OrderBy(a => aliveDefenders.Min(d => a.GetEdgeDistanceTo(d)))
            .ToList();

        var results = new List<(Figure Figure, Position NewPosition)>();

        // Positions déjà "occupées" après déplacement : positions finales des attaquants déjà traités
        // (les défenseurs ne bougent pas, leurs positions sont fixes)
        var occupiedAfterMove = new List<(Position Center, double RadiusMm)>();
        foreach (var def in aliveDefenders)
        {
            occupiedAfterMove.Add((def.Position, def.BaseSizeMm / 2.0));
        }

        foreach (var attacker in sortedAttackers)
        {
            // Chercher la cible la plus proche
            var closestTarget = aliveDefenders
                .OrderBy(d => attacker.GetEdgeDistanceTo(d))
                .First();

            double edgeDist = attacker.GetEdgeDistanceTo(closestTarget);

            // Si la cible est hors de portée de la charge → on rapproche le maximum possible
            double maxMove = chargeDistance;
            bool canReachContact = edgeDist <= maxMove;

            Position newPos;
            if (canReachContact)
            {
                // Essayer de se placer en contact avec la cible la plus proche
                newPos = FindContactPosition(
                    attacker, closestTarget, aliveDefenders,
                    occupiedAfterMove, attackers);
            }
            else
            {
                // Ne peut pas atteindre la cible : se rapprocher le maximum
                newPos = MoveTowardTarget(attacker.Position, closestTarget.Position,
                    maxMove, attacker.BaseSizeMm, occupiedAfterMove);
            }

            results.Add((attacker, newPos));

            // Ajouter la nouvelle position dans les positions occupées
            occupiedAfterMove.Add((newPos, attacker.BaseSizeMm / 2.0));
        }

        return results;
    }

    /// <summary>
    /// Calcule les nouvelles positions pour la consolidation (2" max).
    /// Objectif : maximiser les contacts attaquants↔défenseurs.
    /// Traite les figurines une par une, en priorisant celles sans contact.
    /// </summary>
    public IReadOnlyList<(Figure Figure, Position NewPosition)> CalculateConsolidationPositions(
        IReadOnlyList<Figure> attackers,
        IReadOnlyList<Figure> defenders)
    {
        var aliveAttackers = attackers.Where(f => f.IsAlive).ToList();
        var aliveDefenders = defenders.Where(f => f.IsAlive).ToList();

        if (!aliveAttackers.Any() || !aliveDefenders.Any())
            return [];

        // Prioriser les figurines sans contact existant (elles ont le plus à gagner)
        var sortedAttackers = aliveAttackers
            .OrderBy(a => aliveDefenders.Min(d => a.GetEdgeDistanceTo(d)))
            .ToList();

        var results = new List<(Figure Figure, Position NewPosition)>();

        var occupiedAfterMove = new List<(Position Center, double RadiusMm)>();
        // Les positions des défenseurs sont fixes
        foreach (var def in aliveDefenders)
            occupiedAfterMove.Add((def.Position, def.BaseSizeMm / 2.0));
        // Les positions des attaquants restent les leurs jusqu'à ce qu'on les déplace
        foreach (var att in aliveAttackers)
            occupiedAfterMove.Add((att.Position, att.BaseSizeMm / 2.0));

        foreach (var attacker in sortedAttackers)
        {
            double edgeDist = aliveDefenders.Min(d => attacker.GetEdgeDistanceTo(d));

            // Si déjà en contact (≤ 0"), pas besoin de bouger
            if (edgeDist <= 0)
            {
                results.Add((attacker, attacker.Position));
                continue;
            }

            // Retirer la position actuelle de cet attaquant (il va bouger)
            occupiedAfterMove.RemoveAll(o =>
                o.Center.DistanceTo(attacker.Position) < 0.001 &&
                Math.Abs(o.RadiusMm - attacker.BaseSizeMm / 2.0) < 0.001);

            var closestTarget = aliveDefenders.OrderBy(d => attacker.GetEdgeDistanceTo(d)).First();

            Position newPos;
            if (edgeDist <= ConsolidationDistance)
            {
                newPos = FindContactPosition(
                    attacker, closestTarget, aliveDefenders,
                    occupiedAfterMove, attackers);
            }
            else
            {
                newPos = MoveTowardTarget(attacker.Position, closestTarget.Position,
                    ConsolidationDistance, attacker.BaseSizeMm, occupiedAfterMove);
            }

            results.Add((attacker, newPos));
            occupiedAfterMove.Add((newPos, attacker.BaseSizeMm / 2.0));
        }

        return results;
    }

    /// <summary>
    /// Tente de trouver une position en contact socle-à-socle avec la cible,
    /// en essayant des angles autour de la cible jusqu'à trouver une position libre.
    /// Fallback : se rapprocher le maximum sans contact si aucune position libre n'est trouvée.
    /// </summary>
    private static Position FindContactPosition(
        Figure attacker,
        Figure primaryTarget,
        IReadOnlyList<Figure> allDefenders,
        List<(Position Center, double RadiusMm)> occupied,
        IReadOnlyList<Figure> allAttackers)
    {
        double attackerRadiusMm = attacker.BaseSizeMm / 2.0;
        double targetRadiusMm = primaryTarget.BaseSizeMm / 2.0;
        // Distance centre-à-centre pour être en contact socle-à-socle
        double contactCenterDist = (attackerRadiusMm + targetRadiusMm) / 25.4;

        // Angle de départ : direction du vecteur attacker → cible
        double baseAngle = Math.Atan2(
            primaryTarget.Position.Y - attacker.Position.Y,
            primaryTarget.Position.X - attacker.Position.X);

        // Essayer des positions à angles croissants autour de la cible
        for (int step = 0; step <= 180 / AngleStepDegrees; step++)
        {
            foreach (int sign in new[] { 1, -1 })
            {
                double angle = baseAngle + Math.PI + sign * step * AngleStepDegrees * Math.PI / 180.0;
                // Position candidate : autour du centre de la cible, à contactCenterDist de rayon
                double candidateX = primaryTarget.Position.X + Math.Cos(angle) * contactCenterDist;
                double candidateY = primaryTarget.Position.Y + Math.Sin(angle) * contactCenterDist;
                var candidate = new Position(candidateX, candidateY);

                if (!OverlapsAny(candidate, attackerRadiusMm, occupied))
                    return candidate;

                if (step == 0) break; // Pas besoin de tester les deux signes pour step=0
            }
        }

        // Aucune position de contact libre : se rapprocher le maximum possible
        return MoveTowardTarget(
            attacker.Position, primaryTarget.Position,
            double.MaxValue, attacker.BaseSizeMm, occupied);
    }

    /// <summary>
    /// Déplace une figurine vers une cible du maximum de maxMoveInches, sans superposition.
    /// Retourne la position la plus proche possible.
    /// </summary>
    private static Position MoveTowardTarget(
        Position from, Position to, double maxMoveInches,
        int baseSizeMm, List<(Position Center, double RadiusMm)> occupied)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double totalDist = Math.Sqrt(dx * dx + dy * dy);

        if (totalDist <= 0) return from;

        double moveAmount = Math.Min(maxMoveInches, totalDist);
        double ratio = moveAmount / totalDist;

        double newX = from.X + dx * ratio;
        double newY = from.Y + dy * ratio;
        var candidate = new Position(newX, newY);

        // Si cette position crée une superposition, on essaie de réduire le déplacement
        // par incréments de 10% jusqu'à trouver une position libre
        while (OverlapsAny(candidate, baseSizeMm / 2.0, occupied) && moveAmount > 0.01)
        {
            moveAmount *= 0.9;
            ratio = moveAmount / totalDist;
            newX = from.X + dx * ratio;
            newY = from.Y + dy * ratio;
            candidate = new Position(newX, newY);
        }

        return candidate;
    }

    /// <summary>
    /// Vérifie si une position (centre + rayon) en pouces chevauche une des positions occupées.
    /// </summary>
    private static bool OverlapsAny(
        Position candidateCenter, double candidateRadiusMm,
        List<(Position Center, double RadiusMm)> occupied)
    {
        double candidateRadiusInches = candidateRadiusMm / 25.4;
        foreach (var (center, radiusMm) in occupied)
        {
            double minDist = candidateRadiusInches + radiusMm / 25.4;
            if (candidateCenter.DistanceTo(center) < minDist - 0.001) // tolérance de 0.001"
                return true;
        }
        return false;
    }
}
