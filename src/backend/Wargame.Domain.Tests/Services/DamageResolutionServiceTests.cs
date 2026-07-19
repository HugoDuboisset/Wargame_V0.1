using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.Services.Traits;
using Xunit;

namespace Wargame.Domain.Tests.Services;

public class DamageResolutionServiceTests
{
    private const int StandardBaseSizeMm = 25;

    private (Unit shootingUnit, Unit targetUnit) SetupScenario(
        int targetHitPoints = 1,
        int targetFigureCount = 5,
        ArmorClass targetArmor = ArmorClass.Light)
    {

        var shooterProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var targetProfile = new UnitProfile(6.0, 4, 4, 4, 7, targetArmor);

        var shooterFigure = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        var shootingUnit = new Unit(Guid.NewGuid(), "Shooter", UnitType.Infantry, shooterProfile, [shooterFigure]);

        var targetFigures = new List<Figure>();
        for (int i = 0; i < targetFigureCount; i++)
        {
            // Les cibles sont alignées sur l'axe X (distance de 10, 12, 14, etc.)
            targetFigures.Add(new Figure(Guid.NewGuid(), targetHitPoints, StandardBaseSizeMm, new Position(10 + (i * 2), 0)));
        }
        var targetUnit = new Unit(Guid.NewGuid(), "Target", UnitType.Infantry, targetProfile, targetFigures);

        return (shootingUnit, targetUnit);
    }

    [Fact]
    public void ResolveWoundsAndApplyDamage_Should_Convert_Hits_To_Wounds_Based_On_Armor()
    {
        var (shootingUnit, targetUnit) = SetupScenario(targetArmor: ArmorClass.Light); // TN vs SmallCaliber = 7+
        var roller = new MockDiceRoller(7, 8, 6);
        var strategies = new List<IWeaponTraitStrategy> { new SuppressionTraitStrategy(), new IncendiaryTraitStrategy() };
        var service = new DamageResolutionService(roller, strategies);

        // 3 touches : 2 réussites (7, 8) et 1 échec (6)
        var hits = new List<Hit> { 
            new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.None),
            new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.None),
            new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.None)
        };

        var (wounds, destroyed) = service.ResolveWoundsAndApplyDamage(hits, shootingUnit, targetUnit, []);

        wounds.Should().Be(2);
        destroyed.Should().Be(2); // Cibles 1 PV, 2 blessures = 2 morts
        targetUnit.GetAliveCount().Should().Be(3); 
    }

    [Fact]
    public void ResolveWoundsAndApplyDamage_Should_Remove_Closest_Figures_First()
    {
        var (shootingUnit, targetUnit) = SetupScenario();
        var roller = new MockDiceRoller(10);
        var strategies = new List<IWeaponTraitStrategy> { new SuppressionTraitStrategy(), new IncendiaryTraitStrategy() };
        var service = new DamageResolutionService(roller, strategies);
        
        var hits = new List<Hit> { new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.None) };
        service.ResolveWoundsAndApplyDamage(hits, shootingUnit, targetUnit, []); // Jet 10 = blessure auto

        // Vérifie que c'est bien la figure à X=10 (index 0) qui est morte
        targetUnit.Figures[0].IsAlive.Should().BeFalse();
        targetUnit.Figures[1].IsAlive.Should().BeTrue();
    }

    [Fact]
    public void ResolveWoundsAndApplyDamage_Should_Apply_Status_Effects()
    {
        var (shootingUnit, targetUnit) = SetupScenario();
        var roller = new MockDiceRoller(1, 1);
        var strategies = new List<IWeaponTraitStrategy> { new SuppressionTraitStrategy(), new IncendiaryTraitStrategy() };
        var service = new DamageResolutionService(roller, strategies);

        var hits = new List<Hit> { 
            new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.Suppression),
            new Hit(RangedWeaponCaliber.SmallCaliber, 1, WeaponTrait.Incendiary)
        };

        // Jet 1 = échec pour blesser
        service.ResolveWoundsAndApplyDamage(hits, shootingUnit, targetUnit, []);

        // Même si ça ne blesse pas, les statuts doivent être appliqués car l'unité a été touchée
        targetUnit.ActiveStatusEffects.HasFlag(StatusEffect.Suppressed).Should().BeTrue();
        targetUnit.ActiveStatusEffects.HasFlag(StatusEffect.OnFire).Should().BeTrue();
    }

    [Fact]
    public void ResolveWoundsAndApplyDamage_Should_Not_SpillOver_Excess_Damage()
    {
        // Cibles avec 1 PV
        var (shootingUnit, targetUnit) = SetupScenario(targetHitPoints: 1, targetFigureCount: 5);
        var roller = new MockDiceRoller(10);
        var strategies = new List<IWeaponTraitStrategy> { new SuppressionTraitStrategy(), new IncendiaryTraitStrategy() };
        var service = new DamageResolutionService(roller, strategies);

        // 1 Touche avec 5 dégâts !
        var hits = new List<Hit> { new Hit(RangedWeaponCaliber.AntiTank, 5, WeaponTrait.None) };
        var (wounds, destroyed) = service.ResolveWoundsAndApplyDamage(hits, shootingUnit, targetUnit, []);

        wounds.Should().Be(1);
        destroyed.Should().Be(1); // Seulement 1 mort. Les 4 autres dégâts sont perdus.
        targetUnit.GetAliveCount().Should().Be(4);
    }
}
