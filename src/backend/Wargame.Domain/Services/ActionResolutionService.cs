using Wargame.Domain.Entities;

namespace Wargame.Domain.Services;

/// <summary>
/// Service gérant les jets d'actions et d'actions risquées.
/// </summary>
public class ActionResolutionService
{
    private readonly IDiceRoller _diceRoller;

    public ActionResolutionService(IDiceRoller diceRoller)
    {
        _diceRoller = diceRoller;
    }

    /// <summary>
    /// Résout un test d'action risquée.
    /// Lance 1D10. Si le résultat est strictement inférieur à l'Initiative de l'unité, l'action réussit.
    /// </summary>
    public bool ResolveRiskyAction(Unit unit)
    {
        int roll = _diceRoller.RollD10();
        int targetNumber = unit.GetEffectiveInitiative(); // Peut inclure des bonus/malus temporaires si nécessaire

        return roll < targetNumber;
    }
}
