namespace Wargame.Application.Commands.Movement.DTOs;

/// <summary>
/// DTO d'entrée représentant le mouvement souhaité pour une figurine individuelle.
/// Les coordonnées X et Y représentent le centre du socle, en pouces.
/// </summary>
public record FigureMoveDto(Guid FigureId, double X, double Y, double? TargetOrientationDegrees = null);
