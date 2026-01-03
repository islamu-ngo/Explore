# Architecture Overview (Clean Architecture)

L'architecture respecte strictement la **Dependency Rule** : les dépendances pointent vers l'intérieur.

## 🏗️ Les 4 Couches

### 1. Domain (`Explore.Domain`)
*   **Contenu :** Entités, Value Objects, Enums, Exceptions du domaine, Interfaces du Repository (`IEventRepository`).
*   **Dépendances :** Aucune. C'est le cœur pur.
*   **Règle :** Pas de EF Core, pas de HTTP ici.

### 2. Application (`Explore.Application`)
*   **Contenu :** Logique métier orchestrée via **CQRS**.
    *   `Commands/` : Opérations d'écriture (Create, Update).
    *   `Queries/` : Opérations de lecture (Get, List).
    *   `Validators/` : Règles FluentValidation.
*   **Dépendances :** Domain.

### 3. Infrastructure (`Explore.Infrastructure`)
*   **Contenu :** Implémentation technique.
    *   `Persistence/` : DbContext, EF Core Config, Implémentation des Repositories.
    *   `Services/` : EmailService, StorageService.
    *   `Identity/` : Intégration Keycloak.
*   **Dépendances :** Application, Domain.

### 4. Presentation (`Explore.Api` & `Explore.Blazor`)
*   **Contenu :** Points d'entrée.
    *   **API** : Contrôleurs REST minces qui appellent MediatR.
    *   **Blazor** : UI Components.
*   **Dépendances :** Application.

## 🔄 Flux de Requête (CQRS)
1.  **Request** -> API Controller
2.  **Controller** -> Crée un objet `Command`
3.  **MediatR** -> Trouve le `Handler` correspondant
4.  **Handler** -> Utilise le `Repository` (Domain Interface)
5.  **Repository** -> (Infra) Accède à la DB via EF Core
6.  **Response** -> Remonte la chaîne.
