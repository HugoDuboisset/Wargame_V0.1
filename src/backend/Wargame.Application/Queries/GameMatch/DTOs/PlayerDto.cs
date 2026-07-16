namespace Wargame.Application.Queries.GameMatch;

public record PlayerDto(
    Guid Id,
    string Name,
    int VictoryPoints,
    List<Guid> UnitIds
);
