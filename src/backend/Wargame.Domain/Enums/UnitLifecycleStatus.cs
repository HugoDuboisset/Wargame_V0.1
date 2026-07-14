namespace Wargame.Domain.Enums;

/// <summary>
/// Statut du cycle de vie d'une unité sur la table de jeu.
/// </summary>
public enum UnitLifecycleStatus
{
    Alive,
    Destroyed,
    Escaped //est sortie de la table suite à une fuite. 
}
