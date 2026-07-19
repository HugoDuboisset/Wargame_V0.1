using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry.Bases;

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

    /// <summary>Forme géométrique du socle (Cercle pour l'infanterie, Rectangle pour les véhicules).</summary>
    public IBaseShape BaseShape { get; private set; }

    /// <summary>
    /// Orientation de la figurine en degrés.
    /// Uniquement significatif pour les socles rectangulaires (véhicules).
    /// Convention : 0° = face avant vers l'axe X+, positif = sens antihoraire.
    /// </summary>
    public double OrientationDegrees { get; private set; }

    public Position Position { get; private set; }

    public IReadOnlyList<Weapon> RangedWeapons => _rangedWeapons.AsReadOnly();
    public IReadOnlyList<Weapon> MeleeWeapons => _meleeWeapons.AsReadOnly();

    public bool IsAlive => CurrentHitPoints > 0;

    public Figure(
        Guid id,
        int maxHitPoints,
        IBaseShape baseShape,
        Position position,
        double orientationDegrees = 0,
        IReadOnlyList<Weapon>? rangedWeapons = null,
        IReadOnlyList<Weapon>? meleeWeapons = null) : base(id)
    {
        if (maxHitPoints <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxHitPoints), "Max hit points must be positive.");

        MaxHitPoints = maxHitPoints;
        CurrentHitPoints = maxHitPoints;
        BaseShape = baseShape ?? throw new ArgumentNullException(nameof(baseShape));
        Position = position;
        OrientationDegrees = orientationDegrees;

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

    public void MoveTo(Position newPosition, double? newOrientationDegrees = null)
    {
        Position = newPosition;
        if (newOrientationDegrees.HasValue)
            OrientationDegrees = newOrientationDegrees.Value;
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
    /// Supporte les socles circulaires et rectangulaires via IBaseShape.
    /// Une valeur négative ou nulle signifie que les socles sont en contact ou chevauchants.
    /// </summary>
    public double GetEdgeDistanceTo(Figure other)
    {
        return BaseShape.GetShortestDistanceTo(
            Position, OrientationDegrees,
            other.BaseShape, other.Position, other.OrientationDegrees);
    }

    /// <summary>
    /// Calcule la distance bord à bord entre cette figurine et un socle cible à une position donnée.
    /// Utile pour valider un déplacement avant de l'appliquer.
    /// </summary>
    public double GetEdgeDistanceToPosition(Position targetCenter, IBaseShape targetShape, double targetOrientation = 0)
    {
        return BaseShape.GetShortestDistanceTo(
            Position, OrientationDegrees,
            targetShape, targetCenter, targetOrientation);
    }
}
