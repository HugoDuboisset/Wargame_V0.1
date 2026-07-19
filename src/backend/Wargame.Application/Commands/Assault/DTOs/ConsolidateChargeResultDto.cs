namespace Wargame.Application.Commands.Assault.DTOs;

/// <summary>
/// Résultat d'une consolidation après charge.
/// </summary>
public record ConsolidateChargeResultDto(
    int FiguresMoved // Nombre de figurines ayant bougé lors de la consolidation
);
