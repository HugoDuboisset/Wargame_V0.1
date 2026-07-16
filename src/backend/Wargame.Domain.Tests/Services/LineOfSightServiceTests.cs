using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Tests.Services;

public class LineOfSightServiceTests
{
    private const int StandardBaseSizeMm = 25;

    [Fact]
    public void IsVisible_Should_Return_True_When_No_Terrains()
    {
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        var target = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(10, 0));

        var result = LineOfSightService.IsVisible(shooter, target, []);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsVisible_Should_Return_False_When_Opaque_Terrain_Fully_Blocks()
    {
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        var target = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(10, 0));

        // Terrain très long (1000mm = ~40 pouces en Y), fin (10mm en X), entre les deux (X=5)
        var terrain = new Terrain(Guid.NewGuid(), "Wall", new Position(5, 0),
            10, 1000, 0, TerrainGeometry.Opaque, CoverLevel.Heavy);

        var result = LineOfSightService.IsVisible(shooter, target, [terrain]);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsVisible_Should_Return_True_When_Opaque_Terrain_Is_Behind_Target()
    {
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        var target = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(10, 0));

        // Terrain derrière la cible (X=15)
        var terrain = new Terrain(Guid.NewGuid(), "Wall", new Position(15, 0),
            1000, 10, 0, TerrainGeometry.Opaque, CoverLevel.Heavy);

        var result = LineOfSightService.IsVisible(shooter, target, [terrain]);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsVisible_Should_Return_True_When_Opaque_Terrain_Blocks_Center_But_Not_Edges()
    {
        var shooter = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(0, 0));
        var target = new Figure(Guid.NewGuid(), 1, StandardBaseSizeMm, new Position(10, 0));

        // Un terrain minuscule (1mm x 1mm) au centre du chemin
        // Le centre sera bloqué, mais les lignes tangentes (bords) passeront.
        var terrain = new Terrain(Guid.NewGuid(), "Poteau", new Position(5, 0),
            1, 1, 0, TerrainGeometry.Opaque, CoverLevel.Heavy);

        var result = LineOfSightService.IsVisible(shooter, target, [terrain]);

        result.Should().BeTrue();
    }

    [Fact]
    public void Intersects_Should_Return_True_When_Line_Crosses_Rectangle()
    {
        // Rectangle de 2"x2" centré en (5,5)
        var terrain = new Terrain(Guid.NewGuid(), "Box", new Position(5, 5),
            51, 51, 0, TerrainGeometry.Opaque, CoverLevel.Heavy); // 51mm ~ 2"

        var start = new Position(0, 5);
        var end = new Position(10, 5);

        var result = LineOfSightService.Intersects(start, end, terrain);

        result.Should().BeTrue();
    }

    [Fact]
    public void Intersects_Should_Return_False_When_Line_Misses_Rectangle()
    {
        // Rectangle de 2"x2" centré en (5,5)
        var terrain = new Terrain(Guid.NewGuid(), "Box", new Position(5, 5),
            51, 51, 0, TerrainGeometry.Opaque, CoverLevel.Heavy); // 51mm ~ 2"

        // Ligne qui passe au-dessus (Y = 10)
        var start = new Position(0, 10);
        var end = new Position(10, 10);

        var result = LineOfSightService.Intersects(start, end, terrain);

        result.Should().BeFalse();
    }

    [Fact]
    public void Intersects_Should_Return_True_When_Line_Crosses_Rotated_Rectangle()
    {
        // Rectangle très long (10"x1") centré en (5,5), tourné de 90 degrés (donc vertical)
        var terrain = new Terrain(Guid.NewGuid(), "Wall", new Position(5, 5),
            254, 25, 90, TerrainGeometry.Opaque, CoverLevel.Heavy); 

        // Ligne horizontale qui passe au milieu Y=5
        var start = new Position(0, 5);
        var end = new Position(10, 5);

        var result = LineOfSightService.Intersects(start, end, terrain);

        // La ligne devrait croiser le mur puisqu'il est maintenant vertical
        result.Should().BeTrue();
    }
}
