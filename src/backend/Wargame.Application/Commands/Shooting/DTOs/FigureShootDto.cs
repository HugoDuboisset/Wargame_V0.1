namespace Wargame.Application.Commands.Shooting.DTOs;

/// <summary>
/// DTO d'entrée représentant le tir d'une figurine individuelle.
/// Chaque figurine choisit son arme et sa cible.
/// </summary>
public record FigureShootDto(
    Guid FigureId,
    Guid WeaponId,
    Guid TargetUnitId
);
