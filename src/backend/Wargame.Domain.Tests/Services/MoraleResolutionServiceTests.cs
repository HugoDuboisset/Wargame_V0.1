using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Xunit;

namespace Wargame.Domain.Tests.Services;

public class MoraleResolutionServiceTests
{
    private Unit CreateUnitWithMorale(int morale)
    {
        var profile = new UnitProfile(6.0, 4, 4, 4, morale, ArmorClass.Light);
        var figure = new Figure(Guid.NewGuid(), 1, 25, new Position(0, 0));
        return new Unit(Guid.NewGuid(), "Test Unit", UnitType.Infantry, profile, [figure]);
    }

    [Fact]
    public void ResolveMoraleTest_Should_Pass_When_Roll_Is_Less_Or_Equal_To_Morale()
    {
        var roller = new MockDiceRoller(5);
        var service = new MoraleResolutionService(roller);
        var unit = CreateUnitWithMorale(7); // Moral 7

        var passed = service.ResolveMoraleTest(unit); // 5 <= 7

        passed.Should().BeTrue();
        unit.IsDemoralized().Should().BeFalse();
        unit.IsPinnedDown().Should().BeFalse();
    }

    [Fact]
    public void ResolveMoraleTest_Should_Fail_When_Roll_Is_Greater_Than_Morale()
    {
        var roller = new MockDiceRoller(8);
        var service = new MoraleResolutionService(roller);
        var unit = CreateUnitWithMorale(7); // Moral 7

        var passed = service.ResolveMoraleTest(unit); // 8 > 7

        passed.Should().BeFalse();
        unit.IsDemoralized().Should().BeTrue();
        unit.IsPinnedDown().Should().BeTrue();
    }

    [Fact]
    public void HasLostHalfOrMore_Should_Return_True_When_Half_Or_More_Figures_Are_Dead()
    {
        var profile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var figures = new List<Figure>
        {
            new(Guid.NewGuid(), 1, 25, new Position(0, 0)),
            new(Guid.NewGuid(), 1, 25, new Position(0, 0)),
            new(Guid.NewGuid(), 1, 25, new Position(0, 0)),
            new(Guid.NewGuid(), 1, 25, new Position(0, 0)) // 4 figurines au total
        };
        var unit = new Unit(Guid.NewGuid(), "Test Unit", UnitType.Infantry, profile, figures);

        unit.HasLostHalfOrMore().Should().BeFalse();

        unit.Figures[0].TakeDamage(1); // 1 mort (reste 3/4)
        unit.HasLostHalfOrMore().Should().BeFalse();

        unit.Figures[1].TakeDamage(1); // 2 morts (reste 2/4) -> <= 50%
        unit.HasLostHalfOrMore().Should().BeTrue();
        
        unit.Figures[2].TakeDamage(1); // 3 morts (reste 1/4)
        unit.HasLostHalfOrMore().Should().BeTrue();
    }
}
