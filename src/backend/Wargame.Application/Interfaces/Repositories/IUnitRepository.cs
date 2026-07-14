using Wargame.Domain.Entities;

namespace Wargame.Application.Interfaces.Repositories;

/// <summary>
/// Contrat d'accès aux données pour les unités (Unit).
/// Utilisé pour charger les templates d'unités depuis le catalogue JSON,
/// et pour persister l'état des unités en cours de partie.
/// </summary>
public interface IUnitRepository
{
    /// <summary>Récupère une unité par son identifiant. Retourne null si non trouvée.</summary>
    Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Récupère toutes les unités disponibles (catalogue complet).</summary>
    Task<IEnumerable<Unit>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Persiste (crée ou met à jour) une unité.</summary>
    Task SaveAsync(Unit unit, CancellationToken cancellationToken = default);
}
