using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.ValueObjects;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de la résolution des jets pour toucher.
/// Applique les modificateurs de tir et calcule le nombre de touches générées.
/// </summary>
public class ShootingResolutionService
{
    private readonly IDiceRoller _diceRoller;

    public ShootingResolutionService(IDiceRoller diceRoller)
    {
        _diceRoller = diceRoller;
    }

    /// <summary>
    /// Résout le tir d'une figurine avec une arme spécifique contre une unité cible.
    /// Retourne la liste des touches (Hits) générées.
    /// </summary>
    public IReadOnlyList<Hit> ResolveShot(
        Figure shooter, 
        Unit shootingUnit, 
        Unit targetUnit, 
        Weapon weapon, 
        List<Terrain> boardTerrains)
    {
        var hits = new List<Hit>();

        // 1. Détermination du nombre d'attaques
        int attacks = weapon.Profile.Attacks;
        double minDistance = targetUnit.Figures.Where(f => f.IsAlive)
                                       .Select(f => shooter.GetEdgeDistanceTo(f))
                                       .DefaultIfEmpty(double.MaxValue)
                                       .Min();

        bool isAtHalfRangeOrLess = minDistance <= (weapon.Profile.Range / 2.0);

        if (weapon.HasTrait(WeaponTrait.Bursts) && isAtHalfRangeOrLess)
        {
            attacks += 2;
        }

        if (attacks <= 0)
            return hits;

        // 2. Détermination du calibre (gestion du trait Buckshot)
        var caliber = weapon.Profile.RangedCaliber ?? RangedWeaponCaliber.SmallCaliber;
        if (weapon.HasTrait(WeaponTrait.Buckshot) && isAtHalfRangeOrLess)
        {
            caliber = RangedWeaponCaliber.HeavyCaliber;
        }

        // 3. Calcul des modificateurs de tir
        int modifier = CalculateShootingModifier(shooter, shootingUnit, targetUnit, weapon, boardTerrains);

        // 4. Jets de dés
        int targetNumber = shootingUnit.BaseProfile.Shooting;
        var rolls = Enumerable.Range(0, attacks).Select(_ => _diceRoller.RollD10()).ToList();

        foreach (var roll in rolls)
        {
            // 10 nat est toujours un succès. 1 nat est toujours un échec (optionnel mais standard, ici on vérifie juste >= targetNumber)
            // On peut dire que si c'est 10, c'est réussi peu importe les malus.
            bool isHit = (roll == 10) || (roll + modifier >= targetNumber);

            if (isHit)
            {
                // Nombre de touches générées par ce dé réussi
                int hitsGenerated = 1;

                if (weapon.HasTrait(WeaponTrait.Explosive))
                {
                    hitsGenerated += weapon.Profile.ExplosiveHits;
                }

                for (int i = 0; i < hitsGenerated; i++)
                {
                    hits.Add(new Hit(caliber, weapon.Profile.Damage, weapon.Profile.Traits));
                }
            }
        }

        return hits;
    }

    private static int CalculateShootingModifier(
        Figure shooter, 
        Unit shootingUnit, 
        Unit targetUnit, 
        Weapon weapon, 
        List<Terrain> boardTerrains)
    {
        int modifier = 0;

        // Sprint + Handy
        if (shootingUnit.MovementThisTurn == MovementType.Sprint && weapon.HasTrait(WeaponTrait.Handy))
        {
            modifier -= 2;
        }

        // PinnedDown
        if (shootingUnit.IsPinnedDown())
        {
            modifier -= 1;
        }

        // Pistolet au corps à corps
        if (shootingUnit.IsEngaged() && weapon.HasTrait(WeaponTrait.Pistol))
        {
            modifier -= 2;
        }

        // Tir indirect sans ligne de vue
        if (weapon.HasTrait(WeaponTrait.IndirectFire))
        {
            var opaqueTerrains = boardTerrains.Where(t => t.IsOpaque).ToList();
            bool hasLos = targetUnit.Figures.Where(f => f.IsAlive).Any(f => LineOfSightService.IsVisible(shooter, f, opaqueTerrains));
            if (!hasLos)
            {
                modifier -= 3;
            }
        }

        // Couvert de la cible
        // On prend le meilleur niveau de couvert (plus grosse pénalité)
        if (!weapon.HasTrait(WeaponTrait.IgnoreCover))
        {
            int maxCoverPenalty = 0;
            var targetFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
            
            // Unité dans un terrain Geometry.Occupation (Règle des 50%)
            var occupationTerrains = boardTerrains.Where(t => t.Geometry.HasFlag(TerrainGeometry.Occupation)).ToList();
            foreach (var terrain in occupationTerrains)
            {
                // Le terrain offre le couvert si au moins 50% des figurines vivantes sont dedans
                int figuresInTerrain = targetFigures.Count(f => terrain.Shape.Contains(f.Position));
                bool targetInTerrain = figuresInTerrain >= Math.Ceiling(targetFigures.Count / 2.0);
                
                if (targetInTerrain && terrain.CoverPenalty < maxCoverPenalty)
                {
                    maxCoverPenalty = terrain.CoverPenalty;
                }
            }

            // Couvert par Geometry.Interference
            // Le terrain offre un couvert si la ligne de vue du tireur vers l'une des cibles VIVANTES traverse le terrain
            var interferenceTerrains = boardTerrains.Where(t => t.Geometry.HasFlag(TerrainGeometry.Interference)).ToList();
            foreach (var terrain in interferenceTerrains)
            {
                bool lineOfSightCrosses = targetFigures.Any(f => terrain.Shape.Intersects(shooter.Position, f.Position));
                
                if (lineOfSightCrosses && terrain.CoverPenalty < maxCoverPenalty)
                {
                    maxCoverPenalty = terrain.CoverPenalty;
                }
            }

            modifier += maxCoverPenalty; // maxCoverPenalty est négatif
        }

        return modifier;
    }
}
