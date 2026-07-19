# Titre à définir
## Core rules V0.1
01/07/2026

---

## Table des matières

- Préambule
- Introduction et matériel
- Concepts fondamentaux
  - Mécaniques de base
  - Jets de dés
    - Jets combat
    - Jets de moral
    - Jets d'actions / actions risquées
  - Lignes de vue et mesures
  - Cohésion d'unité
- Séquencement de jeu et Activation
  - Phase d'activation
  - Phase mouvement
    - Rester Immobile
    - Mouvement normal
    - Sprint
    - Désengagement
  - Phase de tir
  - Phase d'assaut
  - Phase de mêlée
- Profils des unités
- Résolution des tirs
  - Définition des lignes de vue
  - Sélection des armes
  - Jet de Touche
  - Jet de blessure
  - Résolution
  - Retrait des pertes
  - Moral
- Résolution des Assauts
- Résolution des Mêlées
  - Ordre de combat
  - Jet pour Toucher
  - Jet de blessure
  - Résolution
  - Retrait des pertes
  - Définition du perdant et moral
- Terrains et système de couverture
  - Zones d'occupation (Couvert interne)
  - Zones d'interférences (Couvert traversant)
  - Les zones opaques (Couverts bloquants)
  - Combinaison de décors
  - Profil de couvert
    - Couverts légers
    - Couverts intermédiaires
    - Couverts lourds
  - Exemples de terrains
- Matrices
  - Matrice de touche au combat au corps à corps
  - Matrice de blessure au Tir
  - Matrice de blessure au Combat
- Armurerie
  - Armes à distance
  - Armes de mêlée
- Traits d'armes
  - Armes à distance
  - Armes de mêlée
- Scénario de référence
  - Annihilation
  - Force Opérationnelle Générique (500 Points)

---

## Préambule

Ce projet de jeu est une création amateur. Il s'agit pour moi de travailler sur ma capacité à mener un projet, le découper, le tester et l'organiser. L'objectif est d'aboutir à une version jouable avec des figurines physiques, mais aussi d'une application web.

Joueur de wargames depuis maintenant plusieurs années, il me semblait intéressant de m'essayer à l'exercice de la création d'un moteur de jeu. Celui-ci se veut simple mais réaliste, permettant des parties immersives et rapides, ne nécessitant pas une relecture constante des règles.

Ce document est une première ébauche, visant notamment à mettre en forme ce qui sera le support d'une future application. Il sera régulièrement mis à jour.

## Introduction et matériel :

Il s'agit d'un jeu dit "agnostique" : il n'y a pas de figurines spécialement dédiées à celui-ci et il est donc tout à fait possible d'utiliser n'importe quelle gamme existante cohérente.

Les éléments principaux à respecter sont la taille des socles, ainsi que le WYSIWYG (correspondance entre l'équipement porté par l'unité sur la fiche, et ce qui est visible sur la figurine).

Sur ce dernier point, il est de bon d'être aussi tolérant avec son adversaire que l'on est rigoureux avec soi-même. Jouer des figurines peintes sur une jolie table est évidemment un plus non négligeable.

En plus de figurines, vous aurez besoin de D3, D6 et D10, ainsi que d'un mètre ruban en unités impériales, et de gabarits.

Le jeu se déroule sur une surface plane de minimum 50" x 40", ainsi que de décors.

Le système de jeu utilise le système d'activations alternées. Les joueurs jouent une unité l'un après l'autre, jusqu'à l'action complète de tout ce qui est présent sur la table. Une fois toutes les unités jouées, un nouveau tour commence.

Les batailles représenteront des escarmouches d'une dizaine d'unités.

Une partie se termine au bout de 6 tours de jeu, quand un objectif principal est complété, ou qu'une armée est entièrement détruite ou en déroute.

Une partie se gagne si l'on a marqué plus de points, ou entièrement détruit ou mis en déroute l'armée adverse.

## Concepts fondamentaux

### Mécaniques de base

Le jeu fonctionne par alternance. Une partie est composée de 6 tours maximum.

Un tour est composé de l'activation de toutes les unités de tous les joueurs. Une fois toutes les unités activées, un nouveau tour commence. Au début de chaque tour (dont le premier), chaque joueur lance 1D10. Celui ayant plus grand résultat choisit de prendre la première activation, ou de laisser la main à son adversaire.

Si un joueur a plus d'activations que son adversaire, il joue ses activations dans l'ordre souhaité jusqu'à la fin de son tour.

Une unité est un ensemble de figurines agissant en même temps et appartenant au même groupe. Ils sont regroupés sous la même fiche d'unité.

L'essentiel des actions se font avec des jets de dés.

### Jets de dés

Des jets peuvent recevoir des modificateurs positifs ou négatifs. Dans ce cas, on les applique une fois le résultat obtenu.

#### Jets combat :

Ils se font en utilisant des D10.

Ces jets permettent de toucher, blesser ou sauvegarder.

Un résultat supérieur ou égal à la valeur cible est une réussite. Un 1 obtenu sur un dé est toujours un échec, même avec des modificateurs.

Par exemple : un jet de tir touche sur 7+. Un résultat supérieur ou égal à 7 est une réussite. Un résultat strictement inférieur à 7 est un échec.

#### Jets de moral :

Un test de moral est obtenu en lançant 1D10, et en obtenant un résultat inférieur ou égal au moral de l'unité.

#### Jets d'actions / actions risquées

Un jet d'action risquée permet de faire des actions spéciales. Il se fait en lançant 1D10 et en comparant le résultat obtenu avec la valeur d'Initiative de l'unité. Un résultat inférieur à l'Initiative permet la réussite de l'action.

Action risquée : l'échec à un jet d'action risquée entraîne la fin de l'activation de l'unité. L'action en cours est terminée, puis l'activation prend fin.

Une unité ne peut entreprendre qu'une seule action ou action risquée par activation.

### Lignes de vue et mesures

Les lignes de vues se prennent par n'importe quel point du socle d'une figurine. Elles doivent permettre d'atteindre n'importe quel point du socle d'une autre figurine. Si c'est le cas, une figurine est visible. Si rien ne coupe les lignes de vue entre les socles des deux figurines, elles sont parfaitement visibles. S'il est possible de passer par un élément obstruant, elles sont partiellement visibles. S'il n'est possible de passer que par des éléments obstruants, elles ne sont pas visibles.

Les mesures entre deux figurines se font socle à socle (même si la figurine déborde du socle).

### Cohésion d'unité

Les figurines d'une unité doivent être placées de façon à être en cohésion. Dans les unités de plus de 5 figurines, et figurines doivent être placées à 2" d'au moins 2 autres figurines de l'unité. En dessous, toute figurine doit être placée à 2" d'au moins 1 autre figurine de l'unité.

Les unités ne pouvant pas être placées en cohésion d'unité sont détruites (on considère ces personnages comme déserteurs)

## Séquencement de jeu et Activation

Le jeu utilise un système d'action alternée. Les joueurs activent une unité à tour de rôle. Quand il n'y a plus d'unités jouables par l'un des joueurs, le joueur à qui il reste des unités peut toutes les jouer l'une après l'autre.

L'activation d'une unité suit ensuite ces phases :
- Phase d'Activation
- Phase de Mouvement
- Phase de Tir
- Phase d'Assaut
- Phase de Mêlée

### Phase d'activation

L'unité s'active. Les divers effets activables à cette phase sont appliqués.

Si l'unité est démoralisée, elle doit faire un test de moral. Un test réussi annule le statut. Sinon elle reste démoralisée.

Si l'unité était clouée au sol, elle garde son statut pour sa prochaine activation. Si l'unité était en fuite, elle doit faire un mouvement normal du maximum de sa capacité de mouvement vers le bord de table le plus proche. Si une figurine de l'unité sort de la table, l'unité est détruite.

### Phase mouvement

L'unité effectue un mouvement au choix parmi ceux listés ci-dessous. Aucun mouvement ne peut être effectué quand une unité est engagée au corps à corps avec une unité ennemie. Aucun mouvement ne doit permettre d'arriver au contact socle à socle avec une unité ennemie.

Une unité peut traverser une autre unité amie. Elle ne peut pas traverser une unité ennemie, ni un terrain infranchissable.

#### Rester Immobile

L'unité reste sur place, et ne fait aucun mouvement. Un véhicule peut pivoter.

#### Mouvement normal

L'unité avance d'une partie ou de l'entièreté de sa caractéristique de mouvement

#### Sprint

L'unité fait un sprint, elle peut se déplacer du double de sa caractéristique de mouvement.

#### Désengagement

Une unité au corps à corps peut s'extraire de celui-ci. Il s'agit du seul mouvement permettant de sortir d'un corps à corps. Dans ce cas, l'unité fait un mouvement normal et toutes les figurines doivent ne plus être en contact socle à socle avec une figurine ennemie.

Le Désengagement nécessite de faire un test d'action risquée.

En cas de réussite, l'unité peut faire son mouvement.

En cas d'échec, l'unité (ou les unités) ennemies avec laquelle elle était engagée peut faire une attaque d'opportunité. Le mouvement a lieu, mais l'unité est considérée comme en fuite (il s'agit plus d'une fuite désordonnée que d'un désengagement en bon ordre).

Une attaque d'opportunité est une attaque normale avec un malus de -3 pour toucher. 

Un désengagement ayant lieu lors de la phase de mouvement ne donne pas de statut en fuite. 

### Phase de tir

Les unités disposant d'armes à distance peuvent faire feu.

Une unité engagée au corps à corps ne peut pas faire d'attaque à distance.

Exceptions : Les figurines disposant d'une arme ayant le trait pistolet peuvent tirer au corps à corps, contre l'unité avec laquelle elle est engagée en mêlée. Un véhicule engagé au corps à corps peut tirer avec ses armes sur les unités avec lesquelles il est engagé au corps à corps, mais aussi contre des unités avec lesquelles il n'est pas engagé au corps à corps.

Tirer en étant engagé au corps à corps donne un malus à la capacité de tir.

Une unité étant restée immobile peut tirer avec tous types d'armes.

Une figurine ayant fait un sprint ne peut tirer qu'avec des armes maniables, avec un malus sur la capacité de tir.

Voir règles suivantes pour les détails sur la gestion du tir.

### Phase d'assaut

Les unités peuvent tenter un assaut. Un assaut a pour but de mener l'unité au contact d'une unité ennemie. Il s'agit de la seule façon d'entrer au contact d'une unité ennemie.

Une unité ayant sprinté ne peut pas charger.

Voir règles suivantes pour les détails sur la gestion de l'Assaut.

### Phase de mêlée

Les unités engagées au corps à corps combattent par ordre d'initiative. Le perdant effectue un test de moral et fuit en cas d'échec.

## Profils des unités

Chaque unité est représentée par une fiche détaillant les caractéristiques des figurines qui la composent.

- **Mouvement (M)** : c'est la distance de déplacement de base, en "
- **Tir (T)** : c'est le score à atteindre sur un dé pour toucher une cible à distance
- **Combat (C)** : c'est la compétence martiale, utilisée pour la confrontation en mêlée
- **Initiative (I)** : c'est l'agilité, utilisée pour définir l'ordre de frappe, fuir un combat, ou mener certaines actions
- **Moral (Mo)** : c'est le courage, permettant de mesurer la capacité à tenir sous le feu ennemi ou après avoir perdu un combat
- **Classe d'Armure (CA)** : c'est la catégorie d'armure portée, elle définit la résistance
  - types d'armures : Sans armure, Léger, Lourd, Véhicule léger, Véhicule lourd
- **Points de vie (PV)** : correspond à la santé, s'il tombe à 0, la figurine est détruite

Les unités portent aussi de l'équipement, générique (pouvant être porté par tous) ou spécialisé (spécifique à cette unité).

Il peut s'agir d'armes, de protections ou d'utilisables (kits de soin, grenades, mines…).

Enfin, le profil d'unité décrit aussi des capacités spéciales propres à chaque unité.

**Types de CA :**
- CA 0 : correspond à une cible sans protection balistique
- CA 1 : correspond à une protection légère (Kevlar, gilet pare balles souple)
- CA 2 : correspond à une protection lourde (Porte plaques, armure balistique…)
- CA 3 : correspond aux véhicules légers (véhicules civils, motos, jeep, trucks, drones)
- CA 4 : correspond aux véhicules blindés (chars, drônes lourds)

## Résolution des tirs

### Définition des lignes de vue

L'attaquant trace une ligne de vue entre chaque figurine de son unité, et les figurines de l'unité ennemie. Seules les unités ayant une ligne de vue directe ou traversant un élément de décors qui n'est pas bloquant peuvent tirer.

### Sélection des armes

Si les figurines de l'unité ont plusieurs armes, le joueur choisit une arme par figurine, qui sera utilisée pour faire feu.

Exception pour les véhicules qui peuvent tirer avec toutes leurs armes pendant une phase.

Chaque type d'arme peut tirer sur une cible différente. Par exemple, les fusils d'un groupe peuvent cibler l'infanterie ennemie, et le RPG un char.

### Jet de Touche

Le jet se fait avec un D10.

Le joueur attaquant jette un dé par figurine de l'unité qui fait feu. Il est conseillé d'utiliser de faire des groupes de dés par types d'armes. Les dés doivent atteindre ou dépasser le Tir du tireur.

Le nombre de dés à lancer est défini par la caractéristique d'Attaque (A) de l'arme.

Des malus liés au couvert peuvent être appliqués. Si l'unité ciblée est partiellement cachée par un élément donnant le couvert, alors un couvert s'applique.

- Couvert Léger : -1 au résultat du dé
- Couvert intermédiaire : -2 au résultat du dé
- Couvert Lourd : -3 au résultat du dé

Un 10 au jet avant modificateur est toujours une réussite.

### Jet de blessure

Le jet se fait avec un D10.

Les tirs ayant réussi doivent ensuite blesser.

Pour chaque jet de touche réussi, les dés sont relancés, et les résultats comparés sur la Matrice de dégâts.

### Résolution

Chaque réussite au jet de blessure retire autant de points de vie que la valeur de Dégât (D) de l'arme. Les dégâts excédant les points d'une figurine ne sont pas transmis à une autre figurine.

### Retrait des pertes

Seules les figurines visibles par l'unité attaquante sont retirées, en commençant par les plus proches de l'unité attaquante.

### Moral

Une unité dont l'effectif tombe à 50% ou moins doit faire un test de moral.

En cas d'échec, elle est Clouée au sol. Elle est considérée comme démoralisée jusqu'à sa prochaine activation.

Sa caractéristique de mouvement est réduite à 0 lors de la prochaine activation, et subit un malus de -1 pour toucher au tir.

Elle reçoit le bénéfice du couvert, et gagne 1 niveau de couvert (Les soldats plongent au sol pour échapper aux tirs).

## Résolution des Assauts

Une charge doit faire arriver au contact deux unités. Elle est considérée comme réussie si au moins une figurine de l'unité arrive au contact socle à socle avec au moins une figurine de l'unité ennemie.

Pour se faire, l'unité déclarant une charge jette 1D6 et y ajoute sa caractéristique de mouvement. Cela correspond à la distance couverte par la charge. Toutes les figurines de l'unité peuvent alors se déplacer de cette distance, avec l'obligation de se rapprocher de l'unité ennemie. 
Si résultat ne permet d'arriver en contact socle à socle, la charge échoue et l'unité ne se déplace pas.

En cas de réussite, l'unité se reforme pour maximiser les mises en contact entre attaquants et défenseurs. Ce mouvement de consolidation est un mouvement de 2" maximum, qui doit permettre de mettre le plus de figurines possibles au contact de l'ennemi.

## Résolution des Mêlées

Toutes les unités engagées au corps à corps combattent, et pas uniquement celles du joueur actif.

### Ordre de combat

Les combats se font par ordre d'initiative décroissante. Les figurines ayant la même valeur d'initiative attaquent simultanément.

Un bonus de +2 en initiative est accordé aux figurines ayant mené un assaut lors de cette activation.

### Jet pour Toucher

Le jet se fait avec un D10.

Les figurines au contact d'une autre figurine peuvent attaquer.

Chaque figurine éligible choisit une arme de mêlée utilisée lors du combat.

Le nombre d'attaques est défini par la caractéristique Attaque (A) de l'arme utilisée.

Le jet de touche fonctionne en opposition. L'attaquant compare sa caractéristique de Combat à celle du défenseur.

Voir section de matrices pour les résultats à obtenir.

Certains équipements comme les boucliers augmentent la capacité de C en défense ou attaque.

### Jet de blessure

Les attaques ayant réussi doivent ensuite blesser.

Pour chaque jet de touche réussi, les dés sont relancés, et les résultats comparés sur la Matrice de dégâts.

### Résolution

Chaque réussite au jet de blessure retire autant de points de vie que la valeur de Dégât (D) de l'arme. Les dégâts excédant les points d'une figurine ne sont pas transmis à une autre figurine.

### Retrait des pertes

Les figurines tuées sont ensuite retirées.

### Définition du perdant et moral

L'unité ayant perdu le plus de PV au cours du combat est considérée comme étant l'unité perdante. Elle doit faire un test de morale. Si elle échoue, l'unité perdante est obligée de faire un mouvement de désengagement (impliquant le test d'action risquée) devant obligatoirement la rapprocher du bord de table le plus proche. 

Ce mouvement de fuite se fait à la fin de phase de combat, après le test de moral. 

Si une figurine sort de la table lors de ce mouvement, l'unité est considérée comme ayant fui la bataille, et est détruite.

Une unité en fuite a le statut Démoralisé et En fuite jusqu'à sa prochaine activation.

## Terrains et système de couverture

Un champ de bataille prend de l'intérêt par ses zones de couvertures permettant aux troupes de se cacher, se protéger des tirs ennemis et tendre des embuscades.

Ces décors sont répartis en plusieurs types

### Zones d'occupation (Couvert interne)

Ces zones représentent des décors en creux comme des cratères. Elles ne masquent pas le paysage, mais protègent ceux qui s'y cachent.

Mécanique : la ligne de vue peut les traverser sans pénalité. Cependant, si le socle d'une figurine de l'unité se trouve à l'intérieur de cette zone, elle bénéficie d'un couvert.

Exemples : cratères, tranchées, ou décombres au sol

### Zones d'interférences (Couvert traversant)

Ces zones représentent des environnements denses ou des obstacles fins. Elles gênent la visibilité mais ne l'annulent pas

Mécanique : Si la ligne de vue tracée entre le tireur et la cible traverse les limites de cette zone, la cible bénéficie du couvert, même si l'unité ciblée ne se trouve pas dans la zone.

Exemple : grillages, champ d'herbes hautes, bâtiment effondré, voiture détruite

### Les zones opaques (Couverts bloquants)

Ces zones sont des obstacles physiques massifs ne cachant de la vue et des projectiles

Mécanique : La ligne de vue est bloquée par cette zone. Si une ligne de vue tracée entre deux unités passe obligatoirement par cet élément de décors, l'unité attaquante ne voit pas sa cible et le tir est impossible.

Exemples : Mur en béton armé sans fenêtres, bâtiment debout, conteneur, épave de char

Ces décors peuvent se manifester par des zones ou des éléments isolés.

### Combinaison de décors

Un décors peut cumuler plusieurs catégories.

Par exemple, une zone de forêt dense est à la fois une zone d'occupation (les unités s'y trouvant bénéficient du couvert) et une zone d'interférence (elle gêne les tirs vers des unités derrière celle-ci).

Un autre exemple est le bâtiment effondré, qui peut cumuler tous les types de décors : se trouver dans les décombres permet de bénéficier du couvert de la zone d'occupation. Se trouver derrière permet de bénéficier du couvert de la zone d'interférence. Un mur encore debout peut créer des zones opaques.

### Profil de couvert

Un décor peut donner un couvert Léger, Intermédiaire ou Lourd.

Ils provoquent un malus sur un jet de touche au tir ciblant une unité bénéficiant du couvert.

- Léger : -1 pour toucher
- Intermédiaire : -2 pour toucher
- Lourd : -3 pour toucher

L'efficacité du couvert est à définir en fonction de celui-ci.

Se trouver dans un couvert permet aussi de mieux se préparer à subir un assaut : l'ennemi doit escalader des pans de murs, se frayer un chemin au travers de débris, ou sauter dans une tranchée.

Ainsi, une unité étant la cible d'un assaut se trouvant dans une zone d'occupation bénéficie d'un bonus d'initiative lors de l'activation où elle subit l'assaut, afin de représenter l'avantage des défenseurs.

- Léger : +1 à l'Initiative
- Intermédiaire : +2 à l'Initiative
- Lourd : +3 à l'Initiative

#### Couverts légers

Représentent des obstacles qui gênent essentiellement la visibilité, mais n'arrêtent pas les projectiles.

Par exemple : cratères peu profonds, hautes herbes, lignes de barbelés, grillage…

#### Couverts intermédiaires

Représentent des couvertures pouvant cacher un soldat de la vue du tireur, et pouvant bloquer un tir.

Par exemple : Tranchée d'infanterie, carcasse de camion ou de voiture, muret, mur effondré…

#### Couverts lourds

Représentent des positions fortifiées conçues pour protéger des soldats d'assauts.

Par exemple : Bunker, mur de béton armé, nid de mitrailleuse, maison encore debout…

### Exemples de terrains

| Nom du Terrain | Règles Géométriques | Valeur | Application Tactique en Jeu |
|---|---|---|---|
| Cratère d'Obus | Occupation | 1 (Léger) | Ne gêne pas la vue du champ de bataille, mais offre un petit abri (-1 Tir, +1 Init) à l'unité qui s'y terre. |
| Champ de Hautes Herbes | Interférence | 1 (Léger) | Si la ligne de vue passe à travers, la cible a -1 au Tir. Personne ne gagne d'Initiative en mêlée, car il n'y a pas d'obstacle physique à franchir. |
| Forêt Dense | Occupation + Interférence | 1 (Léger) | Protège l'unité à l'intérieur (-1 Tir, +1 Init) et gêne les tirs qui tentent de traverser le bois de part en part pour toucher quelqu'un derrière (-1 Tir). |
| Barricade de fortune | Occupation | 2 (Intermédiaire) | Un muret de gravats. Offre une excellente protection (-2 Tir) et ralentit sérieusement une charge ennemie (+2 Init pour le défenseur). |
| Rideau de Fumée Épaisse | Interférence | 2 (Intermédiaire) | Bloque sévèrement la vision de ceux qui tirent à travers (-2 Tir), mais n'offre aucune protection physique en mêlée. |
| Bâtiment Effondré | Occupation + Interférence | 2 (Intermédiaire) | Le classique de la guerre urbaine. Abri solide à l'intérieur (-2 Tir, +2 Init) et bloque en grande partie les tirs qui traversent la zone. |
| Tranchée Fortifiée | Occupation | 3 (Lourd) | Un cauchemar pour l'attaquant. Quasiment intouchable au tir (-3) et confère un avantage massif en cas d'assaut (+3 Init). |
| Mur de Béton Intact | Opaque | Aucun | L'obstacle absolu. La ligne de vue ne peut pas le traverser, les tirs sont impossibles. Force le contournement. |
| Épave de Char d'Assaut | Occupation + Opaque | 2 (Intermédiaire) | La carcasse est Opaque (bloque les lignes de vue de part en part), mais monter sur les débris autour offre un couvert Lourd de zone. |

## Matrices

### Matrice de touche au combat au corps à corps

| Différence de Combat (Attaquant - Défenseur) | Résultat pour Toucher (D10) |
|---|---|
| +3 ou plus | 2+ |
| +2 | 3+ |
| +1 | 4+ |
| 0 | 5+ |
| -1 | 6+ |
| -2 | 7+ |
| -3 ou moins | 8+ |

### Matrice de blessure au Tir

| Type de calibre | CA 0 | CA 1 | CA 2 | CA 3 | CA 4 |
|---|---|---|---|---|---|
| Petit calibre | 3+ | 7+ | 9+ | - | - |
| Calibre moyen | 2+ | 4+ | 7+ | 9+ | - |
| Calibre perforant | 4+ | 3+ | 5+ | 7+ | - |
| Gros calibre | 2+ | 2+ | 4+ | 6+ | 9+ |
| Antichar | 5+ | 3+ | 3+ | 2+ | 5+ |

### Matrice de blessure au Combat

| Type d'arme | CA 0 | CA 1 | CA 2 | CA 3 | CA 4 |
|---|---|---|---|---|---|
| Légère | 4+ | 5+ | 8+ | - | - |
| Moyenne | 3+ | 4+ | 7+ | - | - |
| Lourde | 3+ | 3+ | 5+ | 9+ | - |
| Thermique | 4+ | 3+ | 3+ | 4+ | 6+ |

## Armurerie

### Armes à distance

| Nom de l'Arme | Portée | A | D | Calibre | Traits Spéciaux |
|---|---|---|---|---|---|
| Pistolet de Combat | 12" | 2 | 1 | Petit Calibre | Pistolet, Maniable |
| Pistolet-Mitrailleur (SMG) | 18" | 3 | 1 | Petit Calibre | Rafales, Maniable |
| Fusil à Pompe Tactique | 14" | 2 | 1 | Petit Calibre | Chevrotine, Maniable |
| Fusil d'Assaut Standard | 24" | 2 | 1 | Calibre Moyen | - |
| Fusil de Combat Lourd | 30" | 1 | 1 | Gros Calibre | - |
| Mitrailleuse Légère (LMG) | 30" | 4 | 1 | Calibre Moyen | Saturation, Encombrant |
| Mitrailleuse Lourde (HMG) | 36" | 4 | 1 | Gros Calibre | Saturation, Encombrant |
| Fusil de Précision | 48" | 1 | 2 | Calibre Perforant | Ignore le couvert, Encombrant |
| Lance-Grenades Multiple | 24" | 2 | 1 | Calibre Moyen | Explosif[2], Tir indirect |
| Mortier d'Infanterie | 60" | 1 | 2 | Gros Calibre | Tir indirect, Explosif[3], Encombrant |
| Lance-Roquettes (RPG) | 36" | 1 | 4 | Antichar | Explosif[1], Encombrant |
| Lance-Flammes | 12" | 1 | 1 | Petit Calibre | Incendiaire, Ignore le couvert |

### Armes de mêlée

| Nom de l'Arme | A | D | Catégorie (Matrice) | Traits Spéciaux |
|---|---|---|---|---|
| Couteau de Combat | 2 | 1 | Légère | - |
| Matraque / Tonfa Tactique | 2 | 1 | Légère | Parade |
| Machette / Hachette | 3 | 1 | Moyenne | Balayage |
| Marteau de Brèche Lourd | 1 | 2 | Lourde | Déséquilibrant, Brutal |
| Lance anti-émeute / Baïonnette | 2 | 1 | Moyenne | Allonge, Parade |
| Outil de Perçage Pneumatique | 1 | 2 | Thermique | Brutal |

## Traits d'armes

### Armes à distance

**Maniable** : Une figurine équipée de cette arme peut tirer même si son unité a effectué un Sprint lors de la phase de mouvement, mais elle subit un malus de -2 au jet de Tir.

**Encombrant** : Une figurine ayant fait un mouvement normal ne peut pas tirer avec cette arme. Elle doit obligatoirement être restée Immobile. Tirer avec une arme encombrante empêche de faire un assaut. Un véhicule peut se déplacer et tirer avec une arme encombrante.

**Chevrotine** : Le profil de Dégâts de cette arme change en fonction de la distance. Si la cible se trouve à la moitié de la portée maximale ou moins, l'arme utilise la ligne Gros Calibre.

**Pistolet** : Permet de tirer tout en étant engagé au corps-à-corps contre l'unité ennemie engagée. Le tireur subit un malus de -2 à son jet de Tir.

**Ignore le couvert** : cette arme ignore les malus de couverture lors du tir.

**Tir indirect** : permet de tirer sans avoir de ligne de vue directe sur la cible. Si c'est le cas, le tireur à un malus de -3 sur le jet de Tir.

**Saturation** : Si une unité ennemie subit au moins une touche réussie par cette arme, sa capacité de mouvement est réduite de 2" pour sa prochaine activation.

**Incendiaire** : si une unité ennemie subit au moins une touche réussie, elle gagne le statut Enflammé. Lors de sa prochaine activation, elle subit automatiquement autant de touches de Petit Calibre (Dégât 1) qu'elle compte de figurines. Le statut Enflammé est ensuite retiré.

**Rafales** : augmente de 2 le nombre d'attaques quand la cible est à une distance inférieure ou égale à la mi portée de l'arme.

**Explosif[X]** : un jet de touche réussi avec une arme ayant ce trait génère X touches supplémentaires à l'unité ciblée.

### Armes de mêlée

**Allonge** : Une figurine équipée de cette arme peut attaquer au corps-à-corps sans être en contact socle à socle avec un ennemi, à condition de se trouver à 1" ou moins de cet ennemi.

**Déséquilibrant** : La figurine subit un malus de -2 à son Initiative lors de la définition de l'ordre de frappe de la Phase de Mêlée.

**Parade** : L'arme est conçue autant pour l'attaque que pour la défense (comme un bâton long ou un sabre de duel). Lors du calcul de la Différence pour le jet de Touche, la caractéristique de Combat (C) du porteur est augmentée de +1.

**Brutal** : Si au moins une figurine est tuée par une arme ayant ce trait lors d'une Phase de Mêlée, l'unité qui subit les pertes subit un malus de -1 à son test de Moral de fin de combat.

**Balayage** : Chaque jet de touche réussi avec cette arme génère 1 touche supplémentaire. Cependant, les attaques avec Balayage subissent un malus de -1 pour blesser.

## Scénario de référence

Cette première version des règles propose un scénario de test et des règles associées, ainsi qu'un socle de liste d'armée.

### Annihilation

**Déploiement** : Les joueurs se déploient dans leur zone respective (10" dans le sens de la longueur de la table).

**Méthode de déploiement** : les joueurs lancent 1D10. Celui ayant le plus haut résultat choisit ou non de poser une unité. Les placements se font ensuite en alterné.

**Durée** : La partie prend fin à l'issue du 6ème tour de jeu.

**Conditions de Victoire (Kill Points)** : À la fin du 6ème tour, chaque joueur calcule son score. Il gagne un nombre de points de victoire (PVic) égal à la valeur en points des unités ennemies remplissant l'une des conditions suivantes :

- L'unité a été entièrement détruite (toutes les figurines sont à 0 PV).
- L'unité a fui la bataille (une figurine est sortie de la table suite à un mouvement de déroute ou de désengagement).
- L'unité est actuellement sous le statut Démoralisé (Clouée au sol ou en fuite) à la fin du dernier tour.

**Résolution** : Le joueur ayant le score le plus élevé remporte la bataille.

### Force Opérationnelle Générique (500 Points)

Cette liste d'armée équilibrée permet de faire un scénario de test. Elle propose

#### Unité 1 : Escouade de Commandement Vétéran (100 Pts)

Une petite unité d'élite, très bien protégée, idéale pour tenir un couvert lourd et éliminer les cibles blindées à distance.

- **Effectif** : 5 Figurines (1 PV chacune)
- **Profil** : M 6" | T 6+ | C 6 | I 4 | Mo 3 | CA 2 (Lourd)
- **Équipement** : 4 Fusils d'Assaut (Calibre Moyen), 1 Fusil de Précision (Calibre Perforant). Armes de mêlée Légères (Couteaux).
- **Socles** : 25mm

#### Unité 2 : Troupe de Fusiliers Alpha (100 Pts)

L'ossature de l'armée. Une unité populeuse conçue pour saturer l'ennemi.

- **Effectif** : 10 Figurines (1 PV chacune)
- **Profil** : M 6" | T 7+ | C 4 | I 5 | Mo 5 | CA 1 (Léger)
- **Équipement** : 9 Fusils d'Assaut (Calibre Moyen), 1 Mitrailleuse Légère (Calibre Moyen, Saturation). Armes de mêlée Légères (Baïonnettes).
- **Socles** : 25mm

#### Unité 3 : Troupe de Fusiliers Beta (100 Pts)

La seconde unité de ligne, équipée pour gérer les menaces lourdes et les véhicules.

- **Effectif** : 10 Figurines (1 PV chacune)
- **Profil** : M 6" | T 7+ | C 4 | I 5 | Mo 5 | CA 1 (Léger)
- **Équipement** : 9 Fusils d'Assaut (Calibre Moyen), 1 Lance-Roquettes (Antichar, Explosif[1]). Armes de mêlée Légères (Baïonnettes).
- **Socles** : 25mm

#### Unité 4 : Équipe d'Assaut Urbain (100 Pts)

Des troupes très mobiles destinées à sprinter d'abri en abri pour aller engager l'ennemi au corps-à-corps.

- **Effectif** : 8 Figurines (1 PV chacune)
- **Profil** : M 6" | T 7+ | C 6 | I 4 | Mo 4 | CA 1 (Léger)
- **Équipement** : 4 Pistolets-Mitrailleurs (Rafales, Maniable), 4 Fusils à Pompe Tactiques (Chevrotine). Armes de mêlée Moyennes (Machettes).
- **Socles** : 25mm

#### Unité 5 : Véhicule d'Appui Tout-Terrain (100 Pts)

Permet de tester la résistance balistique des blindages et la mobilité extrême.

- **Effectif** : 1 Véhicule (4 PV)
- **Profil** : M 12" | T 6+ | C 2 | I 6 | Mo 2 | CA 3 (Véhicule Léger)
- **Équipement** : 1 Mitrailleuse Lourde (Gros Calibre, Saturation). Pas d'arme de mêlée.
- **Socles** : 50x80mm