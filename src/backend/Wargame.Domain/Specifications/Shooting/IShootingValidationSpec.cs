using Wargame.Domain.Entities;

namespace Wargame.Domain.Specifications.Shooting;

/// <summary>
/// Interface pour les règles de validation du tir.
/// </summary>
public interface IShootingValidationSpec
{
    /// <summary>
    /// Valide une condition métier. Jette une InvalidOperationException si la condition n'est pas remplie.
    /// </summary>
    void Validate(Figure shooter, Unit shootingUnit, Unit targetUnit, Weapon weapon, List<Terrain> opaqueTerrains);
}
