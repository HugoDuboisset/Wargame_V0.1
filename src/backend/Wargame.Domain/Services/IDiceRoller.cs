namespace Wargame.Domain.Services;

/// <summary>
/// Interface pour lancer les dés. 
/// Permet d'injecter des résultats déterministes dans les tests unitaires.
/// </summary>
public interface IDiceRoller
{
    /// <summary>
    /// Lance 1D10 et retourne le résultat (de 1 à 10).
    /// </summary>
    int RollD10();
}
