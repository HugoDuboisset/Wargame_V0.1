using FluentValidation;
using MediatR;
using Wargame.Application.Commands.Activation.DTOs;
using Wargame.Application.Interfaces.Repositories;
using Wargame.Domain.Enums;
using Wargame.Domain.Services;

namespace Wargame.Application.Commands.Activation;

/// <summary>
/// Commande pour marquer une unité comme active pendant le tour.
/// Gère les tests de moral de début d'activation pour les unités démoralisées.
/// </summary>
public record ActivateUnitCommand(Guid GameMatchId, Guid UnitId) : IRequest<ActivateUnitResultDto>;

public class ActivateUnitCommandValidator : AbstractValidator<ActivateUnitCommand>
{
    public ActivateUnitCommandValidator()
    {
        RuleFor(x => x.GameMatchId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
    }
}

public class ActivateUnitCommandHandler : IRequestHandler<ActivateUnitCommand, ActivateUnitResultDto>
{
    private readonly IGameMatchRepository _repository;
    private readonly MoraleResolutionService _moraleService;

    public ActivateUnitCommandHandler(IGameMatchRepository repository, MoraleResolutionService moraleService)
    {
        _repository = repository;
        _moraleService = moraleService;
    }

    public async Task<ActivateUnitResultDto> Handle(ActivateUnitCommand request, CancellationToken cancellationToken)
    {
        var match = await _repository.GetByIdAsync(request.GameMatchId, cancellationToken);
        if (match == null)
            throw new InvalidOperationException("Partie introuvable.");

        if (match.Status != GameStatus.InProgress)
            throw new InvalidOperationException("La partie n'est pas en cours.");

        var unit = match.Units.FirstOrDefault(u => u.Id == request.UnitId);
        if (unit == null)
            throw new InvalidOperationException("Unité introuvable dans cette partie.");

        if (unit.LifecycleStatus != UnitLifecycleStatus.Alive)
            throw new InvalidOperationException("L'unité n'est pas en état de combattre.");

        if (unit.ActivationStatus != ActivationStatus.Waiting)
            throw new InvalidOperationException("L'unité a déjà été activée ce tour.");

        if (match.ActivePlayerId != null)
        {
            var activePlayer = match.Players.FirstOrDefault(p => p.Id == match.ActivePlayerId);
            if (activePlayer != null && !activePlayer.UnitIds.Contains(unit.Id))
            {
                throw new InvalidOperationException("L'unité n'appartient pas au joueur actif.");
            }
        }

        unit.SetActivationStatus(ActivationStatus.Active);

        bool wasDemoralized = unit.IsDemoralized();
        bool moralePassed = true;

        if (wasDemoralized)
        {
            // Le test de moral "générique" pour une unité au début de son activation
            // Lance 1D10 <= Moral
            moralePassed = _moraleService.ResolveMoraleTest(unit);
            
            if (moralePassed)
            {
                // Un test réussi annule Demoralized et Routing
                unit.RemoveStatusEffect(StatusEffect.Demoralized);
                unit.RemoveStatusEffect(StatusEffect.Routing);
                
                // ATTENTION: PinnedDown n'est PAS retiré ici ! 
                // Il reste actif pour subir les malus pendant cette activation,
                // et sera dissipé à la fin du tour par ResetForNewTurn().
            }
        }

        await _repository.SaveAsync(match, cancellationToken);

        return new ActivateUnitResultDto(wasDemoralized, moralePassed, unit.ActiveStatusEffects);
    }
}
