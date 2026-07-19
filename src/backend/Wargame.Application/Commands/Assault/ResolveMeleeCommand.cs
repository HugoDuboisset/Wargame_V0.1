using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Assault.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Entities;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Application.Commands.Assault;

/// <summary>
/// Commande pour résoudre une mêlée entre plusieurs unités engagées.
/// </summary>
public record ResolveMeleeCommand(
    Guid GameMatchId,
    List<Guid> EngagedUnitIds
) : IRequest<MeleeResultDto>;

public class ResolveMeleeCommandValidator : AbstractValidator<ResolveMeleeCommand>
{
    public ResolveMeleeCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.EngagedUnitIds).NotEmpty()
            .Must(list => list.Count >= 2).WithMessage("Une mêlée nécessite au moins 2 unités engagées.");
    }
}

public class ResolveMeleeCommandHandler : IRequestHandler<ResolveMeleeCommand, MeleeResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly AssaultResolutionService _resolutionService;
    private readonly MoraleResolutionService _moraleService;
    private readonly ActionResolutionService _actionService;

    public ResolveMeleeCommandHandler(
        IGameMatchRepository repository,
        AssaultResolutionService resolutionService,
        MoraleResolutionService moraleService,
        ActionResolutionService actionService)
    {
        _repository = repository;
        _resolutionService = resolutionService;
        _moraleService = moraleService;
        _actionService = actionService;
    }

    public async Task<MeleeResultDto> Handle(ResolveMeleeCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var engagedUnits = match.Units.Where(u => request.EngagedUnitIds.Contains(u.Id)).ToList();
        
        if (engagedUnits.Count != request.EngagedUnitIds.Count)
            throw new InvalidOperationException("Certaines unités spécifiées sont introuvables dans la partie.");

        foreach (var unit in engagedUnits)
        {
            if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
                throw new InvalidOperationException($"L'unité {unit.Id} n'est pas en état de combattre.");
            if (!unit.IsEngaged())
                throw new InvalidOperationException($"L'unité {unit.Id} n'est pas engagée au corps à corps.");
        }

        // 1. Résolution des combats
        var (woundsLost, figuresLost, brutalTriggered) = _resolutionService.ResolveMelee(engagedUnits);

        // Destruction des unités mortes
        foreach (var unit in engagedUnits)
        {
            if (unit.GetAliveCount() == 0 && unit.LifecycleStatus == UnitLifecycleStatus.Alive)
            {
                unit.Destroy();
            }
        }

        // 2. Définition du perdant (celui qui a perdu le plus de PV)
        Guid? loserId = null;
        int maxWoundsLost = woundsLost.Values.Max();
        
        if (maxWoundsLost > 0)
        {
            var unitsWithMaxLosses = woundsLost.Where(kvp => kvp.Value == maxWoundsLost).ToList();
            if (unitsWithMaxLosses.Count == 1)
            {
                loserId = unitsWithMaxLosses.First().Key;
            }
        }

        bool moraleFailed = false;
        bool riskyActionFailed = false;
        int oppWounds = 0;
        int oppFigures = 0;

        // 3. Gestion du moral pour le perdant
        if (loserId.HasValue)
        {
            var loserUnit = engagedUnits.FirstOrDefault(u => u.Id == loserId.Value && u.LifecycleStatus == UnitLifecycleStatus.Alive);
            if (loserUnit != null)
            {
                bool isBrutal = brutalTriggered.ContainsKey(loserId.Value) && brutalTriggered[loserId.Value];
                bool moralePassed = _moraleService.ResolveMeleeMoraleTest(loserUnit, isBrutal);
                
                if (!moralePassed)
                {
                    moraleFailed = true;

                    // Test d'action risquée pour se désengager
                    bool riskyActionPassed = _actionService.ResolveRiskyAction(loserUnit);
                    if (!riskyActionPassed)
                    {
                        riskyActionFailed = true;
                        
                        // Attaques d'opportunité par les ennemis
                        var enemies = engagedUnits.Where(u => u.Id != loserId.Value && u.LifecycleStatus == UnitLifecycleStatus.Alive).ToList();
                        if (enemies.Any())
                        {
                            var (oW, oF, _) = _resolutionService.ResolveOpportunityAttacks(enemies, loserUnit);
                            oppWounds = oW;
                            oppFigures = oF;

                            if (loserUnit.GetAliveCount() == 0)
                            {
                                loserUnit.Destroy();
                            }
                        }
                    }

                    // Forcer le désengagement
                    var enemiesForLoser = engagedUnits.Where(u => u.Id != loserId.Value).ToList();
                    foreach (var enemy in enemiesForLoser)
                    {
                        loserUnit.Disengage(enemy.Id);
                        enemy.Disengage(loserUnit.Id);
                    }
                }
            }
        }

        // 4. Gestion du désengagement naturel si des unités ennemies sont mortes
        foreach (var unit in engagedUnits.Where(u => u.LifecycleStatus == UnitLifecycleStatus.Alive))
        {
            var enemyUnits = engagedUnits.Where(u => u.Id != unit.Id).ToList();
            bool allEnemiesDead = enemyUnits.All(e => e.LifecycleStatus == UnitLifecycleStatus.Destroyed);
            
            if (allEnemiesDead)
            {
                unit.DisengageAll();
            }
        }

        await _repository.SaveAsync(match, cancellationToken);

        return new MeleeResultDto(woundsLost, figuresLost, brutalTriggered, loserId, moraleFailed, riskyActionFailed, oppWounds, oppFigures);
    }
}
