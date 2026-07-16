using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Movement;
using Wargame.Application.Commands.Movement.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Application.Tests.Commands.Movement;

public class MoveUnitCommandTests
{
    private const int BaseSizeMm = 25; // socle standard 25mm
    private const double BaseRadiusInches = BaseSizeMm / 2.0 / 25.4; // ~0.492"

    private readonly Mock<IGameMatchRepository> _repositoryMock = new();

    private (Wargame.Domain.Entities.GameMatch match, Unit unit) CreateMatchWithUnit(
        int figureCount = 2, double movement = 6.0, StatusEffect status = StatusEffect.None, bool engaged = false)
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), [player1, player2], board);

        var profile = new UnitProfile(movement, 4, 4, 4, 7, ArmorClass.Light);
        var figures = Enumerable.Range(0, figureCount)
            .Select(i => new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(i * 2.0, 0)))
            .ToList<Figure>();

        var unit = new Unit(Guid.NewGuid(), "Test Unit", UnitType.Infantry, profile, figures);

        if (status != StatusEffect.None) unit.ApplyStatusEffect(status);

        if (engaged)
        {
            unit.EngageWith(Guid.NewGuid());
        }

        match.AddUnit(unit);
        player1.AddUnit(unit.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        return (match, unit);
    }

    [Fact]
    public async Task Handle_Should_Move_Unit_Normally_When_Valid()
    {
        var (match, unit) = CreateMatchWithUnit(figureCount: 2, movement: 6.0);
        var fig = unit.Figures[0];

        // On déplace la figurine de 3" (dans la limite de 6" de mouvement normal)
        // La figurine 1 se trouve en (0,0), la figurine 2 en (2,0)
        // On place la fig 0 en (1,0) : à 1" - 2*rayon bord à bord des voisins ~ 0"
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Normal,
        [
            new FigureMoveDto(fig.Id, 3, 0) // déplacement de 3" (bord à bord : 3" - rayon = ok)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        await handler.Handle(command, CancellationToken.None);

        unit.MovementThisTurn.Should().Be(MovementType.Normal);
        _repositoryMock.Verify(r => r.SaveAsync(match, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Allow_Sprint()
    {
        var (match, unit) = CreateMatchWithUnit(figureCount: 2, movement: 6.0);

        // Sprint : max 12". On déplace les deux figurines de 10" en restant en cohésion entre elles (séparées de 0.5")
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Sprint,
        [
            new FigureMoveDto(unit.Figures[0].Id, 10, 0),
            new FigureMoveDto(unit.Figures[1].Id, 10.5, 0) // 0.5" de centre à centre (~bord à bord ok pour 25mm)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        await handler.Handle(command, CancellationToken.None);

        unit.MovementThisTurn.Should().Be(MovementType.Sprint);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Is_Engaged()
    {
        var (match, unit) = CreateMatchWithUnit(engaged: true);
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Normal,
        [
            new FigureMoveDto(unit.Figures[0].Id, 3, 0)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*engagée au corps à corps*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Is_Pinned_Down()
    {
        var (match, unit) = CreateMatchWithUnit(status: StatusEffect.PinnedDown);
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Normal,
        [
            new FigureMoveDto(unit.Figures[0].Id, 3, 0)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*clouée au sol*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Figure_Exceeds_Max_Distance()
    {
        var (match, unit) = CreateMatchWithUnit(movement: 4.0);
        var fig = unit.Figures[0];

        // On essaie de déplacer de 10", alors que le max normal est 4"
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Normal,
        [
            new FigureMoveDto(fig.Id, 10, 0)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*dépasse la distance maximale*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Cohesion_Broken_Small_Unit()
    {
        var (match, unit) = CreateMatchWithUnit(figureCount: 3, movement: 12.0);

        // On déplace la figurine 0 à (20, 0), très loin des deux autres (en (2,0) et (4,0))
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Sprint,
        [
            new FigureMoveDto(unit.Figures[0].Id, 20, 0)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cohésion*");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Figure_Too_Close_To_Enemy()
    {
        var (match, unit) = CreateMatchWithUnit(figureCount: 1, movement: 12.0);

        // Créer une unité ennemie à (5, 0) avec socle 25mm
        var enemyProfile = new UnitProfile(6, 4, 4, 4, 7, ArmorClass.Light);
        var enemyFigure = new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(5, 0));
        var enemyUnit = new Unit(Guid.NewGuid(), "Enemy", UnitType.Infantry, enemyProfile, [enemyFigure]);
        match.AddUnit(enemyUnit);

        // On essaie de déplacer notre figurine (en 0,0) vers (4, 0), 
        // soit bord à bord avec l'ennemi à (5,0) : ~0.04" (< 1")
        var command = new MoveUnitCommand(match.Id, unit.Id, MovementType.Normal,
        [
            new FigureMoveDto(unit.Figures[0].Id, 4, 0)
        ]);

        var handler = new MoveUnitCommandHandler(_repositoryMock.Object);
        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*moins de 1\"*");
    }

    [Fact]
    public async Task DeclareStationary_Should_Set_MovementType_None()
    {
        var (match, unit) = CreateMatchWithUnit();
        var command = new DeclareStationaryCommand(match.Id, unit.Id);

        var handler = new DeclareStationaryCommandHandler(_repositoryMock.Object);
        await handler.Handle(command, CancellationToken.None);

        unit.MovementThisTurn.Should().Be(MovementType.None);
        _repositoryMock.Verify(r => r.SaveAsync(match, It.IsAny<CancellationToken>()), Times.Once);
    }
}
