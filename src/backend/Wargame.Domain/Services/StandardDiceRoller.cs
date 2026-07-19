using System;

namespace Wargame.Domain.Services;

/// <summary>
/// Implémentation standard du lanceur de dés, utilisée en production.
/// </summary>
public class StandardDiceRoller : IDiceRoller
{
    public int RollD10()
    {
        return Random.Shared.Next(1, 11);
    }

    public int RollD6()
    {
        return Random.Shared.Next(1, 7);
    }
}
