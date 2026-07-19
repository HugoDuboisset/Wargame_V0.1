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
    /// Résout un test de moral suite à un tir (50% de pertes).
    /// </summary>
    public bool ResolveMoraleTest(Unit unit)
    {
        return PerformTestAndApplyEffects(unit, 0, StatusEffect.Demoralized | StatusEffect.PinnedDown);
    }

    /// <summary>
    /// Résout un test de moral suite à la perte d'un combat au corps à corps.
    /// Si Brutal est true, le test subit un malus de -1 (le seuil de réussite baisse de 1).
    /// Si échoué, l'unité est Demoralized et Routing.
    /// </summary>
    public bool ResolveMeleeMoraleTest(Unit unit, bool isBrutal)
    {
        int modifier = isBrutal ? -1 : 0;
        return PerformTestAndApplyEffects(unit, modifier, StatusEffect.Demoralized | StatusEffect.Routing);
    }

    private bool PerformTestAndApplyEffects(Unit unit, int modifier, StatusEffect failedEffects)
    {
        int roll = _diceRoller.RollD10();
        int targetNumber = unit.BaseProfile.Morale + modifier;

        bool passed = roll <= targetNumber;

        if (!passed)
        {
            unit.ApplyStatusEffect(failedEffects);
        }

        return passed;
    }
}
