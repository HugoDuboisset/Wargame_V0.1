using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Assault;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Xunit;

namespace Wargame.Application.Tests.Commands.Assault;

public class ChargeUnitCommandTests
{
    private const int BaseSizeMm = 25;
    private readonly Mock<IGameMatchRepository> _repositoryMock = new();

    private (Wargame.Domain.Entities.GameMatch match, Unit chargingUnit, Unit targetUnit) CreateMatchWithUnits(
        double targetEdgeDistanceInches = 6.0,
        double chargingUnitMovement = 6.0,
        MovementType chargerMovement = MovementType.None)
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), [player1, player2], board);

        // Chargeur en (0,0), M=chargingUnitMovement"
        var chargerProfile = new UnitProfile(chargingUnitMovement, 4, 4, 4, 7, ArmorClass.Light);
        var chargerFigure = new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(0, 0));
        var chargingUnit = new Unit(Guid.NewGuid(), "Charger", UnitType.Infantry, chargerProfile, [chargerFigure]);
        if (chargerMovement != MovementType.None) chargingUnit.SetMovement(chargerMovement);

        // Cible placée à targetEdgeDistanceInches bord-à-bord
        double radiusInches = (BaseSizeMm / 2.0) / 25.4;
        double centerDistance = targetEdgeDistanceInches + (2 * radiusInches);
        var targetProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var targetFigure = new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(centerDistance, 0));
        var targetUnit = new Unit(Guid.NewGuid(), "Target", UnitType.Infantry, targetProfile, [targetFigure]);

        match.AddUnit(chargingUnit);
        match.AddUnit(targetUnit);
        player1.AddUnit(chargingUnit.Id);
        player2.AddUnit(targetUnit.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(match);

        return (match, chargingUnit, targetUnit);
    }

    private ChargeUnitCommandHandler CreateHandler(IDiceRoller diceRoller)
    {
        return new ChargeUnitCommandHandler(
            _repositoryMock.Object,
            new AssaultValidationService(),
            new AssaultMovementService(),
            diceRoller,
            new Wargame.Domain.Services.UnitCohesionService());
    }

    private static IDiceRoller Roller(params int[] rolls)
    {
        var mock = new Moq.Mock<IDiceRoller>();
        var queue = new Queue<int>(rolls);
        mock.Setup(d => d.RollD6()).Returns(() => queue.Count > 0 ? queue.Dequeue() : 1);
        mock.Setup(d => d.RollD10()).Returns(() => queue.Count > 0 ? queue.Dequeue() : 1);
        return mock.Object;
    }

    [Fact]
    public async Task Handle_Should_Succeed_When_ChargeRoll_Reaches_Target()
    {
        // Cible à 6" bord-à-bord, M=6, D6=4 → distance = 4+6 = 10" → atteint
        var (match, chargingUnit, targetUnit) = CreateMatchWithUnits(targetEdgeDistanceInches: 6.0, chargingUnitMovement: 6.0);

        var result = await CreateHandler(Roller(4)).Handle(
            new ChargeUnitCommand(match.Id, chargingUnit.Id, targetUnit.Id), CancellationToken.None);

        result.ChargingSucceeded.Should().BeTrue();
        result.ChargeRoll.Should().Be(4);
        result.ChargeDistance.Should().Be(10.0);

        chargingUnit.IsEngaged().Should().BeTrue();
        targetUnit.IsEngaged().Should().BeTrue();
        chargingUnit.ActiveStatusEffects.HasFlag(StatusEffect.Charging).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_Fail_When_ChargeRoll_Does_Not_Reach_Target()
    {
        // Cible à 12" bord-à-bord, M=6, D6=1 → distance = 1+6 = 7" → n'atteint pas
        var (match, chargingUnit, targetUnit) = CreateMatchWithUnits(targetEdgeDistanceInches: 12.0, chargingUnitMovement: 6.0);

        var result = await CreateHandler(Roller(1)).Handle(
            new ChargeUnitCommand(match.Id, chargingUnit.Id, targetUnit.Id), CancellationToken.None);

        result.ChargingSucceeded.Should().BeFalse();
        result.ChargeRoll.Should().Be(1);

        chargingUnit.IsEngaged().Should().BeFalse();
        targetUnit.IsEngaged().Should().BeFalse();
        chargingUnit.ActiveStatusEffects.HasFlag(StatusEffect.Charging).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Has_Sprinted()
    {
        var (match, chargingUnit, targetUnit) = CreateMatchWithUnits();
        chargingUnit.SetMovement(MovementType.Sprint);

        Func<Task> act = () => CreateHandler(Roller(6)).Handle(
            new ChargeUnitCommand(match.Id, chargingUnit.Id, targetUnit.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sprinté*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Is_Already_Engaged()
    {
        var (match, chargingUnit, targetUnit) = CreateMatchWithUnits();
        chargingUnit.EngageWith(Guid.NewGuid()); // Déjà engagé

        Func<Task> act = () => CreateHandler(Roller(6)).Handle(
            new ChargeUnitCommand(match.Id, chargingUnit.Id, targetUnit.Id), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*engagée*");
    }

    [Fact]
    public async Task Handle_Should_Move_Figures_Toward_Target_On_Success()
    {
        // Cible à 4" bord-à-bord, M=6, D6=3 → distance = 3+6 = 9" → atteint
        var (match, chargingUnit, targetUnit) = CreateMatchWithUnits(targetEdgeDistanceInches: 4.0, chargingUnitMovement: 6.0);
        var initialX = chargingUnit.Figures[0].Position.X;

        await CreateHandler(Roller(3)).Handle(
            new ChargeUnitCommand(match.Id, chargingUnit.Id, targetUnit.Id), CancellationToken.None);

        // La figurine doit s'être rapprochée (X > 0)
        chargingUnit.Figures[0].Position.X.Should().BeGreaterThan(initialX);
    }
}
