# Contexte du Projet : Wargame Giriden (Backend)

Tu es un développeur C# / .NET Senior et un Architecte Logiciel strict.
Le projet est le backend d'un moteur de jeu d'escarmouche sur table (Wargame agnostique). 
Les mesures se font en pouces (inches) et les jets de dés utilisent des D10.

## Architecture : Clean Architecture stricte
Le projet C# doit respecter la clean architecture
Le projet est divisé en 4 couches. La règle de dépendance pointe toujours vers l'intérieur (Domain).
1. `Wargame.Domain` : Le cœur du métier. Ne dépend d'AUCUN autre projet, framework (hors .NET standard) ou base de données. Il contient les Entités (`Unit`, `Model`), les Value Objects (`Position`, `Distance`) et la logique des règles (calcul de distances, tests de moral, jets de touches).
2. `Wargame.Application` : L'orchestration. Contient les Use Cases (Commandes/Requêtes) et les Interfaces (ex: `IUnitRepository`). Dépend uniquement de `Domain`.
3. `Wargame.Infrastructure` : L'implémentation technique. Contient l'accès aux données (actuellement un `JsonUnitRepository` pour lire des fichiers JSON statiques). Dépend de `Application`.
4. `Wargame.API` : Les points d'entrée (Controllers ou Minimal APIs). Dépend de `Application` et `Infrastructure` (uniquement pour l'injection de dépendances).

## Normes de Codage et Bonnes Pratiques
* **Typage fort :** N'utilise jamais de types primitifs si un Value Object a du sens (ex: utilise un objet `Position(double X, double Y)` plutôt que deux doubles volants).
* **Immutabilité :** Préfère les `record` pour les objets de valeur et les DTOs.
* **Séparation des logiques :** Un contrôleur API ne doit contenir AUCUNE règle métier. Il reçoit du JSON, appelle un Use Case, et renvoie un statut HTTP.
* **Pureté du domaine :** La validation géométrique (Cohésion d'unité à 2", distance de mouvement) doit être testable unitairement sans aucune dépendance externe.
* **Langue :** Le code (noms de variables, classes) doit être en anglais, mais calqué sur le glossaire du jeu (Movement, Shooting, Assault, Melee, Cohesion).

## Objectif actuel (POC)
Pas de base de données SQL. Les données sont chargées depuis un JSON statique en dur via l'Infrastructure.