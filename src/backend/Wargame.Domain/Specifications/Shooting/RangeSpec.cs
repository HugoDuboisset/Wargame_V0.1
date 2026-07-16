using Wargame.Domain.Entities;

namespace Wargame.Domain.Specifications.Shooting;

public class RangeSpec : IShootingValidationSpec
{
    public void Validate(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        // La portée est vérifiée de la figurine tireur à la figurine cible la plus proche (bord à bord)
        var aliveFigures = targetUnit.Figures.Where(f => f.IsAlive).ToList();
        if (!aliveFigures.Any()) return;

        double minDistance = aliveFigures
            .Select(targetFig => shooter.GetEdgeDistanceTo(targetFig))
            .Min();

        if (minDistance > weapon.Profile.Range)
            throw new InvalidOperationException(
                $"La cible est hors de portée. Distance bord à bord : {minDistance:F2}\", portée maximale de l'arme '{weapon.Name}' : {weapon.Profile.Range}\".");
    }
}
