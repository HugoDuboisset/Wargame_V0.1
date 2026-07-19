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
    /// Fait appel à UnitCohesionService pour vérifier les règles globales.
    /// </summary>
    public IReadOnlyList<string> ValidateCohesion(IReadOnlyList<FigureMove> moves, Services.UnitCohesionService cohesionService)
    {
        var errors = new List<string>();
        var aliveFigures = _figures.Where(f => f.IsAlive).ToList();

        // Cas trivial : 1 seule figurine, pas de contrainte de cohésion
        if (aliveFigures.Count <= 1)
            return errors;

        // Reconstruction du dictionnaire des positions finales
        var moveDict = moves.ToDictionary(m => m.FigureId, m => m.NewPosition);
        
        if (!cohesionService.IsInCohesion(aliveFigures, moveDict))
        {
            errors.Add("La position finale ne respecte pas les règles de cohésion d'unité (Distance 2\" et groupe continu).");
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
