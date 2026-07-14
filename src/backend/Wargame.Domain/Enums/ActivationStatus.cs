namespace Wargame.Domain.Enums;

/// <summary>
/// Statut d'activation d'une unité au cours d'un tour.
/// Suit la séquence d'activations alternées entre joueurs.
/// </summary>
public enum ActivationStatus
{
    Waiting,
    Active,
    Completed
}
