namespace Wargame.Application.Queries.GameMatch;

public record GameMatchResponse(
    Guid Id,
    string Status,
    int TurnNumber,
    Guid? ActivePlayerId,
    List<PlayerDto> Players,
    List<UnitDto> Units
);
