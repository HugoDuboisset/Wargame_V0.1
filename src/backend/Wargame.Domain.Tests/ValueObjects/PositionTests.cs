using FluentAssertions;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Tests;

public class PositionTests
{
    [Fact]
    public void DistanceTo_ShouldCalculateCorrectEuclideanDistance()
    {
        // Arrange (Préparation)
        var pos1 = new Position(0, 0);
        var pos2 = new Position(3, 4); // Triangle de Pythagore classique 3-4-5

        // Act (Exécution)
        var distance = pos1.DistanceTo(pos2);

        // Assert (Vérification)
        distance.Should().Be(5);
    }
}
