using System.Collections.Generic;
using Wargame.Domain.Services;

namespace Wargame.Domain.Tests.Services;

/// <summary>
/// Faux lanceur de dés pour les tests unitaires.
/// </summary>
public class MockDiceRoller : IDiceRoller
{
    private readonly Queue<int> _rolls = new();
    
    // Propriété de commodité comme dans ton POC (pour un seul jet)
    public int D10Result 
    { 
        set => _rolls.Enqueue(value); 
    }

    /// <summary>
    /// Constructeur permettant d'enchaîner plusieurs résultats de jets à l'avance.
    /// </summary>
    public MockDiceRoller(params int[] rolls)
    {
        foreach (var roll in rolls)
        {
            _rolls.Enqueue(roll);
        }
    }

    public int RollD10()
    {
        if (_rolls.Count > 0)
        {
            return _rolls.Dequeue();
        }
        
        // Valeur par défaut si on demande plus de jets que prévu
        return 1; 
    }

    public int RollD6()
    {
        if (_rolls.Count > 0)
        {
            return _rolls.Dequeue();
        }
        
        return 1;
    }
}
