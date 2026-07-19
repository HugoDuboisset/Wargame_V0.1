namespace Wargame.Domain.ValueObjects;

/// <summary>
/// Représente le mouvement souhaité d'une figurine individuelle.
/// Contient l'identifiant de la figurine et sa nouvelle position cible (centre du socle, en pouces).
/// </summary>
public record FigureMove(Guid FigureId, Position NewPosition, double? TargetOrientationDegrees = null);
