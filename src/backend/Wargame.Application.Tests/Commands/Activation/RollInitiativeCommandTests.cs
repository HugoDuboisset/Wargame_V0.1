using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Activation;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;

namespace Wargame.Application.Tests.Commands.Activation;

public class RollInitiativeCommandTests
{
    [Fact]
    public async Task Handle_Should_Determine_First_Player_And_Save()
    {
        // Arrange
        var repositoryMock = new Mock<IGameMatchRepository>();
        var handler = new RollInitiativeCommandHandler(repositoryMock.Object);
        
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new Wargame.Domain.Entities.GameMatch(Guid.NewGuid(), new List<Player> { player1, player2 }, board);

        repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(match);

        var command = new RollInitiativeCommand(match.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        match.ActivePlayerId.Should().NotBeNull();
        match.Players.Select(p => p.Id).Should().Contain(match.ActivePlayerId!.Value);
        
        repositoryMock.Verify(r => r.SaveAsync(match, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Match_Not_Found()
    {
        // Arrange
        var repositoryMock = new Mock<IGameMatchRepository>();
        var handler = new RollInitiativeCommandHandler(repositoryMock.Object);
        var command = new RollInitiativeCommand(Guid.NewGuid());

        repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wargame.Domain.Entities.GameMatch?)null);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Partie introuvable (ID: {command.GameMatchId}).");
    }
}
