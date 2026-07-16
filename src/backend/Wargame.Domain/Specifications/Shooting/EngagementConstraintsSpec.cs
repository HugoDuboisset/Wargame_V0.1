using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Specifications.Shooting;

public class EngagementConstraintsSpec : IShootingValidationSpec
{
    public void Validate(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
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
}
