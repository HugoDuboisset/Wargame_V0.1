using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Specifications.Shooting;

public class MovementConstraintsSpec : IShootingValidationSpec
{
    public void Validate(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
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
}
