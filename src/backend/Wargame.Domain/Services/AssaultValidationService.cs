using Wargame.Domain.Entities;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de la validation d'une déclaration de charge.
/// </summary>
public class AssaultValidationService
{
    /// <summary>
    /// Valide qu'une unité peut déclarer une charge contre une cible.
    /// Lance une InvalidOperationException si les conditions ne sont pas remplies.
    /// </summary>
    public void ValidateCharge(Unit chargingUnit, Unit targetUnit)
    {
        if (chargingUnit.MovementThisTurn == Enums.MovementType.Sprint)
            throw new InvalidOperationException("Une unité ayant sprinté ne peut pas charger.");

        if (chargingUnit.IsEngaged())
            throw new InvalidOperationException("Une unité engagée au corps à corps ne peut pas déclarer une nouvelle charge.");

        if (targetUnit.Id == chargingUnit.Id)
            throw new InvalidOperationException("Une unité ne peut pas se charger elle-même.");

        if (targetUnit.LifecycleStatus != Enums.UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité cible est hors de combat.");
    }
}
