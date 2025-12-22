# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ISLAMU Explore is an Events Platform web application built with .NET 10.0, .NET Aspire, and Blazor. It allows Muslims to discover local and digital Events while helping organizers Manage Events and increase visibility.

## Build Commands

```bash
# Build the solution
dotnet build

# Run with Aspire orchestrator (recommended for development)
dotnet run --project Explore.AppHost/Explore.AppHost.csproj

# Run individual projects
dotnet run --project Explore.API/Explore.API.csproj
dotnet run --project Explore.Blazor/Explore.Blazor.csproj

# Database migrations (run from solution root)
dotnet ef migrations add <MigrationName> --project Explore.Persistence --startup-project Explore.API
dotnet ef database update --project Explore.Persistence --startup-project Explore.API
```

## Architecture

**Clean Architecture with CQRS pattern:**

```
Explore.Domain          → Domain entities, enums (no dependencies)
Explore.Application     → Use cases, DTOs, MediatR handlers, FluentValidation
Explore.Persistence     → EF Core, PostgreSQL, repositories
Explore.Infrastructure  → Cross-cutting concerns
Explore.API             → REST API with JWT/Keycloak auth
Explore.Blazor          → Server-side Blazor (BFF with OIDC)
Explore.Blazor.Client   → WebAssembly client components
Explore.AppHost         → .NET Aspire orchestrator
Explore.ServiceDefaults → Aspire service defaults
```

**Key Patterns:**
- MediatR for CQRS: Commands/Queries in `Explore.Application/Features/{Entity}/Requests/`, Handlers in `Handlers/`
- Generic Repository pattern: `IGenericRepository<T, TKey>` with entity-specific repositories
- AutoMapper for DTO mapping
- FluentValidation for input validation in `DTOs/{Entity}/Validators/`

**Feature Structure Example:**
```
Features/Events/
├── Requests/Commands/CreateEventCommand.cs
├── Requests/Queries/GetEventListRequest.cs
└── Handlers/Commands/CreateEventCommandHandler.cs
```

## Key Technologies

- **Database:** PostgreSQL with EF Core, snake_case naming convention
- **Auth:** Keycloak (JWT Bearer for API, OIDC for Blazor)
- **UI:** MudBlazor component library
- **Secrets:** Infisical SDK for production, User Secrets for development
- **API Docs:** Scalar OpenAPI at `/scalar/v1`
- **Logging:** Serilog with console, file, and Seq sinks
- **Observability:** OpenTelemetry

## Configuration

Keycloak settings are required in `appsettings.json` or environment variables:
```json
{
  "Keycloak": {
    "Authority": "https://keycloak.openislamu.org/realms/{realm}",
    "Realm": "islamu-dev",
    "ClientId": "explore-api",
    "ClientSecret": "..."
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  }
}
```

## Domain Entities

Core entities in `Explore.Domain/`: `Event`, `Organization`, `User`, `ProgramRegistration`

Enums in `Explore.Domain/Enums/`: `EventTypeEnum`, `AudienceGenderEnum`, `AudienceAgeEnum`, `OrganizationRoleEnum`
