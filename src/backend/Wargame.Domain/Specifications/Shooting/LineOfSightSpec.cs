using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Domain.Specifications.Shooting;

public class LineOfSightSpec : IShootingValidationSpec
{
    public void Validate(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        if (weapon.HasTrait(WeaponTrait.IndirectFire))
            return; // Les armes à tir indirect ignorent la ligne de vue

        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        // Le tir est possible si AU MOINS UNE figurine de l'unité cible est visible par le tireur
        bool isAnyVisible = aliveFigures.Any(targetFig => LineOfSightService.IsVisible(shooter, targetFig, opaqueTerrains));

        if (!isAnyVisible)
        {
            throw new InvalidOperationException(
                $"Aucune figurine de l'unité cible n'est en ligne de vue de la figurine tireur (arme '{weapon.Name}' ne possède pas IndirectFire).");
        }
    }
}
