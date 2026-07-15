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

public class JsonWeaponRepositoryTests : JsonRepositoryTestBase
{
    [Fact]
    public async Task Can_Load_Weapons_From_File()
    {
        // Arrange
        var weaponId = Guid.NewGuid();
        var weapon = new Weapon(weaponId, "Test Rifle", 
            new WeaponProfile(WeaponType.Ranged, 24, 2, 4, RangedWeaponCaliber.MediumCaliber, null, WeaponTrait.None));

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { JsonPrivateResolver.SetPrivateSettersAndConstructors }
            }
        };

        var filePath = Path.Combine(TestDataDirectory, "weapons.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(new[] { weapon }, options));

        var repository = new JsonWeaponRepository(Options);

        // Act
        var loadedWeapon = await repository.GetByIdAsync(weaponId, CancellationToken.None);
        var allWeapons = await repository.GetAllAsync(CancellationToken.None);

        // Assert
        loadedWeapon.Should().NotBeNull();
        loadedWeapon!.Id.Should().Be(weaponId);
        loadedWeapon.Name.Should().Be("Test Rifle");
        loadedWeapon.Type.Should().Be(WeaponType.Ranged);
        loadedWeapon.Profile.Range.Should().Be(24);
        loadedWeapon.Profile.Attacks.Should().Be(2);
        loadedWeapon.Profile.Damage.Should().Be(4);
        
        allWeapons.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_Returns_Null_For_Unknown_Id()
    {
        // Arrange
        var repository = new JsonWeaponRepository(Options);

        // Act
        var loadedWeapon = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        loadedWeapon.Should().BeNull();
    }
}
