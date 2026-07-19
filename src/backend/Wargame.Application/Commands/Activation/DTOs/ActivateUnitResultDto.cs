using Wargame.Domain.Enums;

namespace Wargame.Application.Commands.Activation.DTOs;

public record ActivateUnitResultDto(
    bool WasDemoralized,
    bool MoralePassed,
    StatusEffect ActiveStatusEffectsAfterActivation
);
