namespace Wargame.Application.Commands.Assault.DTOs;

/// <summary>
/// Résultat de la tentative de charge d'une unité.
/// </summary>
public record ChargeResultDto(
    bool ChargingSucceeded,
    int ChargeRoll,      // Résultat du D6
    double ChargeDistance // Distance totale de charge (D6 + M)
);
