namespace Wargame.Application.Commands.Shooting.DTOs;

/// <summary>
/// Résultat retourné au client après résolution d'une phase de tir.
/// Sera enrichi au fil des commits (touches, blessures, moral...).
/// </summary>
public record ShootingResultDto(
    int TotalHits,
    int TotalWounds,
    int FiguresDestroyed,
    bool MoraleTestTriggered,
    bool MoraleTestPassed,
    bool TargetPinnedDown
);
