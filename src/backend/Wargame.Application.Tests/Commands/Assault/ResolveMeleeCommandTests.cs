using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Assault;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry.Bases;
using Xunit;
using DomainMatch = Wargame.Domain.Entities.GameMatch;

namespace Wargame.Application.Tests.Commands.Assault;

public class ResolveMeleeCommandTests
{
    private static readonly CircularBase StandardBase = new(12.5);
    private readonly Mock<IGameMatchRepository> _repositoryMock = new();

    private (DomainMatch match, Unit u1, Unit u2) CreateEngagedUnits(
        WeaponTrait u1WeaponTrait = WeaponTrait.None,
        WeaponTrait u2WeaponTrait = WeaponTrait.None,
        int u1Combat = 4, int u2Combat = 4,
        int u1Init = 4, int u2Init = 4,
        bool u1Charging = false)
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new DomainMatch(Guid.NewGuid(), [player1, player2], board);

        var wp1 = new WeaponProfile(WeaponType.Melee, 1, 1, 1, null, MeleeWeaponCategory.Light, u1WeaponTrait);
        var wp2 = new WeaponProfile(WeaponType.Melee, 1, 1, 1, null, MeleeWeaponCategory.Light, u2WeaponTrait);

        var weapon1 = new Weapon(Guid.NewGuid(), "Weapon 1", wp1);
        var weapon2 = new Weapon(Guid.NewGuid(), "Weapon 2", wp2);

        var p1 = new UnitProfile(6, 4, u1Combat, u1Init, 7, ArmorClass.Light);
        var p2 = new UnitProfile(6, 4, u2Combat, u2Init, 7, ArmorClass.Light); // Morale 7, Init 4

        var f1 = new Figure(Guid.NewGuid(), 2, StandardBase, new Position(0, 0), meleeWeapons: [weapon1]);
        var f2 = new Figure(Guid.NewGuid(), 2, StandardBase, new Position(0.5, 0), meleeWeapons: [weapon2]); // 2 HP

        var u1 = new Unit(Guid.NewGuid(), "Unit 1", UnitType.Infantry, p1, [f1]);
        var u2 = new Unit(Guid.NewGuid(), "Unit 2", UnitType.Infantry, p2, [f2]);

        u1.EngageWith(u2.Id);
        u2.EngageWith(u1.Id);

        match.AddUnit(u1);
        match.AddUnit(u2);
        player1.AddUnit(u1.Id);
        player2.AddUnit(u2.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(match);

        return (match, u1, u2);
    }

    private static IDiceRoller Roller(params int[] rolls)
    {
        var mock = new Moq.Mock<IDiceRoller>();
        var queue = new Queue<int>(rolls);
        mock.Setup(d => d.RollD10()).Returns(() => queue.Count > 0 ? queue.Dequeue() : 1);
        return mock.Object;
    }

    private ResolveMeleeCommandHandler CreateHandler(IDiceRoller roller)
    {
        var resolutionService = new AssaultResolutionService(roller);
        var moraleService = new MoraleResolutionService(roller);
        return new ResolveMeleeCommandHandler(_repositoryMock.Object, resolutionService, moraleService, new Wargame.Domain.Services.UnitCohesionService());
    }

    [Fact]
    public async Task Handle_Should_Apply_Wounds_And_Identify_Loser()
    {
        var (match, u1, u2) = CreateEngagedUnits();
        // U1 hit(8), U2 miss(2), U1 wound(6)
        var r = Roller(8, 2, 6); 

        var result = await CreateHandler(r).Handle(
            new ResolveMeleeCommand(match.Id, [u1.Id, u2.Id]), CancellationToken.None);

        result.WoundsLostPerUnit[u2.Id].Should().Be(1);
        result.LoserUnitId.Should().Be(u2.Id);
    }

    [Fact]
    public async Task Handle_Should_Fail_Morale_And_Gain_Routing_Status()
    {
        var (match, u1, u2) = CreateEngagedUnits();
        
        // Sequence:
        // 1. Melee phase: U1 hits, U2 misses, U1 wounds -> 1 dmg to u2
        // 2. Morale test for u2: Needs <= 7. We roll 10 -> Fail.
        
        var r = Roller(
            8, 2, 6, // Melee Phase: U1 hit, U2 miss, U1 wound
            10       // Morale test: 10 (Fail)
        );

        var result = await CreateHandler(r).Handle(
            new ResolveMeleeCommand(match.Id, [u1.Id, u2.Id]), CancellationToken.None);

        result.LoserUnitId.Should().Be(u2.Id);
        result.MoraleFailed.Should().BeTrue();
        
        u2.IsDemoralized().Should().BeTrue();
        u2.ActiveStatusEffects.HasFlag(StatusEffect.Routing).Should().BeTrue();
        u2.IsEngaged().Should().BeTrue(); // They stay engaged until the physical disengage movement
    }
}
