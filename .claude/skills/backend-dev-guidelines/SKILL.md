---
name: backend-dev-guidelines
description: Guide de développement backend pour ISLAMU Event (.NET 10). Couvre Clean Architecture, CQRS (MediatR), EF Core, API Controllers, et FluentValidation.
progress: false
---

# Backend Development Guidelines (.NET 10)

## 🎯 Objectif
Standardiser le développement backend sur **ISLAMU Event** en respectant la **Clean Architecture** et le pattern **CQRS**.

## ⚡ Quand utiliser ce Skill ?
S'active automatiquement lorsque vous travaillez sur :
*   Création de **Commandes** ou **Requêtes** (CQRS/MediatR).
*   Modification des **Contrôleurs API** (`Explore.Api`).
*   Gestion de la base de données (**Entity Framework Core**, PostGIS).
*   Validation des données (**FluentValidation**).
*   Configuration et Injection de Dépendances.

## 📚 Ressources
| Fichier | Description |
| :--- | :--- |
| `architecture-overview.md` | Structure des couches (Domain, App, Infra, API). |
| `cqrs-and-handlers.md` | Remplacement des "Services" par des Handlers MediatR. |
| `api-and-controllers.md` | Bonnes pratiques pour les contrôleurs "minces". |
| `ef-core-patterns.md` | Accès données et Spécifications. |
| `validation-patterns.md` | Règles FluentValidation. |
| `configuration.md` | Gestion des secrets (Infisical) et Options Pattern. |
| `observability.md` | Logs (Serilog) et Tracing (OpenTelemetry). |
| `testing-guide.md` | Tests unitaires (xUnit) et d'intégration. |

## 🚀 Checklist Nouvelle Feature
1.  **Domain** : Créer l'Entité et les Value Objects dans `Explore.Domain`.
2.  **Contract** : Définir les DTOs (Request/Response).
3.  **Application** : Créer la Command/Query et le Handler.
4.  **Validation** : Ajouter le `AbstractValidator<T>`.
5.  **API** : Ajouter l'endpoint dans le Contrôleur.
6.  **Tests** : Ajouter les tests unitaires xUnit.
