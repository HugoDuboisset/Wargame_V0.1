using FluentAssertions;
using Moq;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Tests.Services;

public class ShootingResolutionServiceTests
{
    private const int StandardBaseSizeMm = 25;

    private ShootingResolutionService CreateService()
    {
        return new ShootingResolutionService();
    }

    private (Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon) SetupScenario(
        double targetDistance = 10.0,
        double weaponRange = 24.0,
        int weaponAttacks = 1,
        WeaponTrait traits = WeaponTrait.None,
        int shootingSkill = 4,
        MovementType movement = MovementType.None,
        int explosiveHits = 0)
    {
        var shooterProfile = new UnitProfile(6.0, shootingSkill, 4, 4, 7, ArmorClass.Light);
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        
        var weaponProfile = new WeaponProfile(WeaponType.Ranged, weaponRange, weaponAttacks, 1, RangedWeaponCaliber.SmallCaliber, null, traits, explosiveHits);
        var weapon = new Weapon(Guid.NewGuid(), "Test Weapon", weaponProfile);
        
        var shootingUnit = new Unit(Guid.NewGuid(), "Shooter", UnitType.Infantry, shooterProfile, [shooter]);
        shootingUnit.SetMovement(movement);

        var targetProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        double radiusInches = (StandardBaseSizeMm / 2.0) / 25.4;
        var targetFigure = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(targetDistance + (2 * radiusInches), 0));
        var targetUnit = new Unit(Guid.NewGuid(), "Target", UnitType.Infantry, targetProfile, [targetFigure]);

        return (shooter, shootingUnit, targetUnit, weapon);
    }

    [Fact]
    public void ResolveShot_Should_Return_Hits_When_Roll_Is_High_Enough()
    {
        var service = CreateService();
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [4]); // CT 4, Jet 4 = Touche

        hits.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Return_Empty_When_Roll_Is_Too_Low()
    {
        var service = CreateService();
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [3]); // CT 4, Jet 3 = Echec

        hits.Should().BeEmpty();
    }

    [Fact]
    public void ResolveShot_Should_Apply_Sprint_Handy_Modifier()
    {
        var service = CreateService();
        // Mouvement Sprint, Arme Handy. Modificateur = -2. 
        // CT 4. Jet de 5 - 2 = 3 (Echec). Jet de 6 - 2 = 4 (Touche).
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            shootingSkill: 4, movement: MovementType.Sprint, traits: WeaponTrait.Handy);

        var hits1 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [5]);
        hits1.Should().BeEmpty();

        var hits2 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [6]);
        hits2.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Apply_Cover_Penalty()
    {
        var service = CreateService();
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        // Terrain sur la cible (Occupation). Cover Heavy (-3).
        // CT 4. Jet de 6 - 3 = 3 (Echec). Jet de 7 - 3 = 4 (Touche).
        var terrain = new Terrain(Guid.NewGuid(), "Ruines", targetUnit.Figures[0].Position,
            100, 100, 0, TerrainGeometry.Occupation, CoverLevel.Heavy);

        var hits1 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain], [6]);
        hits1.Should().BeEmpty();

        var hits2 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain], [7]);
        hits2.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Ignore_Cover_When_Weapon_Has_IgnoreCover_Trait()
    {
        var service = CreateService();
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            shootingSkill: 4, traits: WeaponTrait.IgnoreCover);

        var terrain = new Terrain(Guid.NewGuid(), "Ruines", targetUnit.Figures[0].Position,
            100, 100, 0, TerrainGeometry.Occupation, CoverLevel.Heavy);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain], [4]); // CT 4, Jet 4 = Touche, même dans Cover Heavy grâce au trait

        hits.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Add_Explosive_Hits()
    {
        var service = CreateService();
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            shootingSkill: 4, traits: WeaponTrait.Explosive, explosiveHits: 2); // 1 touche + 2 extra = 3

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [4]);

        hits.Should().HaveCount(3);
    }

    [Fact]
    public void ResolveShot_Should_Add_Bursts_Attacks_At_Half_Range()
    {
        var service = CreateService();
        // Portée 24", Cible à 10" (<= 12"), Trait Bursts -> +2 attaques
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            weaponRange: 24.0, targetDistance: 10.0, weaponAttacks: 1, traits: WeaponTrait.Bursts);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [4, 4, 3]); // 3 dés lancés : 2 touches, 1 échec

        hits.Should().HaveCount(2);
    }

    [Fact]
    public void ResolveShot_Should_Change_Caliber_For_Buckshot_At_Half_Range()
    {
        var service = CreateService();
        // Portée 24", Cible à 10", Trait Buckshot -> HeavyCaliber
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            weaponRange: 24.0, targetDistance: 10.0, traits: WeaponTrait.Buckshot);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [], [4]);

        hits.Should().HaveCount(1);
        hits[0].Caliber.Should().Be(RangedWeaponCaliber.HeavyCaliber);
    }
}
