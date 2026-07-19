namespace Wargame.Application.Commands.Movement.DTOs;

public record FleeResultDto(
    bool RiskyActionFailed,
    int OpportunityAttacksWounds,
    int OpportunityAttacksFigures,
    bool DestroyedByFleeing
);
