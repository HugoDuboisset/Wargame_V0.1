using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// Représente une figurine individuelle composant une unité.
/// C'est l'élément physique pouvant subir des dégâts ou être retiré comme perte.
/// Chaque figurine a sa propre position sur la table (nécessaire pour la vérification de cohésion à 2").
/// </summary>
public class Figure : Entity
{
    private readonly List<Weapon> _rangedWeapons = [];
    private readonly List<Weapon> _meleeWeapons = [];

    public int CurrentHitPoints { get; private set; }

    public int MaxHitPoints { get; private set; }

    public int BaseSizeMm { get; private set; }

    public Position Position { get; private set; }

    public IReadOnlyList<Weapon> RangedWeapons => _rangedWeapons.AsReadOnly();
    public IReadOnlyList<Weapon> MeleeWeapons => _meleeWeapons.AsReadOnly();

    public bool IsAlive => CurrentHitPoints > 0;

    public Figure(
        Guid id,
        int maxHitPoints,
        int baseSizeMm,
        Position position,
        IReadOnlyList<Weapon>? rangedWeapons = null,
        IReadOnlyList<Weapon>? meleeWeapons = null) : base(id)
    {
        if (maxHitPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHitPoints), "Max hit points must be positive.");

        MaxHitPoints = maxHitPoints;
        CurrentHitPoints = maxHitPoints;
        BaseSizeMm = baseSizeMm;
        Position = position;

        if (rangedWeapons != null) _rangedWeapons.AddRange(rangedWeapons);
        if (meleeWeapons != null) _meleeWeapons.AddRange(meleeWeapons);
    }

    /// <returns>True si la figurine est détruite (PV = 0) suite à ces dégâts.</returns>
    public bool TakeDamage(int damage)
    {
        if (damage < 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");

        CurrentHitPoints = Math.Max(0, CurrentHitPoints - damage);
        return !IsAlive;
    }

    public void MoveTo(Position newPosition)
    {
        Position = newPosition;
    }
}
