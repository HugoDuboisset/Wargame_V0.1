namespace Wargame.Domain.Enums;

/// <summary>
/// Altérations d'état négatives affectant une unité.
/// Implémenté comme Flags pour permettre la combinaison de plusieurs effets simultanés
/// (ex: Demoralized | Fleeing).
/// </summary>
[Flags]
public enum StatusEffect
{
    None = 0,

    /// <summary>
    /// Clouée au sol : mouvement réduit à 0" et -1 au tir lors de la prochaine activation.
    /// Gain de +1 niveau de couvert. Se dissipe en début d'activation suivante.
    /// </summary>
    PinnedDown = 1 << 0,

    /// <summary>
    /// En fuite : l'unité doit se déplacer vers le bord de table le plus proche.
    /// Une figurine sortant de la table détruit l'unité.
    /// </summary>
    Fleeing = 1 << 1,

    /// <summary>
    /// Démoralisé : l'unité doit faire un test de moral en début d'activation.
    /// Un succès annule le statut.
    /// </summary>
    Demoralized = 1 << 2,

    /// <summary>
    /// Enflammée : en début d'activation, subit autant de touches de Petit Calibre (D1)
    /// qu'elle compte de figurines vivantes. Le statut est ensuite retiré.
    /// </summary>
    OnFire = 1 << 3,

    /// <summary>
    /// Saturée : mouvement réduit de 2" lors de la prochaine activation.
    /// Causé par le trait d'arme Saturation.
    /// </summary>
    Suppressed = 1 << 4
}
