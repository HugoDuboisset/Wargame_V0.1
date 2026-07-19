using Wargame.Domain.Enums;

namespace Wargame.Application.Commands.Movement.DTOs;

public record DisengageResultDto(
    bool WasForcedFlee,
    bool RiskyActionFailed,
    int OpportunityAttacksWounds,
    int OpportunityAttacksFiguresLost,
    bool UnitDestroyedByFleeingOffBoard
);
