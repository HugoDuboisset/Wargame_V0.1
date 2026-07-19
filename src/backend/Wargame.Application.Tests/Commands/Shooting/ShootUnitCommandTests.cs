using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Shooting;
using Wargame.Application.Commands.Shooting.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry.Bases;

namespace Wargame.Application.Tests.Commands.Shooting;

public class ShootUnitCommandTests
{
    private static readonly CircularBase StandardBase = new(12.5); // 25mm diameter

    private readonly Mock<IGameMatchRepository> _repositoryMock = new();

    private (Wargame.Domain.Entities.GameMatch match, Unit shootingUnit, Unit targetUnit, Weapon weapon) CreateMatchWithUnits(
        double distanceInches = 10.0,
        double weaponRange = 24.0,
        WeaponTrait traits = WeaponTrait.None,
        UnitType shooterType = UnitType.Infantry,
        MovementType shooterMovement = MovementType.None,
        bool shooterEngaged = false)
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), [player1, player2], board);

        // Tireur en (0,0)
        var shooterProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var weaponProfile = new WeaponProfile(WeaponType.Ranged, weaponRange, 1, 1, RangedWeaponCaliber.SmallCaliber, null, traits);
        var weapon = new Weapon(Guid.NewGuid(), "Test Weapon", weaponProfile);
        
        var shooterFigure = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(0, 0), rangedWeapons: [weapon]);

        var shootingUnit = new Unit(Guid.NewGuid(), "Shooter", shooterType, shooterProfile, [shooterFigure]);
        
        if (shooterMovement != MovementType.None)
        {
            shootingUnit.Move([], shooterMovement); // Applique le statut de mouvement
        }

        // Cible en (distanceInches + diametre du socle pour avoir la distance bord a bord exacte, mais simplifié ici)
        // Distance bord à bord = centre à centre - rayonA - rayonB
        // Si je veux que getEdgeDistanceTo = distanceInches, je dois positionner le centre à :
        // distanceInches + (2 * rayon)
        double radiusInches = StandardBase.RadiusInches;
        double centerDistance = distanceInches + (2 * radiusInches);
        
        var targetProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var targetFigure = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(centerDistance, 0));
        var targetUnit = new Unit(Guid.NewGuid(), "Target", UnitType.Infantry, targetProfile, [targetFigure]);

        if (shooterEngaged)
        {
            shootingUnit.EngageWith(targetUnit.Id);
            targetUnit.EngageWith(shootingUnit.Id);
        }

        match.AddUnit(shootingUnit);
        match.AddUnit(targetUnit);
        player1.AddUnit(shootingUnit.Id);
        player2.AddUnit(targetUnit.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        return (match, shootingUnit, targetUnit, weapon);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Target_Out_Of_Range()
    {
        // Portée de l'arme = 10", distance cible = 12"
        var (match, shooter, target, weapon) = CreateMatchWithUnits(distanceInches: 12.0, weaponRange: 10.0);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hors de portée*");
    }

    [Fact]
    public async Task Handle_Should_Allow_Shooting_When_In_Range()
    {
        // Portée de l'arme = 24", distance cible = 10"
        var (match, shooter, target, weapon) = CreateMatchWithUnits(distanceInches: 10.0, weaponRange: 24.0);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        shooter.HasFired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Cumbersome_Weapon_And_Unit_Moved()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterMovement: MovementType.Normal, 
            traits: WeaponTrait.Cumbersome);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Encombrante*");
    }

    [Fact]
    public async Task Handle_Should_Allow_Cumbersome_Weapon_When_Unit_Is_Vehicle_And_Moved()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterType: UnitType.Vehicle,
            shooterMovement: MovementType.Normal, 
            traits: WeaponTrait.Cumbersome);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Sprint_And_Weapon_Not_Handy()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterMovement: MovementType.Sprint);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maniables*");
    }

    [Fact]
    public async Task Handle_Should_Allow_Sprint_When_Weapon_Is_Handy()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterMovement: MovementType.Sprint,
            traits: WeaponTrait.Handy);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Engaged_And_Weapon_Not_Pistol()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterEngaged: true);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pistolet*");
    }

    [Fact]
    public async Task Handle_Should_Allow_Engaged_When_Weapon_Is_Pistol_And_Target_Is_Engaged()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterEngaged: true,
            traits: WeaponTrait.Pistol);

        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, target.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Engaged_And_Weapon_Is_Pistol_But_Target_Not_Engaged()
    {
        var (match, shooter, target, weapon) = CreateMatchWithUnits(
            shooterEngaged: true, // shooter est engagé avec "target"
            traits: WeaponTrait.Pistol);

        // Création d'une autre cible NON engagée avec le tireur
        var otherTargetProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var otherTargetFigure = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(10, 10));
        var otherTarget = new Unit(Guid.NewGuid(), "Other Target", UnitType.Infantry, otherTargetProfile, [otherTargetFigure]);
        match.AddUnit(otherTarget);
        
        var command = new ShootUnitCommand(match.Id, shooter.Id, [
            new FigureShootDto(shooter.Figures[0].Id, weapon.Id, otherTarget.Id)
        ]);

        var diceRollerMock = new Moq.Mock<Wargame.Domain.Services.IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(10); // par défaut on force un jet de 10 pour que ça touche et blesse
        var diceRoller = diceRollerMock.Object;
        var handler = new ShootUnitCommandHandler(
            _repositoryMock.Object, 
            new Wargame.Domain.Services.ShootingResolutionService(diceRoller), 
            new Wargame.Domain.Services.ShootingValidationService(new List<Wargame.Domain.Specifications.Shooting.IShootingValidationSpec> { new Wargame.Domain.Specifications.Shooting.EngagementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.MovementConstraintsSpec(), new Wargame.Domain.Specifications.Shooting.RangeSpec(), new Wargame.Domain.Specifications.Shooting.LineOfSightSpec() }), 
            new Wargame.Domain.Services.DamageResolutionService(diceRoller, new List<Wargame.Domain.Services.Traits.IWeaponTraitStrategy> { new Wargame.Domain.Services.Traits.SuppressionTraitStrategy(), new Wargame.Domain.Services.Traits.IncendiaryTraitStrategy() }), 
            new Wargame.Domain.Services.MoraleResolutionService(diceRoller),
            new Wargame.Domain.Services.UnitCohesionService());
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cibler que l'unité avec laquelle elle est engagée*");
    }
}
