using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Movement;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using DomainUnit = Wargame.Domain.Entities.Unit;

namespace Wargame.Application.Tests.Commands.Movement;

public class DisengageUnitCommandTests
{
    private readonly Mock<IGameMatchRepository> _repositoryMock;

    public DisengageUnitCommandTests()
    {
        _repositoryMock = new Mock<IGameMatchRepository>();
    }

    private IDiceRoller Roller(params int[] rolls)
    {
        var mock = new Mock<IDiceRoller>();
        var queue = new Queue<int>(rolls);
        mock.Setup(d => d.RollD10()).Returns(() => queue.Count > 0 ? queue.Dequeue() : 1);
        return mock.Object;
    }

    private DisengageUnitCommandHandler CreateHandler(IDiceRoller roller)
    {
        var actionService = new ActionResolutionService(roller);
        var assaultService = new AssaultResolutionService(roller);
        var withdrawalService = new WithdrawalResolutionService(actionService, assaultService);
        return new DisengageUnitCommandHandler(_repositoryMock.Object, withdrawalService, new Wargame.Domain.Services.UnitCohesionService());
    }

    private (Wargame.Domain.Entities.GameMatch, DomainUnit, DomainUnit) CreateEngagedUnits()
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), [player1, player2], board);

        var wp1 = new WeaponProfile(WeaponType.Melee, 1, 1, 1, null, MeleeWeaponCategory.Light, WeaponTrait.Reach);
        var weapon1 = new Weapon(Guid.NewGuid(), "Weapon 1", wp1);

        var p1 = new UnitProfile(6, 4, 4, 4, 7, ArmorClass.Light);

        // U1 at 10,10. Left edge distance = 10. Bottom edge distance = 10.
        var f1 = new Figure(Guid.NewGuid(), 2, 25, new Position(10, 10), null, [weapon1]);
        var f2 = new Figure(Guid.NewGuid(), 2, 25, new Position(11, 10), null, [weapon1]); // ~1"

        var u1 = new DomainUnit(Guid.NewGuid(), "Unit 1", UnitType.Infantry, p1, [f1]);
        var u2 = new DomainUnit(Guid.NewGuid(), "Unit 2", UnitType.Infantry, p1, [f2]);

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

    [Fact]
    public async Task Handle_Voluntary_ShouldFailRiskyAction_And_TakeOppAttacks()
    {
        var (match, u1, u2) = CreateEngagedUnits();
        
        var dto = new FigureMoveDto(u1.Figures.First().Id, 16, 10);
        var cmd = new DisengageUnitCommand(match.Id, u1.Id, [dto]);

        var r = Roller(8, 9, 7);
        var handler = CreateHandler(r);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.RiskyActionFailed.Should().BeTrue();
        result.OpportunityAttacksWounds.Should().Be(1);
        
        u1.LifecycleStatus.Should().Be(UnitLifecycleStatus.Alive);
        
        u1.IsEngaged().Should().BeFalse();
        u2.IsEngaged().Should().BeFalse();
        
        u1.Figures.First().Position.X.Should().BeApproximately(16, 0.01);
    }
}
