using Wargame.Domain.Entities;

namespace Wargame.Application.Interfaces.Repositories;

/// <summary>
/// Contrat d'accès aux données pour l'agrégat GameMatch.
/// Implémenté dans l'Infrastructure (ex: JsonGameMatchRepository).
/// </summary>
public interface IGameMatchRepository
{
    /// <summary>Récupère une partie par son identifiant. Retourne null si non trouvée.</summary>
    Task<GameMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persiste (crée ou met à jour) l'état d'une partie.</summary>
    Task SaveAsync(GameMatch gameMatch, CancellationToken cancellationToken = default);
}
