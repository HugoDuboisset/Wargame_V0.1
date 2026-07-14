namespace Wargame.Domain.Enums;

/// <summary>
/// Traits spéciaux d'une arme modifiant les règles standard de résolution.
/// Implémenté comme Flags : une arme peut cumuler plusieurs traits
/// (ex: Handy | Pistol, ou IndirectFire | Explosive).
/// </summary>
[Flags]
public enum WeaponTrait
{
    None = 0,

    // --- Traits d'armes à distance ---

    /// <summary>
    /// Maniable : peut tirer même après un Sprint (-2 au jet de Tir).
    /// </summary>
    Handy = 1 << 0,

    /// <summary>
    /// Encombrant : nécessite que l'unité soit Immobile pour tirer. Empêche l'assaut après tir.
    /// Un véhicule ignore cette contrainte.
    /// </summary>
    Cumbersome = 1 << 1,

    /// <summary>
    /// Chevrotine : à mi-portée ou moins, utilise le profil Gros Calibre pour blesser.
    /// </summary>
    Buckshot = 1 << 2,

    /// <summary>
    /// Pistolet : peut tirer en étant engagé au corps à corps contre l'unité engagée (-2 au Tir).
    /// </summary>
    Pistol = 1 << 3,

    /// <summary>
    /// Ignore le couvert : les malus de couverture adverses sont ignorés.
    /// </summary>
    IgnoreCover = 1 << 4,

    /// <summary>
    /// Tir indirect : peut tirer sans ligne de vue directe (-3 au Tir si c'est le cas).
    /// </summary>
    IndirectFire = 1 << 5,

    /// <summary>
    /// Saturation : si au moins une touche réussie, la cible subit -2" de mouvement à sa prochaine activation.
    /// </summary>
    Suppression = 1 << 6,

    /// <summary>
    /// Incendiaire : si au moins une touche réussie, la cible gagne le statut OnFire.
    /// </summary>
    Incendiary = 1 << 7,

    /// <summary>
    /// Rafales : +2 attaques si la cible est à mi-portée ou moins.
    /// </summary>
    Bursts = 1 << 8,

    /// <summary>
    /// Explosif[X] : chaque touche réussie génère X touches supplémentaires (voir WeaponProfile.ExplosiveHits).
    /// </summary>
    Explosive = 1 << 9,

    // --- Traits d'armes de mêlée ---

    /// <summary>
    /// Balayage : chaque touche réussie génère 1 touche supplémentaire (-1 pour blesser).
    /// </summary>
    Sweep = 1 << 10,

    /// <summary>
    /// Allonge : peut attaquer sans contact socle à socle, si à 1" ou moins de l'ennemi.
    /// </summary>
    Reach = 1 << 11,

    /// <summary>
    /// Déséquilibrant : -2 à l'Initiative lors de la définition de l'ordre de frappe.
    /// </summary>
    Unbalancing = 1 << 12,

    /// <summary>
    /// Parade : +1 au Combat du porteur lors du calcul de différence pour toucher.
    /// </summary>
    Parry = 1 << 13,

    /// <summary>
    /// Brutal : si au moins une figurine est tuée, l'unité adverse subit -1 à son test de Moral de fin de combat.
    /// </summary>
    Brutal = 1 << 14
}
