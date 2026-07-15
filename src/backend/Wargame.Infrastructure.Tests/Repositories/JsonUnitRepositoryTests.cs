using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;
using Wargame.Infrastructure.Repositories;
using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Wargame.Infrastructure.Serialization;

namespace Wargame.Infrastructure.Tests.Repositories;

public class JsonUnitRepositoryTests : JsonRepositoryTestBase
{
    [Fact]
    public async Task Can_Load_Units_From_File()
    {
        // Arrange
        var unitId = Guid.NewGuid();
        
        var weapon = new Weapon(Guid.NewGuid(), "Rifle", 
            new WeaponProfile(WeaponType.Ranged, 24, 2, 4, RangedWeaponCaliber.MediumCaliber, null, WeaponTrait.None));
        var figure = new Figure(Guid.NewGuid(), 1, 25, new Position(0, 0), [weapon]);
        var profile = new UnitProfile(6, 4, 4, 4, 7, ArmorClass.Light);
        
        var unit = new Unit(unitId, "Test Squad", UnitType.Infantry, profile, [figure]);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { JsonPrivateResolver.SetPrivateSettersAndConstructors }
            }
        };

        var filePath = Path.Combine(TestDataDirectory, "units.json");
        var jsonText = JsonSerializer.Serialize(new[] { unit }, options);
        await File.WriteAllTextAsync(filePath, jsonText);

        // Force throw exception to see what's wrong:
        var testDeserialization = JsonSerializer.Deserialize<Unit[]>(jsonText, options);

        var repository = new JsonUnitRepository(Options);

        // Act
        var loadedUnit = await repository.GetByIdAsync(unitId, CancellationToken.None);

        // Assert
        loadedUnit.Should().NotBeNull();
        loadedUnit!.Id.Should().Be(unitId);
        loadedUnit.Name.Should().Be("Test Squad");
        loadedUnit.Type.Should().Be(UnitType.Infantry);
        loadedUnit.BaseProfile.Movement.Should().Be(6);
        loadedUnit.Figures.Should().HaveCount(1);
        loadedUnit.Figures.First().CurrentHitPoints.Should().Be(1);
    }
}
