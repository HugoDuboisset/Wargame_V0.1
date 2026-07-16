using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de valider si un tir est possible (Ligne de vue, Portée, Mouvement, Engagement).
/// </summary>
public class ShootingValidationService
{
    public void ValidateShot(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        ValidateEngagementConstraints(shootingUnit, targetUnit, weapon);
        ValidateMovementConstraints(shootingUnit, weapon);
        ValidateRange(shooter, targetUnit, weapon);
        ValidateLineOfSight(shooter, targetUnit, weapon, opaqueTerrains);
    }

    private static void ValidateEngagementConstraints(Unit shootingUnit, Unit targetUnit, Weapon weapon)
    {
        if (!shootingUnit.IsEngaged()) return;

        bool isPistol = weapon.HasTrait(WeaponTrait.Pistol);
        bool isVehicle = shootingUnit.Type == UnitType.Vehicle;

        if (isVehicle) return; // Les véhicules peuvent toujours tirer, même engagés

        if (!isPistol)
            throw new InvalidOperationException(
                "L'unité est engagée au corps à corps. Seules les armes avec le trait Pistolet peuvent tirer dans cet état.");

        // Arme Pistol : peut uniquement cibler l'unité avec laquelle elle est engagée
        bool targetIsEngaged = shootingUnit.EngagedWithUnitIds.Contains(targetUnit.Id);
        if (!targetIsEngaged)
            throw new InvalidOperationException(
                "Une arme Pistolet engagée au corps à corps ne peut cibler que l'unité avec laquelle elle est engagée.");
    }

    private static void ValidateMovementConstraints(Unit shootingUnit, Weapon weapon)
    {
        bool isVehicle = shootingUnit.Type == UnitType.Vehicle;
        if (isVehicle) return; // Les véhicules ignorent les contraintes de mouvement sur les armes

        switch (shootingUnit.MovementThisTurn)
        {
            case MovementType.Sprint:
                if (!weapon.HasTrait(WeaponTrait.Handy))
                    throw new InvalidOperationException(
                        $"L'unité a effectué un sprint. Seules les armes Maniables peuvent tirer. L'arme '{weapon.Name}' n'a pas ce trait.");
                break;

            case MovementType.Normal:
                if (weapon.HasTrait(WeaponTrait.Cumbersome))
                    throw new InvalidOperationException(
                        $"L'arme '{weapon.Name}' est Encombrante et nécessite que l'unité soit restée Immobile pour tirer.");
                break;

            case MovementType.None:
                // Immobile : toutes les armes peuvent tirer, pas de restriction
                break;
        }
    }

    private static void ValidateRange(Figure shootingFigure, Unit targetUnit, Weapon weapon)
    {
        // La portée est vérifiée de la figurine tireur à la figurine cible la plus proche (bord à bord)
        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        double minDistance = aliveFigures
            .Select(targetFig => shootingFigure.GetEdgeDistanceTo(targetFig))
            .Min();

        if (minDistance > weapon.Profile.Range)
            throw new InvalidOperationException(
                $"La cible est hors de portée. Distance bord à bord : {minDistance:F2}\", portée maximale de l'arme '{weapon.Name}' : {weapon.Profile.Range}\".");
    }

    private static void ValidateLineOfSight(Figure shootingFigure, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        if (weapon.HasTrait(WeaponTrait.IndirectFire))
            return; // Les armes à tir indirect ignorent la ligne de vue

        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        // Le tir est possible si AU MOINS UNE figurine de l'unité cible est visible par le tireur
        bool isAnyVisible = aliveFigures.Any(targetFig => LineOfSightService.IsVisible(shootingFigure, targetFig, opaqueTerrains));

        if (!isAnyVisible)
        {
            throw new InvalidOperationException(
                $"Aucune figurine de l'unité cible n'est en ligne de vue de la figurine tireur (arme '{weapon.Name}' ne possède pas IndirectFire).");
        }
    }
}
