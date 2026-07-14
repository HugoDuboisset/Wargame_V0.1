using Wargame.Domain.Primitives;

namespace Wargame.Domain.Entities;

/// <summary>
/// Représente un joueur participant à la partie.
/// Maintient la liste de ses unités et accumule ses points de victoire.
/// </summary>
public class Player : Entity
{
    private readonly List<Guid> _unitIds = [];

    public string Name { get; private set; }

    /// <summary>Points de victoire accumulés (Kill Points dans le scénario Annihilation).</summary>
    public int VictoryPoints { get; private set; }

    public IReadOnlyList<Guid> UnitIds => _unitIds.AsReadOnly();

    public Player(Guid id, string name) : base(id)
    {
        Name = name;
        VictoryPoints = 0;
    }

    /// <summary>Enregistre une unité sous le contrôle de ce joueur.</summary>
    public void AddUnit(Guid unitId)
    {
        if (!_unitIds.Contains(unitId))
            _unitIds.Add(unitId);
    }

    /// <summary>Ajoute des points de victoire au score du joueur.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Si points est négatif.</exception>
    public void AddVictoryPoints(int points)
    {
        if (points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Victory points cannot be negative.");
        VictoryPoints += points;
    }
}
