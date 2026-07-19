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
    private readonly UnitCohesionService _cohesionService;

    public ResolveMeleeCommandHandler(
        IGameMatchRepository repository,
        AssaultResolutionService resolutionService,
        MoraleResolutionService moraleService,
        UnitCohesionService cohesionService)
    {
        _repository = repository;
        _resolutionService = resolutionService;
        _moraleService = moraleService;
        _cohesionService = cohesionService;
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

        var boardTerrains = match.Board.Terrains.ToList();

        // 1. Résolution des combats
        var (woundsLost, figuresLost, brutalTriggered) = _resolutionService.ResolveMelee(engagedUnits, boardTerrains);

        // Destruction des unités mortes et résolution de la perte de cohésion
        var cohesionLostPerUnit = new Dictionary<Guid, int>();
        foreach (var unit in engagedUnits)
        {
            if (unit.GetAliveCount() == 0 && unit.LifecycleStatus == UnitLifecycleStatus.Alive)
            {
                unit.Destroy();
            }
            else if (unit.LifecycleStatus == UnitLifecycleStatus.Alive)
            {
                var (movedFigures, destroyedByCohesion) = _cohesionService.ResolveCohesionLoss(unit);
                if (destroyedByCohesion.Count > 0)
                {
                    cohesionLostPerUnit[unit.Id] = destroyedByCohesion.Count;
                    // Note : Ces morts par cohésion ne comptent pas dans maxWoundsLost pour déterminer le perdant.
                    // Si on voulait qu'elles comptent, on les ajouterait à woundsLost ou figuresLost.
                }
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
                    // L'unité gagne les statuts Routing et Demoralized via le service.
                    // Elle reste Engagée pour le moment. Le désengagement et les éventuelles attaques d'opportunité
                    // seront résolus lors de la commande de mouvement de Flee/Désengagement physique.
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

        return new MeleeResultDto(woundsLost, figuresLost, cohesionLostPerUnit, brutalTriggered, loserId, moraleFailed);
    }
}
