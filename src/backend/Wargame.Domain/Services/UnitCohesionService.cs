using Wargame.Domain.Entities;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services;

/// <summary>
/// Service de domaine gérant la cohésion d'unité.
/// Règles : 
/// - > 5 figurines : à 2" d'au moins 2 autres.
/// - <= 5 figurines : à 2" d'au moins 1 autre.
/// - L'unité doit former un seul groupe continu.
/// </summary>
public class UnitCohesionService
{
    private const double CohesionDistance = 2.0;
    private const double RegroupDistance = 2.0;

    /// <summary>
    /// Vérifie si l'ensemble des figurines fournies respecte les règles de cohésion.
    /// Accepte un dictionnaire optionnel de positions simulées pour valider un déplacement avant de l'appliquer.
    /// </summary>
    public bool IsInCohesion(IReadOnlyList<Figure> figures, IReadOnlyDictionary<Guid, Position>? simulatedPositions = null)
    {
        if (figures.Count <= 1)
            return true;

        int requiredConnections = figures.Count > 5 ? 2 : 1;

        double GetDist(Figure a, Figure b)
        {
            var posA = simulatedPositions?.GetValueOrDefault(a.Id, a.Position) ?? a.Position;
            var posB = simulatedPositions?.GetValueOrDefault(b.Id, b.Position) ?? b.Position;
            // On utilise GetEdgeDistanceTo quand les positions ne sont pas simulées,
            // sinon on délègue à la forme de base (les orientations ne changent pas lors des simulations de mouvement d'infanterie)
            if (simulatedPositions == null)
                return a.GetEdgeDistanceTo(b);
            var centreToCentre = posA.DistanceTo(posB);
            return a.BaseShape.GetShortestDistanceTo(posA, a.OrientationDegrees, b.BaseShape, posB, b.OrientationDegrees);
        }

        // 1. Vérification des degrés
        foreach (var fig in figures)
        {
            int connections = figures.Count(other => other != fig && GetDist(fig, other) <= CohesionDistance + 0.001);
            if (connections < requiredConnections)
                return false;
        }

        // 2. Vérification de la connexité (Graphe continu)
        var visited = new HashSet<Figure>();
        var queue = new Queue<Figure>();
        
        queue.Enqueue(figures[0]);
        visited.Add(figures[0]);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var other in figures)
            {
                if (!visited.Contains(other) && GetDist(current, other) <= CohesionDistance + 0.001)
                {
                    visited.Add(other);
                    queue.Enqueue(other);
                }
            }
        }

        return visited.Count == figures.Count;
    }

    /// <summary>
    /// Résout la perte de cohésion d'une unité en appliquant la règle de regroupement de 2",
    /// puis en détruisant les figurines isolées si la cohésion n'est toujours pas rétablie.
    /// </summary>
    public (List<Figure> MovedFigures, List<Figure> DestroyedFigures) ResolveCohesionLoss(Unit unit)
    {
        var aliveFigures = unit.Figures.Where(f => f.IsAlive).ToList();
        
        if (aliveFigures.Count <= 1 || IsInCohesion(aliveFigures))
        {
            return (new List<Figure>(), new List<Figure>());
        }

        var movedFigures = new List<Figure>();
        var destroyedFigures = new List<Figure>();

        // 1. Déterminer le "Groupe Principal" (Core) initial
        var coreGroup = GetCoreGroup(aliveFigures);
        var isolatedFigures = aliveFigures.Except(coreGroup).ToList();

        // 2. Mouvement de Regroupement de 2" pour les figurines isolées
        if (coreGroup.Any() && isolatedFigures.Any())
        {
            foreach (var isolated in isolatedFigures)
            {
                var nearestCore = coreGroup.OrderBy(c => isolated.GetEdgeDistanceTo(c)).First();
                
                // Mouvement vers la figurine du core
                double dist = isolated.GetEdgeDistanceTo(nearestCore);
                if (dist > 0)
                {
                    double moveDist = Math.Min(RegroupDistance, dist);
                    double dx = nearestCore.Position.X - isolated.Position.X;
                    double dy = nearestCore.Position.Y - isolated.Position.Y;
                    double length = Math.Sqrt(dx * dx + dy * dy);
                    
                    if (length > 0)
                    {
                        double newX = isolated.Position.X + (dx / length) * moveDist;
                        double newY = isolated.Position.Y + (dy / length) * moveDist;
                        isolated.MoveTo(new Position(newX, newY));
                        movedFigures.Add(isolated);
                    }
                }
            }
        }

        // 3. Revérification de la cohésion
        if (IsInCohesion(aliveFigures))
        {
            return (movedFigures, destroyedFigures);
        }

        // 4. Fallback : Destruction des déserteurs
        // On recalcule le core après les mouvements
        var finalCore = GetCoreGroup(aliveFigures);
        var finalIsolated = aliveFigures.Except(finalCore).ToList();

        foreach (var fig in finalIsolated)
        {
            fig.TakeDamage(9999); // Force la destruction
            destroyedFigures.Add(fig);
        }

        return (movedFigures, destroyedFigures);
    }

    /// <summary>
    /// Retourne le plus grand sous-groupe continu respectant les connexions requises.
    /// Utilise un algorithme k-core simplifié.
    /// </summary>
    private List<Figure> GetCoreGroup(List<Figure> figures)
    {
        if (figures.Count <= 1) return figures.ToList();

        var candidates = figures.ToList();

        while (candidates.Count > 0)
        {
            int req = candidates.Count > 5 ? 2 : 1;
            
            var invalidNodes = candidates.Where(fig => 
                candidates.Count(other => other != fig && fig.GetEdgeDistanceTo(other) <= CohesionDistance + 0.001) < req
            ).ToList();

            if (!invalidNodes.Any())
                break; // Le sous-graphe est valide pour les degrés

            // On retire les noeuds invalides
            candidates.RemoveAll(f => invalidNodes.Contains(f));
        }

        if (candidates.Count == 0)
        {
            // Si l'algo supprime tout, on retourne au moins la plus grande composante connexe initiale
            // pour avoir un point d'ancrage.
            return GetLargestConnectedComponent(figures);
        }

        // Ensuite, on s'assure de ne garder que la plus grande composante connexe
        return GetLargestConnectedComponent(candidates);
    }

    private List<Figure> GetLargestConnectedComponent(List<Figure> figures)
    {
        if (figures.Count == 0) return new List<Figure>();

        var allVisited = new HashSet<Figure>();
        var components = new List<List<Figure>>();

        foreach (var startNode in figures)
        {
            if (allVisited.Contains(startNode)) continue;

            var compVisited = new HashSet<Figure>();
            var queue = new Queue<Figure>();
            
            queue.Enqueue(startNode);
            compVisited.Add(startNode);
            allVisited.Add(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var other in figures)
                {
                    if (!compVisited.Contains(other) && current.GetEdgeDistanceTo(other) <= CohesionDistance + 0.001)
                    {
                        compVisited.Add(other);
                        allVisited.Add(other);
                        queue.Enqueue(other);
                    }
                }
            }

            components.Add(compVisited.ToList());
        }

        return components.OrderByDescending(c => c.Count).First();
    }
}
