using Wargame.Domain.Entities;

namespace Wargame.Application.Interfaces.Repositories;

/// <summary>
/// Contrat d'accès aux données pour les armes (Weapon).
/// Principalement utilisé pour charger le catalogue d'armurerie depuis le JSON statique.
/// </summary>
public interface IWeaponRepository
{
    /// <summary>Récupère une arme par son identifiant. Retourne null si non trouvée.</summary>
    Task<Weapon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Récupère tout le catalogue d'armes disponibles.</summary>
    Task<IEnumerable<Weapon>> GetAllAsync(CancellationToken cancellationToken = default);
}
