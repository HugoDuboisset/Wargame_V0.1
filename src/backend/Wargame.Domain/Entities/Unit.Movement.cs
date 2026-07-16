using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// Partie de la classe Unit dédiée à la mécanique de mouvement.
/// Séparée dans un fichier partiel pour maintenir la lisibilité du fichier principal.
/// </summary>
public partial class Unit
{
    // =====================================================================
    //  MOUVEMENT
    // =====================================================================

    /// <summary>
    /// Vérifie si l'unité est capable d'effectuer un mouvement normal ou un sprint.
    /// Retourne false si l'unité est engagée au corps à corps ou clouée au sol.
    /// </summary>
    public bool CanMove() => !IsEngaged() && GetEffectiveMovement() > 0;

    /// <summary>
    /// Valide que la cohésion de l'unité sera respectée après application des nouveaux déplacements.
    /// Règle : si 1 figurine, pas de contrainte.
    ///         si ≤ 5 figurines : chaque figurine doit être à ≤ 1" bord à bord d'au moins 1 voisine.
    ///         si > 5 figurines : chaque figurine doit être à ≤ 2" bord à bord d'au moins 2 voisines.
    /// </summary>
    /// <param name="moves">Les nouveaux déplacements demandés. Les figurines non listées gardent leur position actuelle.</param>
    /// <returns>La liste des messages d'erreur de cohésion. Vide si tout est valide.</returns>
    public IReadOnlyList<string> ValidateCohesion(IReadOnlyList<FigureMove> moves)
    {
        var errors = new List<string>();
        var aliveFigures = _figures.Where(f => f.IsAlive).ToList();

        // Cas trivial : 1 seule figurine, pas de contrainte de cohésion
        if (aliveFigures.Count <= 1)
            return errors;

        // Reconstruction du dictionnaire des positions finales (centre du socle)
        var moveDict = moves.ToDictionary(m => m.FigureId, m => m.NewPosition);
        var finalPositions = aliveFigures.ToDictionary(
            f => f.Id,
            f => moveDict.TryGetValue(f.Id, out var newPos) ? newPos : f.Position
        );

        bool largeUnit = aliveFigures.Count > 5;
        double maxDistance = largeUnit ? 2.0 : 1.0;
        int requiredNeighbors = largeUnit ? 2 : 1;

        foreach (var figure in aliveFigures)
        {
            var figurePos = finalPositions[figure.Id];
            var neighbors = aliveFigures
                .Where(other => other.Id != figure.Id)
                .Select(other =>
                {
                    var otherPos = finalPositions[other.Id];
                    var centreToCentre = figurePos.DistanceTo(otherPos);
                    var radiusA = (figure.BaseSizeMm / 2.0) / 25.4;
                    var radiusB = (other.BaseSizeMm / 2.0) / 25.4;
                    return centreToCentre - radiusA - radiusB;
                })
                .Count(dist => dist <= maxDistance);

            if (neighbors < requiredNeighbors)
            {
                errors.Add(
                    $"La figurine {figure.Id} brise la cohésion de l'unité : " +
                    $"seulement {neighbors} voisine(s) à {maxDistance}\" ou moins, {requiredNeighbors} requise(s).");
            }
        }

        return errors;
    }

    /// <summary>
    /// Applique les déplacements des figurines et enregistre le type de mouvement effectué.
    /// Ne fait aucune validation (celle-ci doit être effectuée par le Handler avant d'appeler cette méthode).
    /// </summary>
    public void Move(IReadOnlyList<FigureMove> moves, MovementType type)
    {
        foreach (var move in moves)
        {
            var figure = _figures.FirstOrDefault(f => f.Id == move.FigureId);
            figure?.MoveTo(move.NewPosition);
        }
        SetMovement(type);
    }
}
