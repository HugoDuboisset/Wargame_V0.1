# Contexte du Projet : Wargame Giriden (Frontend)

Tu es un développeur Frontend Senior expert en React, TypeScript et rendu Canvas (Konva.js).
Le projet est une interface de wargame tactique 2D vue de dessus (Top-Down).

## Concepts Clés du Moteur Spacial (IMPORTANT)
* **Séparation Logique / Rendu :** Le "State" (données du jeu venant de l'API) est STRICTEMENT stocké en pouces (Inches), la vraie mesure du wargame.
* **Le PPI (Pixels Per Inch) :** Le rendu Canvas (`react-konva`) convertit la logique en pixels uniquement au moment du dessin en multipliant la valeur en pouces par une constante PPI (ex: 50px = 1 pouce).
* **Zoom et Pan :** La taille de la table (ex: 50"x40") est fixe. L'adaptation à l'écran de l'utilisateur se fait via le `scale` (zoom) et le `x/y` (pan) de la caméra (le Layer principal de Konva), et non en utilisant des pourcentages de taille d'écran.

## Architecture Frontend
1. **Couche API :** Services (fetch/axios) isolés pour communiquer avec le backend .NET.
2. **Couche State :** Gestion de l'état local (Hooks personnalisés ou Context) pour stocker les unités et la phase de jeu en cours.
3. **Couche UI (DOM) :** Composants React standards (HTML/CSS) pour les menus, fiches d'unités, et boutons d'actions (placés par-dessus le canvas en absolute).
4. **Couche Rendu (Canvas) :** Composants `react-konva` qui se contentent de lire le State et de dessiner des formes (socles circulaires, auras de mouvement). Ils ne mutent jamais le state directement.

## Normes de Codage et Bonnes Pratiques
* **TypeScript Strict :** Pas de `any`. Définis des Interfaces précises pour calquer les DTOs envoyés par le backend C# (UnitProfile, Model, Position).
* **ESLint :** Respecte les règles d'ESLint (notamment les dépendances des `useEffect`).
* **Composants Fonctionnels :** Utilise exclusivement des Functional Components et des Hooks.
* **Dumb Components :** Les composants Konva (ex: `<ModelBase />`) doivent être "stupides" : ils reçoivent des props (x, y, radius, color) et s'affichent. La logique complexe (calcul de collision, validation géométrique) reste dans le backend.

## Objectif actuel (POC)
Afficher une table vide, y placer une unité chargée depuis l'API, et permettre le drag & drop de figurines avec renvoi des nouvelles coordonnées (converties de px en pouces) vers l'API.