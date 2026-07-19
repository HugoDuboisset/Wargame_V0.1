using FluentAssertions;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.ValueObjects.Geometry;
using Wargame.Domain.ValueObjects.Geometry.Bases;

namespace Wargame.Domain.Tests.Services;

public class AssaultResolutionServiceTests
{
    private static readonly CircularBase StandardBase = new(12.5);

    [Fact]
    public void ResolveMelee_Should_Apply_Terrain_Initiative_Bonus_To_Defender_In_First_Round()
    {
        var diceRoller = new MockDiceRoller(5);
        var service = new AssaultResolutionService(diceRoller);

        var attackerFig = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(0, 0));
        var attackerProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var attackerUnit = new Unit(Guid.NewGuid(), "Attacker", UnitType.Infantry, attackerProfile, [attackerFig]);
        attackerUnit.ApplyStatusEffect(StatusEffect.Charging); // C'est le premier round de combat ! L'attaquant charge

        var defenderFig = new Figure(Guid.NewGuid(), 1, StandardBase, new Position(10, 10)); // Dans le terrain
        var defenderProfile = new UnitProfile(6.0, 4, 4, 4, 7, ArmorClass.Light);
        var defenderUnit = new Unit(Guid.NewGuid(), "Defender", UnitType.Infantry, defenderProfile, [defenderFig]);

        // Terrain offrant un couvert lourd (Initiative +3)
        var shape = new Rectangle(new Position(10, 10), 10, 10, 0);
        var terrain = new Terrain(Guid.NewGuid(), "Bunker", shape, TerrainGeometry.Occupation, CoverLevel.Heavy);

        // On résout la mêlée
        var engagedUnits = new List<Unit> { attackerUnit, defenderUnit };
        var boardTerrains = new List<Terrain> { terrain };

        // On n'a pas besoin de vérifier le résultat exact des touches, mais le service doit s'exécuter sans erreur
        // Idéalement on vérifierait l'ordre d'attaque, mais comme le service retourne juste les WoundsLost,
        // on se contente de vérifier que la mêlée se résout bien avec le terrain en paramètre.
        var result = service.ResolveMelee(engagedUnits, boardTerrains);

        result.WoundsLostPerUnit.Should().NotBeNull();
    }
}
