using Wargame.Domain.Enums;

namespace Wargame.Domain.ValueObjects;

/// <summary>
/// Regroupe les 6 caractéristiques de base d'une unité en un objet de valeur immuable.
/// Utilisé comme profil de référence — les effets situationnels (statuts, couverts) ne
/// modifient pas ce profil mais sont calculés à la volée via les méthodes de Unit.
/// </summary>
public record UnitProfile(
    
    double Movement,
    int Shooting,
    int Combat,
    int Initiative,
    int Morale,

    ArmorClass ArmorClass
);
