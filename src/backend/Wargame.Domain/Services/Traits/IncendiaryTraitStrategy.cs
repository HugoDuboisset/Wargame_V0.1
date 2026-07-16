using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services.Traits;

public class IncendiaryTraitStrategy : IWeaponTraitStrategy
{
    public WeaponTrait TargetTrait => WeaponTrait.Incendiary;

    public void ApplyEffect(Unit targetUnit, Hit hit)
    {
        targetUnit.ApplyStatusEffect(StatusEffect.OnFire);
    }
}
