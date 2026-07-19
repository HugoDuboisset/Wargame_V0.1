using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Tests.Services;

public class UnitCohesionServiceTests
{
    private readonly UnitCohesionService _service;

    public UnitCohesionServiceTests()
    {
        _service = new UnitCohesionService();
    }

    private static Figure CreateFigure(Guid id, double x, double y)
    {
        // Supposons socle de 25mm => rayon = 12.5mm = 0.492"
        return new Figure(id, 1, 25, new Position(x, y));
    }

    [Fact]
    public void IsInCohesion_Should_Return_True_For_Small_Unit_In_Line()
    {
        // Arrange
        // Socles de 25mm (~1" de diamètre), placés avec 1" d'écart bord à bord
        // Centre à centre distance ~2". Edge distance ~1". Requis <= 2".
        var f1 = CreateFigure(Guid.NewGuid(), 0, 0);
        var f2 = CreateFigure(Guid.NewGuid(), 2, 0);
        var f3 = CreateFigure(Guid.NewGuid(), 4, 0);
        var f4 = CreateFigure(Guid.NewGuid(), 6, 0);
        var f5 = CreateFigure(Guid.NewGuid(), 8, 0);

        var figures = new List<Figure> { f1, f2, f3, f4, f5 };

        // Act
        var result = _service.IsInCohesion(figures);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInCohesion_Should_Return_False_For_Disconnected_Graph()
    {
        // Arrange
        // Deux groupes séparés. Unité de 4 figurines (requis 1 voisin à 2").
        var f1 = CreateFigure(Guid.NewGuid(), 0, 0);
        var f2 = CreateFigure(Guid.NewGuid(), 1, 0); // Voisin de f1

        var f3 = CreateFigure(Guid.NewGuid(), 10, 0); // Très loin
        var f4 = CreateFigure(Guid.NewGuid(), 11, 0); // Voisin de f3

        var figures = new List<Figure> { f1, f2, f3, f4 };

        // Act
        var result = _service.IsInCohesion(figures);

        // Assert
        // Chaque figurine a bien 1 voisin à 2", mais le graphe n'est pas continu.
        result.Should().BeFalse();
    }

    [Fact]
    public void IsInCohesion_Should_Return_False_For_Large_Unit_In_Line()
    {
        // Arrange
        // Unité de 6 figurines (requis 2 voisins à 2").
        // On les espace de 2.5" centre à centre (soit ~1.5" bord à bord).
        // Ainsi f1 n'est à <= 2" QUE de f2 (f3 est à 5" centre à centre, > 2" bord à bord).
        var f1 = CreateFigure(Guid.NewGuid(), 0, 0); // N'a qu'un seul voisin à <= 2"
        var f2 = CreateFigure(Guid.NewGuid(), 2.5, 0);
        var f3 = CreateFigure(Guid.NewGuid(), 5.0, 0);
        var f4 = CreateFigure(Guid.NewGuid(), 7.5, 0);
        var f5 = CreateFigure(Guid.NewGuid(), 10.0, 0);
        var f6 = CreateFigure(Guid.NewGuid(), 12.5, 0); // N'a qu'un seul voisin à <= 2"

        var figures = new List<Figure> { f1, f2, f3, f4, f5, f6 };

        // Act
        var result = _service.IsInCohesion(figures);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ResolveCohesionLoss_Should_Move_Isolated_Figures_If_Possible()
    {
        // Arrange
        var f1 = CreateFigure(Guid.NewGuid(), 0, 0);
        var f2 = CreateFigure(Guid.NewGuid(), 1, 0);
        // f3 est isolé, à distance 4 de f2 (centre à centre = 4). 
        // Edge distance = 4 - 0.984 = ~3.01". Max regroup move is 2".
        var f3 = CreateFigure(Guid.NewGuid(), 5, 0); 

        var unit = new Unit(Guid.NewGuid(), "Test", Wargame.Domain.Enums.UnitType.Infantry, new UnitProfile(6, 4, 4, 4, 10, Wargame.Domain.Enums.ArmorClass.Heavy), new List<Figure> { f1, f2, f3 });

        // Act
        var (moved, destroyed) = _service.ResolveCohesionLoss(unit);

        // Assert
        destroyed.Should().BeEmpty();
        moved.Should().ContainSingle();
        moved.First().Id.Should().Be(f3.Id);
        
        // f3 a avancé de 2" vers f2
        f3.Position.X.Should().BeApproximately(3.0, 0.01);
        f3.Position.Y.Should().Be(0);

        // Revérifions la cohésion
        _service.IsInCohesion(unit.Figures).Should().BeTrue();
    }

    [Fact]
    public void ResolveCohesionLoss_Should_Destroy_Figure_If_Regroup_Fails()
    {
        // Arrange
        var f1 = CreateFigure(Guid.NewGuid(), 0, 0);
        var f2 = CreateFigure(Guid.NewGuid(), 1, 0);
        // f3 est très isolé, à distance 10. Un mouvement de 2" ne suffira pas.
        var f3 = CreateFigure(Guid.NewGuid(), 10, 0); 

        var unit = new Unit(Guid.NewGuid(), "Test", Wargame.Domain.Enums.UnitType.Infantry, new UnitProfile(6, 4, 4, 4, 10, Wargame.Domain.Enums.ArmorClass.Heavy), new List<Figure> { f1, f2, f3 });

        // Act
        var (moved, destroyed) = _service.ResolveCohesionLoss(unit);

        // Assert
        destroyed.Should().ContainSingle();
        destroyed.First().Id.Should().Be(f3.Id);
        f3.IsAlive.Should().BeFalse();

        // Le mouvement de 2" a quand même été effectué avant la destruction
        moved.Should().ContainSingle();
        
        // Revérifions la cohésion des survivants
        var alive = unit.Figures.Where(f => f.IsAlive).ToList();
        _service.IsInCohesion(alive).Should().BeTrue();
    }
}
