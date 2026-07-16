using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de la résolution des tests de moral.
/// </summary>
public class MoraleResolutionService
{
    private readonly IDiceRoller _diceRoller;

    public MoraleResolutionService(IDiceRoller diceRoller)
    {
        _diceRoller = diceRoller;
    }
    /// <summary>
    /// Résout un test de moral pour une unité.
    /// Lance 1D10. Si le résultat est inférieur ou égal au moral de l'unité, le test est réussi.
    /// Si échoué, l'unité gagne les statuts Demoralized et PinnedDown.
    /// </summary>
    /// <returns>True si le test est réussi, False s'il est échoué.</returns>
    public bool ResolveMoraleTest(Unit unit)
    {
        int roll = _diceRoller.RollD10();
        int targetNumber = unit.BaseProfile.Morale;

        bool passed = roll <= targetNumber;

        if (!passed)
        {
            unit.ApplyStatusEffect(StatusEffect.Demoralized | StatusEffect.PinnedDown);
        }

        return passed;
    }
}
