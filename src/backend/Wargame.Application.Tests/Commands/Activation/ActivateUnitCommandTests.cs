using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Activation;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

using Wargame.Domain.ValueObjects.Geometry.Bases;

namespace Wargame.Application.Tests.Commands.Activation;

public class ActivateUnitCommandTests
{
    private static readonly CircularBase StandardBase = new(12.5); // 25mm diameter

    private readonly Mock<IGameMatchRepository> _repositoryMock;
    private readonly ActivateUnitCommandHandler _handler;
    private readonly Wargame.Domain.Entities.GameMatch _match;
    private readonly Unit _unit;

    public ActivateUnitCommandTests()
    {
        _repositoryMock = new Mock<IGameMatchRepository>();
        var diceRollerMock = new Mock<IDiceRoller>();
        diceRollerMock.Setup(d => d.RollD10()).Returns(1); // Succès garanti par défaut
        var moraleService = new MoraleResolutionService(diceRollerMock.Object);
        _handler = new ActivateUnitCommandHandler(_repositoryMock.Object, moraleService);

        // Configuration d'une partie simulée
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);

        _match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), new List<Player> { player1, player2 }, board);

        var profile = new UnitProfile(6, 4, 4, 4, 7, ArmorClass.Light);
        _unit = new Unit(Guid.NewGuid(), "Tireurs d'élite", UnitType.Infantry, profile, new List<Figure> { new Figure(Guid.NewGuid(), 1, StandardBase, new Position(0, 0)) });
        
        _match.AddUnit(_unit);
        player1.AddUnit(_unit.Id);
        
        _match.DetermineFirstPlayer(); // Fixe l'ActivePlayerId
        
        // On s'assure que Player1 est bien le joueur actif pour le test
        while (_match.ActivePlayerId != player1.Id)
        {
            _match.SwitchActivePlayer();
        }

        _repositoryMock.Setup(r => r.GetByIdAsync(_match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_match);
    }

    [Fact]
    public async Task Handle_Should_Activate_Unit_When_Valid()
    {
        // Arrange
        var command = new ActivateUnitCommand(_match.Id, _unit.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unit.ActivationStatus.Should().Be(ActivationStatus.Active);
        _repositoryMock.Verify(r => r.SaveAsync(_match, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Already_Active()
    {
        // Arrange
        _unit.SetActivationStatus(ActivationStatus.Active);
        var command = new ActivateUnitCommand(_match.Id, _unit.Id);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("L'unité a déjà été activée ce tour.");
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Unit_Belongs_To_Other_Player()
    {
        // Arrange
        // On force le passage de tour au Player 2
        _match.SwitchActivePlayer(); 
        
        var command = new ActivateUnitCommand(_match.Id, _unit.Id);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("L'unité n'appartient pas au joueur actif.");
    }
}
