namespace Wargame.Domain.Services;

/// <summary>
/// Matrice pour déterminer le score à atteindre sur 1D10 pour toucher au corps à corps.
/// Basée sur la différence entre le Combat de l'attaquant et le Combat du défenseur.
/// 
/// Différence = Combat(Attaquant) - Combat(Défenseur)
/// </summary>
public static class MeleeHitMatrix
{
    /// <summary>
    /// Retourne le résultat nécessaire sur 1D10 pour toucher au corps à corps.
    /// Un 1 naturel est toujours un échec (géré par le resolveur).
    /// </summary>
    public static int GetTargetNumber(int attackerCombat, int defenderCombat)
    {
        int difference = attackerCombat - defenderCombat;

        if (difference >= 3) return 3;
        if (difference >= 1) return 4;
        if (difference == 0) return 5;
        if (difference >= -2) return 6;
        return 7; // Différence <= -3
    }
}
