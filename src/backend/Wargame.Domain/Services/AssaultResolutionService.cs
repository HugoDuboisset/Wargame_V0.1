using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Services;

/// <summary>
/// Service responsable de la résolution complète d'un combat de mêlée.
/// Gère l'ordre d'initiative, les jets pour toucher, blesser et les sauvegardes.
/// </summary>
public class AssaultResolutionService
{
    private readonly IDiceRoller _diceRoller;

    public AssaultResolutionService(IDiceRoller diceRoller)
    {
        _diceRoller = diceRoller;
    }

    /// <summary>
    /// Représente une attaque prête à être résolue.
    /// </summary>
    public class MeleeAttack
    {
        public Figure Attacker { get; set; } = null!;
        public Unit AttackerUnit { get; set; } = null!;
        public Weapon Weapon { get; set; } = null!;
        public Figure Target { get; set; } = null!;
        public Unit TargetUnit { get; set; } = null!;
        public int EffectiveInitiative { get; set; }
    }

    /// <summary>
    /// Résout la mêlée entre un ensemble d'unités engagées.
    /// 1. Identifie toutes les figurines éligibles pour combattre (au contact ou Allonge).
    /// 2. Détermine l'Initiative effective de chaque arme/figurine.
    /// 3. Résout par ordre d'initiative décroissant.
    /// 4. Retire les pertes à la fin de chaque palier d'initiative.
    /// </summary>
    /// <returns>Dictionnaire des dégâts subis par unité, et un flag BrutalTriggered</returns>
    public (Dictionary<Guid, int> WoundsLostPerUnit, Dictionary<Guid, int> FiguresLostPerUnit, Dictionary<Guid, bool> BrutalTriggeredAgainst) ResolveMelee(
        List<Unit> engagedUnits)
    {
        var woundsLost = engagedUnits.ToDictionary(u => u.Id, u => 0);
        var figuresLost = engagedUnits.ToDictionary(u => u.Id, u => 0);
        var brutalTriggered = engagedUnits.ToDictionary(u => u.Id, u => false);

        var allAttacks = GenerateAllAttacks(engagedUnits);

        // Grouper les attaques par Initiative effective (décroissante)
        var attacksByInit = allAttacks
            .GroupBy(a => a.EffectiveInitiative)
            .OrderByDescending(g => g.Key);

        foreach (var initGroup in attacksByInit)
        {
            // Les figurines mortes lors des paliers d'initiative précédents ne peuvent plus attaquer
            var validAttacks = initGroup.Where(a => a.Attacker.IsAlive).ToList();

            // Touches et Blessures simultanées pour ce palier
            var hits = new List<(MeleeAttack Attack, int Hits)>();

            foreach (var attack in validAttacks)
            {
                // Si la cible originale est morte, on essaie de trouver une autre cible valide
                if (!attack.Target.IsAlive)
                {
                    var newTarget = FindValidTarget(attack.Attacker, engagedUnits.Where(u => u.Id != attack.AttackerUnit.Id).ToList(), attack.Weapon);
                    if (newTarget == null) continue; // Plus de cible, l'attaque est perdue
                    attack.Target = newTarget.Value.TargetFigure;
                    attack.TargetUnit = newTarget.Value.TargetUnit;
                }

                int hitsScored = ResolveHits(attack);
                if (hitsScored > 0)
                {
                    hits.Add((attack, hitsScored));
                }
            }

            // Résoudre les blessures
            var damageToApply = new Dictionary<Figure, (int Damage, bool IsBrutal)>();

            foreach (var (attack, hitCount) in hits)
            {
                int wounds = ResolveWounds(attack, hitCount);
                if (wounds > 0)
                {
                    int damage = wounds * attack.Weapon.Profile.Damage;
                    bool isBrutal = attack.Weapon.Profile.Traits.HasFlag(WeaponTrait.Brutal);
                    
                    if (!damageToApply.ContainsKey(attack.Target))
                    {
                        damageToApply[attack.Target] = (0, false);
                    }
                    
                    var current = damageToApply[attack.Target];
                    damageToApply[attack.Target] = (current.Damage + damage, current.IsBrutal || isBrutal);
                }
            }

            // Appliquer les dégâts et retirer les pertes pour ce palier d'initiative
            foreach (var kvp in damageToApply)
            {
                var targetFigure = kvp.Key;
                var damageInfo = kvp.Value;

                var targetUnit = engagedUnits.First(u => u.Figures.Contains(targetFigure));
                
                int hpBefore = targetFigure.CurrentHitPoints;
                bool killed = targetFigure.TakeDamage(damageInfo.Damage);
                int hpLost = hpBefore - targetFigure.CurrentHitPoints;

                woundsLost[targetUnit.Id] += hpLost;

                if (killed)
                {
                    figuresLost[targetUnit.Id]++;
                    if (damageInfo.IsBrutal)
                    {
                        brutalTriggered[targetUnit.Id] = true;
                    }
                }
            }
        }

        return (woundsLost, figuresLost, brutalTriggered);
    }

    private List<MeleeAttack> GenerateAllAttacks(List<Unit> engagedUnits)
    {
        var attacks = new List<MeleeAttack>();

        foreach (var attackerUnit in engagedUnits)
        {
            var enemyUnits = engagedUnits.Where(u => u.Id != attackerUnit.Id).ToList();
            if (!enemyUnits.Any()) continue;

            foreach (var attackerFig in attackerUnit.Figures.Where(f => f.IsAlive))
            {
                // Choisir la meilleure arme de mêlée (simplification : on prend la première, ou celle avec le plus de Dégâts)
                var weapon = attackerFig.MeleeWeapons.OrderByDescending(w => w.Profile.Damage).FirstOrDefault();
                if (weapon == null) continue;

                var targetInfo = FindValidTarget(attackerFig, enemyUnits, weapon);
                if (targetInfo == null) continue; // Pas d'ennemi à portée

                // Calcul Initiative
                int effectiveInit = attackerUnit.BaseProfile.Initiative;
                if (attackerUnit.ActiveStatusEffects.HasFlag(StatusEffect.Charging))
                    effectiveInit += 2;
                if (weapon.Profile.Traits.HasFlag(WeaponTrait.Unbalancing))
                    effectiveInit -= 2;
                
                // Note : le bonus d'initiative lié au terrain du défenseur n'est pas encore géré à l'échelle de la figurine
                
                attacks.Add(new MeleeAttack
                {
                    Attacker = attackerFig,
                    AttackerUnit = attackerUnit,
                    Weapon = weapon,
                    Target = targetInfo.Value.TargetFigure,
                    TargetUnit = targetInfo.Value.TargetUnit,
                    EffectiveInitiative = effectiveInit
                });
            }
        }

        return attacks;
    }

    private (Unit TargetUnit, Figure TargetFigure)? FindValidTarget(Figure attacker, List<Unit> enemyUnits, Weapon weapon)
    {
        bool hasReach = weapon.Profile.Traits.HasFlag(WeaponTrait.Reach);
        double maxDist = hasReach ? 1.0 : 0.0; // 0.0 = contact socle à socle

        var validTargets = enemyUnits
            .SelectMany(u => u.Figures.Where(f => f.IsAlive).Select(f => new { Unit = u, Figure = f }))
            .Where(x => attacker.GetEdgeDistanceTo(x.Figure) <= maxDist + 0.001)
            .OrderBy(x => attacker.GetEdgeDistanceTo(x.Figure))
            .ToList();

        if (validTargets.Any())
        {
            var bestTarget = validTargets.First();
            return (bestTarget.Unit, bestTarget.Figure);
        }

        return null;
    }

    private int ResolveHits(MeleeAttack attack)
    {
        int a = attack.Weapon.Profile.Attacks;
        int attackerCombat = attack.AttackerUnit.BaseProfile.Combat;
        int defenderCombat = attack.TargetUnit.BaseProfile.Combat;

        if (attack.Weapon.Profile.Traits.HasFlag(WeaponTrait.Parry))
        {
            attackerCombat += 1;
        }

        int targetNumber = MeleeHitMatrix.GetTargetNumber(attackerCombat, defenderCombat);
        int hits = 0;

        for (int i = 0; i < a; i++)
        {
            int roll = _diceRoller.RollD10();
            if (roll > 1 && roll >= targetNumber) // 1 naturel est toujours un échec
            {
                hits++;
                if (attack.Weapon.Profile.Traits.HasFlag(WeaponTrait.Sweep))
                {
                    hits++; // Balayage génère 1 touche supplémentaire
                }
            }
        }

        return hits;
    }

    private int ResolveWounds(MeleeAttack attack, int hitCount)
    {
        if (hitCount <= 0) return 0;

        var category = attack.Weapon.Profile.MeleeCategory ?? MeleeWeaponCategory.Light;
        var targetArmor = attack.TargetUnit.BaseProfile.ArmorClass;
        
        int targetNumber = WoundMatrix.GetMeleeTargetNumber(category, targetArmor);
        if (targetNumber == WoundMatrix.Impossible) return 0;

        // Le trait Balayage donne -1 pour blesser (augmente le score cible de 1)
        if (attack.Weapon.Profile.Traits.HasFlag(WeaponTrait.Sweep))
        {
            targetNumber += 1;
        }

        int wounds = 0;
        for (int i = 0; i < hitCount; i++)
        {
            int roll = _diceRoller.RollD10();
            if (roll > 1 && roll >= targetNumber)
            {
                wounds++;
            }
        }

        return wounds;
    }
}
