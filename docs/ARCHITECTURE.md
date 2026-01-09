# Technical Architecture

## Technology Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Runtime** | .NET 10.0 | Primary framework |
| **Orchestration** | .NET Aspire | Service orchestration, observability |
| **Web UI** | Blazor Server + WebAssembly | Hybrid rendering model |
| **UI Components** | MudBlazor | Material Design components |
| **Database** | PostgreSQL + PostGIS | Primary datastore with spatial queries |
| **ORM** | Entity Framework Core | Data access layer |
| **Authentication** | Keycloak | OIDC/JWT identity provider |
| **Authorization** | Cerbos | Policy Decision Point (PDP) |
| **Secrets** | Infisical | Secrets management |
| **Logging** | Serilog | Structured logging |
| **Telemetry** | OpenTelemetry | Distributed tracing and metrics |
| **API Docs** | Scalar + Swagger | OpenAPI documentation |
| **Federation** | ActivityPub | Decentralized social networking protocol |

## Architectural Pattern: Clean Architecture with CQRS

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐ │
│  │   Explore.API   │  │ Explore.Blazor  │  │ Explore.Blazor.     │ │
│  │   (REST API)    │  │ (Server BFF)    │  │ Client (WASM)       │ │
│  └────────┬────────┘  └────────┬────────┘  └──────────┬──────────┘ │
└───────────┼─────────────────────┼─────────────────────┼────────────┘
            │                     │                     │
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       Application Layer                             │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    Explore.Application                       │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │   │
│  │  │   Commands   │  │   Queries    │  │   Validators     │   │   │
│  │  │  (MediatR)   │  │  (MediatR)   │  │ (FluentValidation)│   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────────┘   │   │
│  │  ┌──────────────┐  ┌──────────────┐                         │   │
│  │  │   Handlers   │  │    DTOs      │                         │   │
│  │  └──────────────┘  └──────────────┘                         │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
            │                                           │
            ▼                                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Domain Layer                                 │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                      Explore.Domain                          │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │   │
│  │  │   Entities   │  │    Enums     │  │  Value Objects   │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
            │                                           │
            ▼                                           ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                            │
│  ┌──────────────────────────┐  ┌────────────────────────────────┐  │
│  │   Explore.Persistence    │  │    Explore.Infrastructure      │  │
│  │  ┌────────────────────┐  │  │  ┌────────────────────────┐    │  │
│  │  │   DbContext        │  │  │  │   EmailService         │    │  │
│  │  │   Repositories     │  │  │  │   ActivityPubService   │    │  │
│  │  │   Migrations       │  │  │  │   FileStorageService   │    │  │
│  │  └────────────────────┘  │  │  └────────────────────────┘    │  │
│  └──────────────────────────┘  └────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
```

### Data Flow Pattern

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Request Flow (CQRS Pattern)                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Controller → MediatR → Handler → Repository → Entity → AutoMapper → DTO    │
│  ────────────────▶─────────────▶──────────────▶───────▶──────────  │
│                                                                             │
│  QUERY FLOW (Read):                                                       │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐            │
│  │   GET /api   │───▶│   Query     │───▶│  Repository  │───▶ Entities │
│  │              │    │              │    │              │     │          │
│  │              │    │              │    │              │     ▼          │
│  │              │◀───│  Response     │◀───│ DTOs (via    │            │
│  │              │    │              │    │     AutoMapper) │            │
│  └──────────────┘    └──────────────┘    └──────────────┘            │
│                                                                             │
│  COMMAND FLOW (Write):                                                     │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐            │
│  │  POST /api   │───▶│   Command   │───▶│  Handler     │            │
│  │              │    │              │    │              │    │              │
│  │              │    │              │    │              │    ▼              │
│  │              │    │              │    │              │  ┌──────────────┐│
│  │              │    │              │    │              │──▶│ Validator    ││
│  │              │    │              │    │              │  └──────────────┘│
│  │              │    │              │    │              │    │              │
│  │              │    │              │    │              │    ▼              │
│  │              │    │              │    │              │  ┌──────────────┐│
│  │              │    │              │    │              │──▶│ Repository   ││
│  │              │◀───│  BaseCommand│    │              │  │   (Entity)  ││
│  │              │    │  Response<Guid>│    │              │  └──────────────┘│
│  └──────────────┘    └──────────────┘    └──────────────┘            │
│                                                                             │
│  KEY PATTERN: Repositories return ENTITIES, not DTOs. DTOs are created in    │
│  handlers via AutoMapper.                                                     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Project Structure

### Solution Layout

```
Explore.sln
├── src/
│   ├── Explore.Domain/              # Domain layer (innermost) Domain entities, enums, value objects (no dependencies)
│   ├── Explore.Application/         # Application layer DTOs, MediatR handlers, FluentValidation
│   │   ├── Features/
│   │   │   └── {Entity}/
│   │   │       ├── Requests/
│   │   │       │   ├── Commands/    # CreateEventCommand.cs, UpdateEventCommand.cs
│   │   │       │   └── Queries/     # GetEventListRequest.cs
│   │   │       └── Handlers/
│   │   │           ├── Commands/    # CreateEventCommandHandler.cs
│   │   │           └── Queries/     
│   │   └── DTOs/
│   │       └── {Entity}/
│   │           ├── {Entity}Dto.cs
│   │           └── Validators/      # {Entity}DtoValidator.cs
│   ├── Explore.Persistence/         # Data access layer EF Core DbContext, repositories, migrations
│   ├── Explore.Infrastructure/      # External services: email, ActivityPub, file storage
│   ├── Explore.API/                 # REST API with JWT/Keycloak auth, Cerbos authz
│   ├── Explore.Blazor/              # Server-side Blazor (BFF pattern with OIDC)
│   ├── Explore.Blazor.Client/       # WebAssembly client components
│   ├── Explore.AppHost/             # Aspire orchestrator
│   └── Explore.ServiceDefaults/     # Shared Aspire config defaults (telemetry, health checks)
├── tests/
│   ├── Explore.Domain.Tests/
│   ├── Explore.Application.Tests/
│   ├── Explore.API.Tests/
│   └── Explore.Integration.Tests/
├── docs/
│   ├── api/                         # API documentation
│   ├── architecture/                # Architecture decisions
│   └── federation/                  # ActivityPub specs
└── scripts/
    ├── migrations/                  # Database scripts
    └── deployment/                  # CI/CD scripts
```

### Layer Dependencies

```
                    ┌─────────────────────┐
                    │   Explore.Domain    │  ◄── No external dependencies
                    └─────────────────────┘
                              ▲
                              │
                    ┌─────────────────────┐
                    │ Explore.Application │  ◄── References Domain only
                    └─────────────────────┘
                              ▲
                    ┌─────────┴─────────┐
                    │                   │
          ┌─────────────────┐  ┌─────────────────────┐
          │   Persistence   │  │   Infrastructure    │  ◄── Reference Application
          └─────────────────┘  └─────────────────────┘
                    ▲                   ▲
                    └─────────┬─────────┘
                              │
                    ┌─────────────────────┐
                    │    Explore.API      │  ◄── References all
                    └─────────────────────┘
```
