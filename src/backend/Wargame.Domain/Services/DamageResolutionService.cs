using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;
using Wargame.Domain.Services.Traits;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de la résolution des blessures et de l'application des dégâts.
/// </summary>
public class DamageResolutionService
{
    private readonly IDiceRoller _diceRoller;
    private readonly IEnumerable<IWeaponTraitStrategy> _traitStrategies;

    public DamageResolutionService(IDiceRoller diceRoller, IEnumerable<IWeaponTraitStrategy> traitStrategies)
    {
        _diceRoller = diceRoller;
        _traitStrategies = traitStrategies ?? Enumerable.Empty<IWeaponTraitStrategy>();
    }

    /// <summary>
    /// Résout les jets de blessure à partir d'une liste de touches, et applique les dégâts et statuts à l'unité cible.
    /// Retourne le nombre de blessures réussies et de figurines détruites.
    /// </summary>
    public (int TotalWounds, int FiguresDestroyed) ResolveWoundsAndApplyDamage(
        IReadOnlyList<Hit> hits,
        Unit shootingUnit,
        Unit targetUnit,
        IReadOnlyList<Terrain> opaqueTerrains)
    {
        int totalWounds = 0;
        int figuresDestroyed = 0;

        // 1. Déterminer quelles figurines sont éligibles à subir des dégâts
        // Règle : Il n'est possible de détruire que des figurines en ligne de vue des figurines de l'unité qui tire.
        var eligibleTargets = GetEligibleTargets(shootingUnit, targetUnit, opaqueTerrains);

        if (!eligibleTargets.Any())
        {
            // Aucune cible visible (par ex: pertes retirées précédemment), les touches sont perdues
            return (0, 0);
        }

        // 2. Traitement de chaque touche
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];

            // Application des effets de statut liés aux touches (Indépendamment du jet de blessure)
            foreach (var strategy in _traitStrategies)
            {
                if (hit.Traits.HasFlag(strategy.TargetTrait))
                {
                    strategy.ApplyEffect(targetUnit, hit);
                }
            }

            // Jet de blessure
            int targetNumber = WoundMatrix.GetRangedTargetNumber(hit.Caliber, targetUnit.BaseProfile.ArmorClass);
            int roll = _diceRoller.RollD10();
            bool isWound = (roll == 10) || (roll >= targetNumber);

            if (isWound)
            {
                totalWounds++;

                // Application des dégâts
                // On reprend toujours la figurine éligible la plus proche encore en vie
                var targetFig = eligibleTargets.FirstOrDefault(f => f.IsAlive);
                
                if (targetFig != null)
                {
                    bool died = targetFig.TakeDamage(hit.Damage);
                    if (died)
                    {
                        figuresDestroyed++;
                    }
                }
            }
        }

        // Si l'unité a perdu toutes ses figurines, on marque l'unité comme détruite
        if (targetUnit.GetAliveCount() == 0)
        {
            targetUnit.Destroy();
        }

        return (totalWounds, figuresDestroyed);
    }

    /// <summary>
    /// Retourne la liste des figurines cibles en ligne de vue, triées de la plus proche à la plus lointaine.
    /// </summary>
    private static List<Figure> GetEligibleTargets(Unit shootingUnit, Unit targetUnit, IReadOnlyList<Terrain> opaqueTerrains)
    {
        var activeShooters = shootingUnit.Figures.Where(f => f.IsAlive).ToList();
        var eligibleTargets = new List<(Figure Figure, double MinDistance)>();

        foreach (var targetFig in targetUnit.Figures.Where(f => f.IsAlive))
        {
            // Est-elle visible par au moins UN tireur ?
            bool isVisible = activeShooters.Any(shooter => LineOfSightService.IsVisible(shooter, targetFig, opaqueTerrains));
            
            if (isVisible)
            {
                // On calcule la distance minimale depuis l'unité de tir
                double minDistance = activeShooters.Min(shooter => shooter.GetEdgeDistanceTo(targetFig));
                eligibleTargets.Add((targetFig, minDistance));
            }
        }

        // Trie par distance croissante (les plus proches en premier)
        return eligibleTargets.OrderBy(e => e.MinDistance).Select(e => e.Figure).ToList();
    }
}
