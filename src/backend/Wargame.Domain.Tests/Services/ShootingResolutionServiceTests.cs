using FluentAssertions;
using Moq;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry;
using Wargame.Domain.ValueObjects.Geometry.Bases;

namespace Wargame.Domain.Tests.Services;

public class ShootingResolutionServiceTests
{
    private static readonly CircularBase StandardBase = new(12.5); // 25mm diameter

    private ShootingResolutionService CreateService(IDiceRoller? diceRoller = null)
    {
        return new ShootingResolutionService(diceRoller ?? new MockDiceRoller());
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
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(0, 0));
        
        var weaponProfile = new WeaponProfile(WeaponType.Ranged, weaponRange, weaponAttacks, 1, RangedWeaponCaliber.SmallCaliber, null, traits, explosiveHits);
        var weapon = new Weapon(Guid.NewGuid(), "Test Weapon", weaponProfile);
        
        var shootingUnit = new Unit(Guid.NewGuid(), "Shooter", UnitType.Infantry, shooterProfile, [shooter]);
        shootingUnit.SetMovement(movement);

        var targetProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        double radiusInches = StandardBase.RadiusInches;
        var targetFigure = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(targetDistance + (2 * radiusInches), 0));
        var targetUnit = new Unit(Guid.NewGuid(), "Target", UnitType.Infantry, targetProfile, [targetFigure]);

        return (shooter, shootingUnit, targetUnit, weapon);
    }

    [Fact]
    public void ResolveShot_Should_Return_Hits_When_Roll_Is_High_Enough()
    {
        var roller = new MockDiceRoller(4);
        var service = CreateService(roller);
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []); // CT 4, Jet 4 = Touche

        hits.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Not_Return_Hits_When_Roll_Is_Too_Low()
    {
        var roller = new MockDiceRoller(3);
        var service = CreateService(roller);
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []); // CT 4, Jet 3 = Rate

        hits.Should().BeEmpty();
    }

    [Fact]
    public void ResolveShot_Should_Apply_Sprint_Handy_Modifier()
    {
        var roller = new MockDiceRoller(5, 6);
        var service = CreateService(roller);
        // Mouvement Sprint, Arme Handy. Modificateur = -2. 
        // CT 4. Jet de 5 - 2 = 3 (Echec). Jet de 6 - 2 = 4 (Touche).
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            shootingSkill: 4, movement: MovementType.Sprint, traits: WeaponTrait.Handy);

        var hits1 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []);
        hits1.Should().BeEmpty();

        var hits2 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []);
        hits2.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Apply_Cover_Penalty()
    {
        var roller = new MockDiceRoller(6, 7);
        var service = CreateService(roller);
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4);

        // Terrain sur la cible (Occupation). Cover Heavy (-3).
        // CT 4. Jet de 6 - 3 = 3 (Echec). Jet de 7 - 3 = 4 (Touche).
        var shape = new Rectangle(targetUnit.Figures[0].Position, 100 / 25.4, 100 / 25.4, 0);
        var terrain = new Terrain(Guid.NewGuid(), "Ruines", shape, TerrainGeometry.Occupation, CoverLevel.Heavy);

        var hits1 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain]);
        hits1.Should().BeEmpty();

        var hits2 = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain]);
        hits2.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Ignore_Cover_When_Weapon_Has_IgnoreCover_Trait()
    {
        var roller = new MockDiceRoller(4);
        var service = CreateService(roller);
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            shootingSkill: 4, traits: WeaponTrait.IgnoreCover);

        var shape = new Rectangle(targetUnit.Figures[0].Position, 100 / 25.4, 100 / 25.4, 0);
        var terrain = new Terrain(Guid.NewGuid(), "Ruines", shape, TerrainGeometry.Occupation, CoverLevel.Heavy);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, [terrain]); // CT 4, Jet 4 = Touche, même dans Cover Heavy grâce au trait

        hits.Should().HaveCount(1);
    }

    [Fact]
    public void ResolveShot_Should_Generate_Multiple_Hits_With_Explosive_Trait()
    {
        var roller = new MockDiceRoller(4);
        var service = CreateService(roller);
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(shootingSkill: 4, traits: WeaponTrait.Explosive, explosiveHits: 2);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []); // 1 touche + 2 explosif = 3 touches

        hits.Should().HaveCount(3);
    }

    [Fact]
    public void ResolveShot_Should_Increase_Attacks_With_Bursts_Trait_At_Half_Range()
    {
        var roller = new MockDiceRoller(4, 5, 2); // 3 jets
        var service = CreateService(roller);
        // Arme portée 24", cible à 10" (donc à mi-portée ou moins)
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(weaponRange: 24.0, targetDistance: 10.0, traits: WeaponTrait.Bursts, weaponAttacks: 1);

        // 1 Attaque de base + 2 pour Bursts à mi-portée = 3 attaques. Jets: 4, 5, 2. (CT 4) -> 2 réussites.
        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []); 

        hits.Should().HaveCount(2);
    }

    [Fact]
    public void ResolveShot_Should_Change_Caliber_For_Buckshot_At_Half_Range()
    {
        var roller = new MockDiceRoller(4);
        var service = CreateService(roller);
        // Portée 24", Cible à 10", Trait Buckshot -> HeavyCaliber
        var (shooter, shootingUnit, targetUnit, weapon) = SetupScenario(
            weaponRange: 24.0, targetDistance: 10.0, traits: WeaponTrait.Buckshot);

        var hits = service.ResolveShot(shooter, shootingUnit, targetUnit, weapon, []);

        hits.Should().HaveCount(1);
        hits[0].Caliber.Should().Be(RangedWeaponCaliber.HeavyCaliber);
    }
}
