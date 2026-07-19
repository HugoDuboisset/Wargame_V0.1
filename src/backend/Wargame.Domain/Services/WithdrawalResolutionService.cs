using Wargame.Domain.Entities;
using Wargame.Domain.Enums;

namespace Wargame.Domain.Services;

public record WithdrawalResult(bool RiskyActionFailed, int OpportunityWoundsLost, int OpportunityFiguresLost);

/// <summary>
/// Service gérant la rupture de l'engagement en mêlée (retrait volontaire ou fuite).
/// Coordonne le test d'Action Risquée, les attaques d'opportunité, et la mise à jour des statuts Engaged.
/// </summary>
public class WithdrawalResolutionService
{
    private readonly ActionResolutionService _actionService;
    private readonly AssaultResolutionService _assaultResolutionService;

    public WithdrawalResolutionService(
        ActionResolutionService actionService,
        AssaultResolutionService assaultResolutionService)
    {
        _actionService = actionService;
        _assaultResolutionService = assaultResolutionService;
    }

    /// <summary>
    /// Exécute la procédure de désengagement (volontaire ou fuite) pour l'unité.
    /// </summary>
    /// <param name="fleeingUnit">L'unité qui tente de partir.</param>
    /// <param name="match">La partie en cours, contenant toutes les unités.</param>
    /// <returns>Bilan de la tentative (réussite ou échec de l'action risquée, et dégâts subis).</returns>
    public WithdrawalResult ResolveWithdrawal(Unit fleeingUnit, GameMatch match)
    {
        var enemyUnits = match.Units
            .Where(u => fleeingUnit.EngagedWithUnitIds.Contains(u.Id) && u.LifecycleStatus == UnitLifecycleStatus.Alive)
            .ToList();

        bool riskyActionFailed = false;
        int oppWounds = 0;
        int oppFigures = 0;

        if (enemyUnits.Any())
        {
            // Test d'action risquée
            bool passRiskyAction = _actionService.ResolveRiskyAction(fleeingUnit);
            if (!passRiskyAction)
            {
                riskyActionFailed = true;

                // Résolution des attaques d'opportunité
                var oppResult = _assaultResolutionService.ResolveOpportunityAttacks(enemyUnits, fleeingUnit);
                oppWounds = oppResult.WoundsLost;
                oppFigures = oppResult.FiguresLost;
            }

            // Mettre à jour le statut Engaged des ennemis qui survivaient à la fuite de l'unité
            DisengageEnemies(match, fleeingUnit.Id, enemyUnits);
        }

        // L'unité en fuite n'est plus engagée avec personne
        fleeingUnit.DisengageAll();

        return new WithdrawalResult(riskyActionFailed, oppWounds, oppFigures);
    }

    private void DisengageEnemies(GameMatch match, Guid fleeingUnitId, List<Unit> enemies)
    {
        foreach (var enemy in enemies)
        {
            enemy.Disengage(fleeingUnitId);
            // Vérifier si l'ennemi n'a plus d'adversaires
            var otherOpponents = match.Units.Where(u => enemy.EngagedWithUnitIds.Contains(u.Id) && u.LifecycleStatus == UnitLifecycleStatus.Alive).ToList();
            if (!otherOpponents.Any())
            {
                enemy.DisengageAll();
            }
        }
    }
}
