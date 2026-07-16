using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services.Traits;

public class SuppressionTraitStrategy : IWeaponTraitStrategy
{
    public WeaponTrait TargetTrait => WeaponTrait.Suppression;

    public void ApplyEffect(Unit targetUnit, Hit hit)
    {
        targetUnit.ApplyStatusEffect(StatusEffect.Suppressed);
    }
}
