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

    public void AddRangedWeapon(Weapon weapon)
    {
        if (weapon == null) throw new ArgumentNullException(nameof(weapon));
        _rangedWeapons.Add(weapon);
    }

    public void AddMeleeWeapon(Weapon weapon)
    {
        if (weapon == null) throw new ArgumentNullException(nameof(weapon));
        _meleeWeapons.Add(weapon);
    }

    /// <summary>
    /// Calcule la distance bord à bord entre cette figurine et une autre.
    /// Les socles sont circulaires ; les rayons sont convertis de mm en pouces (÷ 25.4).
    /// Une valeur négative ou nulle signifie que les socles sont en contact ou chevauchants.
    /// </summary>
    public double GetEdgeDistanceTo(Figure other)
    {
        var centreToCentre = Position.DistanceTo(other.Position);
        var radiusA = (BaseSizeMm / 2.0) / 25.4;
        var radiusB = (other.BaseSizeMm / 2.0) / 25.4;
        return centreToCentre - radiusA - radiusB;
    }

    /// <summary>
    /// Calcule la distance bord à bord entre cette figurine et une position de centre cible,
    /// en supposant que l'autre figurine a le même BaseSizeMm.
    /// Utile pour valider un déplacement avant de l'appliquer.
    /// </summary>
    public double GetEdgeDistanceToPosition(Position targetCenter, int targetBaseSizeMm)
    {
        var centreToCentre = Position.DistanceTo(targetCenter);
        var radiusA = (BaseSizeMm / 2.0) / 25.4;
        var radiusB = (targetBaseSizeMm / 2.0) / 25.4;
        return centreToCentre - radiusA - radiusB;
    }
}
