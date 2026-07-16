using Wargame.Domain.Entities;
using Wargame.Domain.Specifications.Shooting;

namespace Wargame.Domain.Services;

/// <summary>
/// Service du domaine responsable de valider si un tir est possible (Ligne de vue, Portée, Mouvement, Engagement).
/// </summary>
public class ShootingValidationService
{
    private readonly IEnumerable<IShootingValidationSpec> _specs;

    public ShootingValidationService(IEnumerable<IShootingValidationSpec> specs)
    {
        _specs = specs ?? Enumerable.Empty<IShootingValidationSpec>();
    }

    public void ValidateShot(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains)
    {
        foreach (var spec in _specs)
        {
            spec.Validate(shooter, shootingUnit, targetUnit, weapon, opaqueTerrains);
        }
    }
}
