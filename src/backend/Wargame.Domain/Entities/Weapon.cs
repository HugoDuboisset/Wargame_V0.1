using Wargame.Domain.Enums;
using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// Représente une arme (à distance ou de mêlée) pouvant être équipée par une figurine.
/// Encapsule ses caractéristiques offensives et ses traits spéciaux.
/// </summary>
public class Weapon : Entity
{
    public string Name { get; private set; }
    public WeaponProfile Profile { get; private set; }

    public WeaponType Type => Profile.Type;
    public bool IsRanged => Type == WeaponType.Ranged;
    public bool IsMelee => Type == WeaponType.Melee;

    public Weapon(Guid id, string name, WeaponProfile profile) : base(id)
    {
        Name = name;
        Profile = profile;
    }

    public bool HasTrait(WeaponTrait trait) => Profile.Traits.HasFlag(trait);

    // récupération du nombre d'attaques en fonction de la distance (pour les armes à rafales)
    public int GetEffectiveAttacks(double targetDistance)
    {
        var attacks = Profile.Attacks;
        if (HasTrait(WeaponTrait.Bursts) && Profile.Range > 0 && targetDistance <= Profile.Range / 2.0)
            attacks += 2;
        return attacks;
    }
}
