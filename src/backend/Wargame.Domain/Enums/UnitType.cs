namespace Wargame.Domain.Enums;

/// <summary>
/// Type d'unité, déterminant certaines règles spéciales de la phase de Tir.
/// Les véhicules peuvent tirer avec toutes leurs armes en une seule phase,
/// contrairement à l'infanterie qui choisit une arme par figurine.
/// </summary>
public enum UnitType
{
    Infantry,
    Vehicle
}
