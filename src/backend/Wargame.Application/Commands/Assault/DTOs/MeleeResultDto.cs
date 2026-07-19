namespace Wargame.Application.Commands.Assault.DTOs;

public record MeleeResultDto(
    Dictionary<Guid, int> WoundsLostPerUnit,
    Dictionary<Guid, int> FiguresLostPerUnit,
    Dictionary<Guid, bool> BrutalTriggeredAgainst,
    Guid? LoserUnitId,
    bool MoraleFailed = false
);
