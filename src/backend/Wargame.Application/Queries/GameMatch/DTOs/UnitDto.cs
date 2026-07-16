namespace Wargame.Application.Queries.GameMatch;

public record UnitDto(
    Guid Id,
    string Name,
    string Type,
    string LifecycleStatus,
    string ActivationStatus,
    int AliveFiguresCount,
    bool HasFired,
    bool HasCharged
);
