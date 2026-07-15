using FluentAssertions;
using Moq;
using Wargame.Application.Commands.GameMatch;
using Wargame.Application.Interfaces.Repositories;

namespace Wargame.Application.Tests.Commands.GameMatch;

public class CreateGameMatchCommandTests
{
    [Fact]
    public async Task Handle_Should_Create_And_Save_GameMatch()
    {
        // Arrange
        var repositoryMock = new Mock<IGameMatchRepository>();
        var handler = new CreateGameMatchCommandHandler(repositoryMock.Object);
        var command = new CreateGameMatchCommand("Alice", "Bob", 48, 48);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        
        repositoryMock.Verify(r => r.SaveAsync(
            It.Is<Wargame.Domain.Entities.GameMatch>(g => 
                g.Id == result && 
                g.Players.Count == 2 && 
                g.Players[0].Name == "Alice" && 
                g.Players[1].Name == "Bob" && 
                g.Board.Width == 48), 
            It.IsAny<CancellationToken>()), 
        Times.Once);
    }
}
