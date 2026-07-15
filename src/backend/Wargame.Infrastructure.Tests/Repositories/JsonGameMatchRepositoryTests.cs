using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Infrastructure.Repositories;
using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Wargame.Infrastructure.Serialization;

namespace Wargame.Infrastructure.Tests.Repositories;

public class JsonGameMatchRepositoryTests : JsonRepositoryTestBase
{
    [Fact]
    public async Task Can_Load_GameMatch_From_File()
    {
        // Arrange
        var matchId = Guid.NewGuid();
        
        var board = new Board(Guid.NewGuid(), 48, 48);
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        
        var gameMatch = new GameMatch(matchId, [player1, player2], board);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { JsonPrivateResolver.SetPrivateSettersAndConstructors }
            }
        };

        var filePath = Path.Combine(TestDataDirectory, "games.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(new[] { gameMatch }, options));

        var repository = new JsonGameMatchRepository(Options);

        // Act
        var loadedMatch = await repository.GetByIdAsync(matchId, CancellationToken.None);

        // Assert
        loadedMatch.Should().NotBeNull();
        loadedMatch!.Id.Should().Be(matchId);
        loadedMatch.Players.Should().HaveCount(2);
        loadedMatch.Players.First().Name.Should().Be("Player 1");
        loadedMatch.Board.Should().NotBeNull();
        loadedMatch.Board.Width.Should().Be(48);
    }
}
