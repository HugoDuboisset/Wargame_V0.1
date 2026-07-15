using Wargame.Domain.Enums;
using Wargame.Domain.Primitives;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Entities;

/// <summary>
/// L'entité centrale du jeu. Représente un groupe de figurines agissant ensemble
/// sous la même fiche, avec un seul profil de caractéristiques partagé.
/// Gère son propre cycle de vie, son statut d'activation, et ses altérations d'état.
/// </summary>
public class Unit : Entity
{
    private readonly List<Figure> _figures = [];
    private readonly List<Guid> _engagedWithUnitIds = [];

    // --- Identité ---
    public string Name { get; private set; }
    public UnitType Type { get; private set; }

    /// <summary>
    /// Profil de base de l'unité. Ce profil ne change jamais.
    /// Les modificateurs situationnels sont calculés via les méthodes GetEffective*().
    /// </summary>
    public UnitProfile BaseProfile { get; private set; }

    // --- Composition ---
    public IReadOnlyList<Figure> Figures => _figures.AsReadOnly();

    // --- Cycle de vie ---
    public UnitLifecycleStatus LifecycleStatus { get; private set; } = UnitLifecycleStatus.Alive;

    // --- Activation ---
    public ActivationStatus ActivationStatus { get; private set; } = ActivationStatus.Waiting;

    // --- Tracker d'actions du tour (réinitialisé à chaque nouveau tour) ---
    /// <summary>Type de mouvement effectué lors de la phase de mouvement ce tour.</summary>
    public MovementType MovementThisTurn { get; private set; } = MovementType.None;

    /// <summary>L'unité a-t-elle fait feu ce tour ?</summary>
    public bool HasFired { get; private set; }

    /// <summary>L'unité a-t-elle chargé ce tour ?</summary>
    public bool HasCharged { get; private set; }

    // --- Altérations d'état (Flags cumulables) ---
    public StatusEffect ActiveStatusEffects { get; private set; } = StatusEffect.None;

    // --- Corps à corps ---
    /// <summary>Identifiants des unités ennemies avec lesquelles cette unité est engagée.</summary>
    public IReadOnlyList<Guid> EngagedWithUnitIds => _engagedWithUnitIds.AsReadOnly();

    public Unit(Guid id, string name, UnitType type, UnitProfile baseProfile,
                IReadOnlyList<Figure> figures) : base(id)
    {
        Name = name;
        Type = type;
        BaseProfile = baseProfile;
        _figures.AddRange(figures);

        if (!_figures.Any())
            throw new ArgumentException("A unit must have at least one figure.", nameof(figures));
    }

    // =====================================================================
    //  REQUÊTES D'ÉTAT
    // =====================================================================

    /// <summary>Nombre de figurines encore en vie.</summary>
    public int GetAliveCount() => _figures.Count(f => f.IsAlive);

    /// <summary>L'unité est-elle engagée au corps à corps ?</summary>
    public bool IsEngaged() => _engagedWithUnitIds.Count > 0;

    /// <summary>L'unité est-elle démoralisée ?</summary>
    public bool IsDemoralized() => ActiveStatusEffects.HasFlag(StatusEffect.Demoralized);

    /// <summary>L'unité est-elle en fuite ?</summary>
    public bool IsFleeing() => ActiveStatusEffects.HasFlag(StatusEffect.Fleeing);

    /// <summary>L'unité est-elle clouée au sol ?</summary>
    public bool IsPinnedDown() => ActiveStatusEffects.HasFlag(StatusEffect.PinnedDown);

    /// <summary>
    /// L'unité a-t-elle atteint ou dépassé le seuil de 50% de pertes ?
    /// Déclenche un test de moral si true.
    /// </summary>
    public bool HasLostHalfOrMore()
    {
        var totalCount = _figures.Count;
        return GetAliveCount() <= totalCount / 2;
    }

    // =====================================================================
    //  CARACTÉRISTIQUES EFFECTIVES (avec modificateurs situationnels)
    // =====================================================================

    /// <summary>
    /// Mouvement effectif en pouces.
    /// Réduit à 0 si Clouée au sol, réduit de 2 si Saturée.
    /// </summary>
    public double GetEffectiveMovement()
    {
        if (IsPinnedDown()) return 0;
        var movement = BaseProfile.Movement;
        if (ActiveStatusEffects.HasFlag(StatusEffect.Suppressed))
            movement = Math.Max(0, movement - 2);
        return movement;
    }

    /// <summary>
    /// Modificateur cumulé sur les jets de Tir (valeur négative = malus).
    /// -1 si Clouée au sol.
    /// </summary>
    public int GetShootingModifier()
    {
        var modifier = 0;
        if (IsPinnedDown()) modifier -= 1;
        return modifier;
    }

    /// <summary>
    /// Initiative effective, intégrant les bonus de charge et de couvert défensif.
    /// </summary>
    /// <param name="assaultCoverBonus">Bonus de couvert reçu si l'unité défend dans une zone d'Occupation (fourni par Terrain.AssaultInitiativeBonus).</param>
    /// <param name="isCharging">True si l'unité a déclaré un assaut ce tour (+2 en Initiative).</param>
    public int GetEffectiveInitiative(int assaultCoverBonus = 0, bool isCharging = false)
    {
        var initiative = BaseProfile.Initiative;
        if (isCharging) initiative += 2;
        initiative += assaultCoverBonus;
        return initiative;
    }

    // =====================================================================
    //  MUTATIONS D'ÉTAT
    // =====================================================================

    public void ApplyStatusEffect(StatusEffect effect)
    {
        ActiveStatusEffects |= effect;
    }

    public void RemoveStatusEffect(StatusEffect effect)
    {
        ActiveStatusEffects &= ~effect;
    }

    public void SetMovement(MovementType movementType)
    {
        MovementThisTurn = movementType;
    }

    public void RegisterFired() => HasFired = true;

    public void RegisterCharged() => HasCharged = true;

    public void SetActivationStatus(ActivationStatus status)
    {
        ActivationStatus = status;
    }

    public void EngageWith(Guid enemyUnitId)
    {
        if (!_engagedWithUnitIds.Contains(enemyUnitId))
            _engagedWithUnitIds.Add(enemyUnitId);
    }

    public void Disengage(Guid enemyUnitId)
    {
        _engagedWithUnitIds.Remove(enemyUnitId);
    }

    public void DisengageAll()
    {
        _engagedWithUnitIds.Clear();
    }

    public void Destroy()
    {
        LifecycleStatus = UnitLifecycleStatus.Destroyed;
    }

    public void MarkAsEscaped()
    {
        LifecycleStatus = UnitLifecycleStatus.Escaped;
    }

    /// <summary>
    /// Réinitialise le tracker d'actions pour le nouveau tour.
    /// Appelé par GameMatch.AdvanceToNextTurn() sur toutes les unités en vie.
    /// </summary>
    public void ResetForNewTurn()
    {
        MovementThisTurn = MovementType.None;
        HasFired = false;
        HasCharged = false;
        ActivationStatus = ActivationStatus.Waiting;

        // PinnedDown ne dure qu'une activation : dissipé en début de tour suivant.
        RemoveStatusEffect(StatusEffect.PinnedDown);
        // Suppressed ne dure qu'une activation.
        RemoveStatusEffect(StatusEffect.Suppressed);
        // OnFire est géré (résolu et retiré) en phase d'Activation, pas ici.
    }
}
