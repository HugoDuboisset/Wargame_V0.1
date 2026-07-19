using FluentAssertions;
using Moq;
using Wargame.Application.Commands.Assault;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;
using Wargame.Domain.ValueObjects;
using Xunit;
using DomainMatch = Wargame.Domain.Entities.GameMatch;

namespace Wargame.Application.Tests.Commands.Assault;

public class ResolveMeleeCommandTests
{
    private const int BaseSizeMm = 25;
    private readonly Mock<IGameMatchRepository> _repositoryMock = new();

    private (DomainMatch match, Unit unit1, Unit unit2) CreateEngagedUnits(
        WeaponTrait u1WeaponTrait = WeaponTrait.None,
        WeaponTrait u2WeaponTrait = WeaponTrait.None,
        int u1Combat = 4,
        int u2Combat = 4,
        int u1Init = 4,
        int u2Init = 4,
        bool u1Charging = false)
    {
        var player1 = new Player(Guid.NewGuid(), "Player 1");
        var player2 = new Player(Guid.NewGuid(), "Player 2");
        var board = new Board(Guid.NewGuid(), 48, 48);
        var match = new DomainMatch(Guid.NewGuid(), [player1, player2], board);

        var wp1 = new WeaponProfile(WeaponType.Melee, 1, 1, 1, null, MeleeWeaponCategory.Light, u1WeaponTrait);
        var wp2 = new WeaponProfile(WeaponType.Melee, 1, 1, 1, null, MeleeWeaponCategory.Light, u2WeaponTrait);

        var weapon1 = new Weapon(Guid.NewGuid(), "Weapon 1", wp1);
        var weapon2 = new Weapon(Guid.NewGuid(), "Weapon 2", wp2);

        var p1 = new UnitProfile(6, 4, u1Combat, u1Init, 7, ArmorClass.Light);
        var p2 = new UnitProfile(6, 4, u2Combat, u2Init, 7, ArmorClass.Light);

        // Au contact (0,0) et (0, 0.5") -> base to base distance < 0
        var f1 = new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(0, 0), null, [weapon1]);
        var f2 = new Figure(Guid.NewGuid(), 1, BaseSizeMm, new Position(0.5, 0), null, [weapon2]);

        var u1 = new Unit(Guid.NewGuid(), "Unit 1", UnitType.Infantry, p1, [f1]);
        var u2 = new Unit(Guid.NewGuid(), "Unit 2", UnitType.Infantry, p2, [f2]);

        u1.EngageWith(u2.Id);
        u2.EngageWith(u1.Id);

        if (u1Charging)
        {
            u1.ApplyStatusEffect(StatusEffect.Charging);
        }

        match.AddUnit(u1);
        match.AddUnit(u2);
        player1.AddUnit(u1.Id);
        player2.AddUnit(u2.Id);

        _repositoryMock.Setup(r => r.GetByIdAsync(match.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(match);

        return (match, u1, u2);
    }

    private static IDiceRoller Roller(params int[] rolls)
    {
        var mock = new Moq.Mock<IDiceRoller>();
        var queue = new Queue<int>(rolls);
        mock.Setup(d => d.RollD10()).Returns(() => queue.Count > 0 ? queue.Dequeue() : 1);
        return mock.Object;
    }

    private ResolveMeleeCommandHandler CreateHandler(IDiceRoller roller)
    {
        var resolutionService = new AssaultResolutionService(roller);
        return new ResolveMeleeCommandHandler(_repositoryMock.Object, resolutionService);
    }

    [Fact]
    public async Task Handle_Should_Apply_Wounds_And_Identify_Loser()
    {
        var (match, u1, u2) = CreateEngagedUnits();
        
        // U1 attaque: touche (C4 vs C4 -> 5+). On donne 8.
        // Blesse: (Legere vs Light -> 5+). On donne 6. (1 blessure, 1 mort pour U2)
        // U2 attaque: touche (5+). On donne 2 (échec).
        var roller = Roller(8, 6, 2); 
        // Note: ordre de frappe ? Même init = simultané.
        // Service va lancer D10 pour U1 (toucher), U2 (toucher), puis U1 (blesser), U2 (blesser).
        // En réalité: le service lance Toucher puis Blesser pour chaque figurine du même palier.
        // Le foreach attaque parcourt U1 puis U2.
        // U1: Roll(8) -> Hit.
        // U2: Roll(6) -> Hit.
        // U1 Wounds: Roll(2) -> Fail.
        // U2 Wounds: Roll(?) ... 
        // Mieux vaut donner une séquence de dés contrôlée et abondante.
        // U1 attaque: 8 (hit), 6 (wound) -> 1 dmg
        // U2 attaque: 2 (miss)
        // Mais comment être sûr de l'ordre ? GroupBy Init. Ils ont même Init.
        // GroupBy préserve l'ordre initial des éléments (OrderBy descending init).
        // U1 a été ajouté en premier dans GenerateAllAttacks. Donc U1 tire ses dés, puis U2 tire ses dés pour Toucher. 
        // Puis U1 blessures, puis U2 blessures.
        var r = Roller(8, 2, 6); // U1 hit, U2 miss, U1 wound

        var result = await CreateHandler(r).Handle(
            new ResolveMeleeCommand(match.Id, [u1.Id, u2.Id]), CancellationToken.None);

        result.WoundsLostPerUnit[u2.Id].Should().Be(1);
        result.WoundsLostPerUnit[u1.Id].Should().Be(0);
        result.LoserUnitId.Should().Be(u2.Id);

        u2.Figures.First().IsAlive.Should().BeFalse();
        u2.LifecycleStatus.Should().Be(UnitLifecycleStatus.Destroyed);
    }

    [Fact]
    public async Task Handle_Should_Disengage_When_Enemies_Destroyed()
    {
        var (match, u1, u2) = CreateEngagedUnits();
        var r = Roller(8, 2, 6); // U1 tue U2

        await CreateHandler(r).Handle(
            new ResolveMeleeCommand(match.Id, [u1.Id, u2.Id]), CancellationToken.None);

        u1.IsEngaged().Should().BeFalse(); // U2 est mort, U1 doit être désengagé
    }

    [Fact]
    public async Task Handle_Should_Apply_Brutal_Trait()
    {
        var (match, u1, u2) = CreateEngagedUnits(u1WeaponTrait: WeaponTrait.Brutal);
        var r = Roller(8, 2, 6); // U1 hit, U2 miss, U1 wound

        var result = await CreateHandler(r).Handle(
            new ResolveMeleeCommand(match.Id, [u1.Id, u2.Id]), CancellationToken.None);

        result.BrutalTriggeredAgainst[u2.Id].Should().BeTrue();
    }
}
