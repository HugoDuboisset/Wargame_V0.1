using Wargame.Domain.Enums;
using Wargame.Domain.Primitives;

namespace Wargame.Domain.Entities;

/// <summary>
/// Racine d'agrégation de la partie. Supervise le séquencement global du jeu,
/// les activations alternées, la gestion des tours et les conditions de victoire.
/// </summary>
public class GameMatch : Entity
{
    /// <summary>Nombre maximum de tours par partie.</summary>
    public const int MaxTurns = 6;

    private readonly List<Player> _players = [];
    private readonly List<Unit> _units = [];

    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    public IReadOnlyList<Unit> Units => _units.AsReadOnly();

    /// <summary>Le plateau de jeu associé à cette partie.</summary>
    public Board Board { get; private set; }

    /// <summary>Tour actuel (commence à 1, maximum MaxTurns).</summary>
    public int CurrentTurn { get; private set; } = 1;

    /// <summary>Identifiant du joueur devant activer une unité en ce moment.</summary>
    public Guid? ActivePlayerId { get; private set; }

    /// <summary>Statut global de la partie (en cours ou terminée).</summary>
    public GameStatus Status { get; private set; } = GameStatus.InProgress;



    public GameMatch(Guid id, IEnumerable<Player> players, Board board) : base(id)
    {
        Board = board;
        _players.AddRange(players);

        if (_players.Count < 2)
            throw new ArgumentException("A game match requires at least 2 players.", nameof(players));
    }

    // =====================================================================
    //  GESTION DES UNITÉS
    // =====================================================================

    /// <summary>Ajoute une unité au registre de la partie.</summary>
    public void AddUnit(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        _units.Add(unit);
    }

    // =====================================================================
    //  INITIATIVE ET SÉQUENCEMENT
    // =====================================================================

    /// <summary>
    /// Désigne aléatoirement le premier joueur à activer une unité ce tour.
    /// </summary>
    public void DetermineFirstPlayer()
    {
        if (_players.Count == 0) return;
        ActivePlayerId = _players[Random.Shared.Next(_players.Count)].Id;
    }

    /// <summary>
    /// Passe l'activation au joueur suivant (alternance).
    /// </summary>
    public void SwitchActivePlayer()
    {
        if (ActivePlayerId == null) return;
        var nextPlayer = _players.FirstOrDefault(p => p.Id != ActivePlayerId);
        ActivePlayerId = nextPlayer?.Id;
    }

    // =====================================================================
    //  GESTION DES TOURS
    // =====================================================================

    /// <summary>
    /// Avance au tour suivant : incrémente le compteur, réinitialise toutes les unités,
    /// et vide les jets d'initiative.
    /// Si le tour maximum est atteint, clôture la partie.
    /// </summary>
    public void AdvanceToNextTurn()
    {
        if (CurrentTurn >= MaxTurns)
        {
            Status = GameStatus.Completed;
            return;
        }

        CurrentTurn++;

        foreach (var unit in _units.Where(u => u.LifecycleStatus == UnitLifecycleStatus.Alive))
        {
            unit.ResetForNewTurn();
        }
    }

    // =====================================================================
    //  CONDITIONS DE VICTOIRE
    // =====================================================================

    /// <summary>
    /// Vérifie si la partie est terminée.
    /// Conditions : 6 tours joués, ou un camp n'a plus aucune unité en vie.
    /// </summary>
    public bool IsGameOver()
    {
        if (Status == GameStatus.Completed) return true;
        if (CurrentTurn > MaxTurns) return true;

        // Fin de partie si un joueur n'a plus aucune unité opérationnelle
        // (toutes ses unités sont soit hors-jeu (détruites, échappées), soit démoralisées).
        foreach (var player in _players)
        {
            var playerUnits = _units.Where(u => player.UnitIds.Contains(u.Id));
            if (playerUnits.All(u => u.LifecycleStatus != UnitLifecycleStatus.Alive || u.IsDemoralized()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Calcule et attribue les points de victoire (Kill Points) selon les règles Annihilation.
    /// Chaque joueur gagne les points des unités ennemies Détruites, En fuite ou Démoralisées.
    /// </summary>
    /// <param name="unitPointValues">Dictionnaire associant l'Id de chaque unité à sa valeur en points.</param>
    public void CalculateVictoryPoints(IReadOnlyDictionary<Guid, int> unitPointValues)
    {
        foreach (var player in _players)
        {
            var enemyUnitIds = _players
                .Where(p => p.Id != player.Id)
                .SelectMany(p => p.UnitIds)
                .ToHashSet();

            var pointsEarned = _units
                .Where(u => enemyUnitIds.Contains(u.Id) && IsScoringUnit(u))
                .Sum(u => unitPointValues.TryGetValue(u.Id, out var pts) ? pts : 0);

            player.AddVictoryPoints(pointsEarned);
        }

        Status = GameStatus.Completed;
    }

    /// <summary>
    /// Détermine si une unité ennemie est "scorante" selon les règles Kill Points.
    /// </summary>
    private static bool IsScoringUnit(Unit unit) =>
        unit.LifecycleStatus == UnitLifecycleStatus.Destroyed ||
        unit.LifecycleStatus == UnitLifecycleStatus.Escaped ||
        unit.IsDemoralized();
}
