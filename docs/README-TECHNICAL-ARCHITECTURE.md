# ISLAMU Event — ATProto-First Federated Event Platform

<div align="center">

[![Build Status](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&logo=github&style=flat-square)](https://github.com/islamu-ngo/Explore/actions)
[![Code Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore)](https://app.codecov.io/github/islamu-ngo/Explore)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)
[![License: AGPL v3](https://img.shields.io/github/license/islamu-ngo/Explore?color=594ae2&logo=github&style=flat-square)](https://github.com/islamu-ngo/Explore/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1357505436479131668?color=%237289da&label=Discord&logo=discord&logoColor=%237289da&style=flat-square)](https://discord.gg/wrkY824Yv5)

**Production-grade .NET event discovery platform built on Clean Architecture principles with ATProto federation**

[Architecture](#-architecture) •
[Tech Stack](#%EF%B8%8F-technology-stack) •
[Quick Start](#-quick-start) •
[Documentation](#-documentation) •
[Contributing](#-contributing)

</div>

---

## 🏗️ Architecture

**ISLAMU Event** is engineered as a **multi-tenant, federated event management system** following **Clean Architecture** and **SOLID principles**. The system prioritizes **ATProto-first federation** with a planned **ActivityPub gateway** for broader Fediverse interoperability.

### Architectural Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ISLAMU Event Architecture                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────────────────────┐   │
│   │   Users     │────▶│    PDS      │────▶│    ATProto Network          │   │
│   │  (DIDs)     │     │  (Hosting)  │     │  (Relay/Firehose/AppView)   │   │
│   └─────────────┘     └─────────────┘     └─────────────────────────────┘   │
│         │                                              │                    │
│         │                                              │                    │
│         ▼                                              ▼                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    ISLAMU Event AppView                              │   │
│   │  • Indexes ngo.islamu.event.* records                               │   │
│   │  • Provides search/discovery APIs                                    │   │
│   │  • Manages cultural/audience filtering                               │   │
│   │  • Hosts ActivityPub Gateway (planned)                                │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                        │                                    │
│                                        ▼                                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │               ActivityPub Gateway (Bridge, planned)                  │   │
│   │  • Exposes ATProto events as ActivityPub Event objects              │   │
│   │  • Translates ActivityPub Follow → ATProto follow records           │   │
│   │  • Translates ActivityPub RSVP → ATProto participation records      │   │
│   │  • Would provide WebFinger, Actor endpoints, Inbox/Outbox (planned) │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                        │                                    │
│                                        ▼                                    │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      Fediverse                                       │   │
│   │              (Mastodon, Mobilizon, Pleroma, etc.)                   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Clean Architecture Layers

The codebase follows **strict dependency rules** where dependencies flow inward:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Layer Dependency Flow                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                         Presentation Layer                           │   │
│   │                  (Explore.API, Explore.Blazor)                       │   │
│   │  • ASP.NET Core Web API with Scalar/Swagger                         │   │
│   │  • Blazor Server + WASM (BFF pattern)                               │   │
│   │  • Controllers (thin, delegate to MediatR)                          │   │
│   │  • Dependency Injection registration                                 │   │
│   └──────────────────────────────┬──────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      Infrastructure Layer                            │   │
│   │         (Explore.Persistence, Explore.Infrastructure)                │   │
│   │  • EF Core 10 (PostgreSQL + PostGIS)                                │   │
│   │  • Repository pattern implementation                                 │   │
│   │  • External service integrations (Auth, Storage, Email)             │   │
│   │  • DbContext + Entity configurations                                 │   │
│   └──────────────────────────────┬──────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                       Application Layer                              │   │
│   │                    (Explore.Application)                             │   │
│   │  • CQRS with MediatR (Commands/Queries/Handlers)                    │   │
│   │  • DTOs + FluentValidation validators                               │   │
│   │  • AutoMapper profiles                                               │   │
│   │  • Repository interfaces                                             │   │
│   │  • Application business logic                                        │   │
│   └──────────────────────────────┬──────────────────────────────────────┘   │
│                                  │                                          │
│                                  ▼                                          │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                          Domain Layer                                │   │
│   │                      (Explore.Domain)                                │   │
│   │  • Entities (Event, Organization, User, EventSession)               │   │
│   │  • Value Objects                                                     │   │
│   │  • Domain Events                                                     │   │
│   │  • Enums (lookup tables)                                             │   │
│   │  • NO external dependencies                                          │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Key Architectural Principles:**

- **Dependency Inversion:** All layers depend on abstractions, not implementations
- **Single Responsibility:** Each handler handles one command/query; each repository handles one entity
- **Open/Closed:** Extend behavior via interfaces without modifying existing code
- **Interface Segregation:** Small, focused interfaces (e.g., `IEventRepository`, not `IRepository`)
- **Liskov Substitution:** All repository implementations fully implement their contracts

### CQRS Pattern with MediatR

**Command Query Responsibility Segregation** separates read and write operations:

```
HTTP Request → Controller → MediatR → Handler → Repository → Entity → AutoMapper → DTO → Response
```

**Write Operations (Commands):**
```csharp
CreateEventCommand → CreateEventCommandHandler → IEventRepository.Create()
                                                 → BaseCommandResponse<Guid>
```

**Read Operations (Queries):**
```csharp
GetEventListRequest → GetEventListRequestHandler → IEventRepository.GetEventsWithDetails()
                                                  → List<EventListDto>
```

**Benefits:**
- **Separation of Concerns:** Read and write models evolve independently
- **Scalability:** Query and command operations can be scaled separately
- **Testability:** Each handler is a single-purpose unit
- **Maintainability:** Clear request/response contracts

### Repository Pattern

**Generic Repository + Specific Repositories:**

```csharp
// Generic base (common CRUD)
public interface IGenericRepository<T, TId> where T : class
{
    Task<T?> GetById(TId id);
    Task<List<T>> GetAll();
    Task<bool> Exists(TId id);
    Task<T> Create(T entity);
    Task Update(T entity);
    Task Delete(T entity);
}

// Entity-specific (complex queries)
public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<List<Event>> GetEventsWithDetails();
    Task<Event?> GetEventWithDetails(Guid id);
    Task<List<Event>> GetEventsByOrganization(Guid organizationId);
}
```

**Critical Rules:**
- ✅ Repositories return **entities**, never DTOs
- ✅ Handlers map entities to DTOs via AutoMapper
- ✅ Navigation properties on link tables are **readonly** (write through repository)
- ✅ Validators use **manual instantiation**, not DI

See [GOVERNANCE.md](GOVERNANCE.md) for complete coding conventions.

---

## 🛠️ Technology Stack

### Backend (.NET 10)

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Framework** | ASP.NET Core 10 | Web API + Blazor Server |
| **CQRS** | MediatR | Command/Query separation |
| **Validation** | FluentValidation | DTO validation |
| **Mapping** | AutoMapper | Entity ↔ DTO transformation |
| **ORM** | Entity Framework Core 10 | Data access |
| **Database** | PostgreSQL 17 + PostGIS | Relational + geospatial queries |
| **API Docs** | Scalar + Swagger | OpenAPI 3.0 documentation |
| **Orchestration** | .NET Aspire | Multi-project orchestration |

### Frontend (Blazor)

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **UI Framework** | Blazor Server + WASM | Hybrid rendering |
| **Component Library** | MudBlazor | Material Design components |
| **Architecture** | BFF Pattern | Backend-for-Frontend security |
| **State Management** | Fluxor (planned) | Redux-like state management |

### Infrastructure

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Authentication** | Keycloak (OIDC) | Identity provider |
| **Authorization** | Cerbos (PDP) | Policy Decision Point |
| **Secrets** | Infisical | Secrets management |
| **Webhooks** | Svix | Webhook delivery |
| **Storage** | MinIO/S3 | Object storage |
| **Email** | SendGrid/SMTP | Transactional emails |
| **Error Tracking** | Sentry | Error monitoring |

### DevOps

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **CI/CD** | GitHub Actions | Build/test/deploy pipeline |
| **Containerization** | Docker + Docker Compose | Container orchestration |
| **Deployment** | Coolify | PaaS for self-hosting |
| **Monitoring** | Kener | Status page |
| **Code Quality** | SonarCloud | Static analysis |

---

## 🚀 Quick Start

### Prerequisites

- **.NET 10 SDK** (`dotnet --version` ≥ 10.0)
- **PostgreSQL 17** with PostGIS extension
- **Docker** (optional, for infrastructure services)

### Local Development

```bash
# 1. Clone the repository
git clone https://github.com/islamu-ngo/Explore.git
cd Explore

# 2. Restore dependencies
dotnet restore

# 3. Set up database (modify connection string in appsettings.Development.json)
dotnet ef database update --project Explore.Persistence

# 4. Run with Aspire orchestrator (recommended)
dotnet run --project Explore.AppHost/Explore.AppHost.csproj

# Or run individual projects:
# API: dotnet run --project Explore.API
# Blazor: dotnet run --project Explore.Blazor
```

**Default URLs:**
- **Aspire Dashboard:** `https://localhost:17225`
- **API:** `https://localhost:7001`
- **Blazor:** `https://localhost:7002`
- **Scalar API Docs:** `https://localhost:7001/scalar/v1`

### Docker Compose (Full Stack)

```bash
# Start all services (PostgreSQL, Keycloak, MinIO, etc.)
docker-compose up -d

# Run migrations
dotnet ef database update --project Explore.Persistence

# Start API and Blazor
dotnet run --project Explore.AppHost
```

---

## 📊 Database Schema

The system uses **PostgreSQL 17** with **PostGIS** for geospatial queries. The schema follows **Clean Architecture** principles with clear entity relationships.

**Key Entities:**
- **Event:** Core entity (Guid PK, tenant isolation, soft delete)
- **Organization:** Event organizers (actor-based identity)
- **User:** Platform users (multi-tenant, external auth)
- **EventSession:** Individual event occurrences (temporal + location)
- **Actor:** ATProto decentralized identity (DID-based)
- **ATProtoRecord:** Federated event records (indexing)

**Entity Conventions:**
- **Primary Keys:** `Guid` (main entities), `int` (lookup tables)
- **Auditing:** `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- **Soft Delete:** `IsDeleted` (EF Core 10 named query filters)
- **Tenant Isolation:** `TenantId` (multi-tenant security)

See [schema/islamu-event.md](schema/islamu-event.md) for full database schema.

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[CLAUDE.md](CLAUDE.md)** | AI agent entrypoint, project instructions |
| **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** | System architecture deep-dive |
| **[API.md](docs/API.md)** | REST API conventions + endpoints |
| **[BLAZOR.md](docs/BLAZOR.md)** | Frontend architecture (Server + WASM) |
| **[DOMAIN.md](docs/DOMAIN.md)** | Domain model + business logic |
| **[SECURITY.md](docs/SECURITY.md)** | Authentication/authorization architecture |
| **[GOVERNANCE.md](docs/GOVERNANCE.md)** | Coding conventions + standards |
| **[QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md)** | 12 critical rules (never violate) |
| **[TEMPLATE_GLOSSARY.md](docs/TEMPLATE_GLOSSARY.md)** | Placeholder substitution guide |
| **[OPERATIONS.md](docs/OPERATIONS.md)** | Deployment + environment variables |
| **[TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)** | Common issues + solutions |
| **[CONTRIBUTING.md](docs/CONTRIBUTING.md)** | Contribution workflow |

**Skills (Custom AI Agents):**
- `clean-architecture-rules` — Enforces layer dependencies
- `cqrs-mediatr-guidelines` — CQRS patterns with MediatR
- `dotnet-efcore-guidelines` — EF Core best practices
- `error-tracking` — Sentry error handling
- `blazor-ui-conventions` — MudBlazor component patterns

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Explore.Application.UnitTests
```

**Testing Strategy:**
- **Unit Tests:** Application layer (handlers, validators)
- **Integration Tests:** API endpoints (WebApplicationFactory)
- **Repository Tests:** In-memory database (EF Core)
- **E2E Tests:** Playwright (planned)

**Code Coverage:** Target ≥ 80% for critical paths.

---

## 🏛️ Design Patterns

| Pattern | Implementation | Purpose |
|---------|----------------|---------|
| **CQRS** | MediatR | Separate reads from writes |
| **Repository** | `IEventRepository`, `IOrganizationRepository` | Data access abstraction |
| **Unit of Work** | `DbContext` | Transaction management |
| **Mediator** | MediatR | Decouple controllers from handlers |
| **Factory** | AutoMapper | Entity/DTO creation |
| **Strategy** | Validators | Pluggable validation rules |
| **Specification** | EF Core query filters | Composable query logic |

---

## 🔐 Security Architecture

**Authentication:**
- **OIDC/OAuth 2.0** via Keycloak
- **JWT tokens** (access + refresh)
- **DID-based identity** (ATProto actors)

**Authorization:**
- **Policy-based** (Cerbos PDP)
- **Attribute-based access control** (ABAC)
- **Role-based access control** (RBAC)

**Controller Authorization:**
- `[AllowAnonymous]` — Public read access (GET endpoints)
- `[Authorize]` — Authenticated write access (POST/PUT/DELETE)
- `[Authorize(Roles = "Admin")]` — Admin-only operations

**Security Best Practices:**
- ✅ Secrets in Infisical (never hardcoded)
- ✅ HTTPS enforced (ASP.NET Core HSTS)
- ✅ CORS configured (origin whitelist)
- ✅ Rate limiting (ASP.NET Core middleware)
- ✅ SQL injection prevention (parameterized queries)
- ✅ XSS prevention (Razor encoding)

See [SECURITY.md](docs/SECURITY.md) for details.

---

## 🌍 Federation (ATProto)

**Current State:** AppView indexing layer (reads ATProto records)
**Planned:** Full ATProto federation with ActivityPub gateway

**ATProto Integration:**
- **DIDs:** Decentralized identifiers for actors
- **Records:** `ngo.islamu.event.*` lexicon
- **Indexing:** Firehose subscription + cursor tracking
- **Storage:** Actor key store (encrypted private keys)

**ActivityPub Gateway (Planned):**
- Expose ATProto events as ActivityPub `Event` objects
- Translate `Follow` → ATProto follow records
- Translate `RSVP` → ATProto participation records

See [FEDERATION.md](docs/FEDERATION.md) for details.

---

## 🤝 Contributing

We follow **Clean Architecture** and **SOLID principles**. Before contributing:

1. **Read the documentation:**
   - [GOVERNANCE.md](docs/GOVERNANCE.md) — Coding standards
   - [QUICK_REFERENCE.md](docs/QUICK_REFERENCE.md) — 12 critical rules
   - [CONTRIBUTING.md](docs/CONTRIBUTING.md) — Workflow guide

2. **Follow the patterns:**
   - Commands return `BaseCommandResponse<Guid>`
   - Queries return `List<EntityDto>` or `EntityDto`
   - Repositories return entities (never DTOs)
   - Validators use manual instantiation

3. **Test your changes:**
   - Write unit tests for handlers
   - Write integration tests for controllers
   - Ensure code coverage ≥ 80%

4. **Submit a pull request:**
   - Fork the repository
   - Create a feature branch (`git checkout -b feature/my-feature`)
   - Commit your changes (`git commit -am 'Add feature'`)
   - Push to the branch (`git push origin feature/my-feature`)
   - Open a pull request

See [Contribution Guidelines](https://sites.plane.so/pages/b957e6c5278845feac5557d22bd54756) for details.

---

## 📈 Roadmap

View the [Roadmap Kanban](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988) for all work items. Vote, comment, and contribute!

**Major Milestones:**
- ✅ Clean Architecture foundation (Domain, Application, Infrastructure, Presentation)
- ✅ CQRS with MediatR
- ✅ EF Core 10 with PostgreSQL + PostGIS
- ✅ Blazor Server + WASM (BFF pattern)
- ✅ Keycloak OIDC authentication
- 🚧 ATProto indexing layer
- 🚧 Cerbos authorization policies
- ⏳ ActivityPub gateway (planned)
- ⏳ Real-time updates (SignalR)
- ⏳ Advanced search (ElasticSearch)

---

## 📊 Metrics

![Build Status](https://img.shields.io/github/actions/workflow/status/islamu-ngo/Explore/build.yml?branch=main&logo=github&style=flat-square)
[![Code Coverage](https://img.shields.io/codecov/c/github/islamu-ngo/Explore)](https://app.codecov.io/github/islamu-ngo/Explore)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=alert_status)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=islamu-ngo_Explore&metric=sqale_index)](https://sonarcloud.io/summary/overall?id=islamu-ngo_Explore)

![Repository Stats](https://repobeats.axiom.co/api/embed/a0f11a3d9b80342b5f5965127c2c45871c9d3397.svg "Repobeats analytics")

---

## 🙏 Acknowledgements

This project builds on incredible open-source tools:

- **[Keycloak](https://www.keycloak.org/)** — Identity and Access Management
- **[Cerbos](https://www.cerbos.dev/)** — Policy Decision Point
- **[Svix](https://www.svix.com/)** — Webhooks service
- **[Infisical](https://infisical.com/)** — Secrets management
- **[MudBlazor](https://www.mudblazor.com/)** — Blazor UI library
- **[Penpot](https://penpot.app/)** — Design tool
- **[Plane](https://plane.so/)** — Project management
- **[Coolify](https://coolify.io/)** — Self-hosting platform
- **[Kener](https://kener.ing/)** — Status page

---

## 📞 Contact

- **GitHub Issues:** [Report bugs or request features](https://github.com/islamu-ngo/Explore/issues)
- **Discord:** [Join our community](https://discord.gg/wrkY824Yv5)
- **Email:** contact@openislamu.org

---

## 📄 License

This project is licensed under the **GNU Affero General Public License v3.0 (AGPL-3.0)**.

**Key Terms:**
- ✅ **Use:** Free to use for any purpose
- ✅ **Modify:** Free to modify and create derivatives
- ✅ **Distribute:** Free to distribute original or modified versions
- ⚠️ **Network Use:** If you run a modified version as a service, you **must** release your source code
- ⚠️ **Copyleft:** Derivative works must use AGPL-3.0

See [LICENSE](LICENSE) for full details.

---

## 🇵🇸 Support Palestine

The tyranny of Israel on the Palestinian people is horrifying and heartbreaking. Consider supporting Palestinians by donating to the [Palestinian Red Crescent Society](https://www.palestinercs.org/en/Donation).

[![Support Palestine](https://github.com/Safouene1/support-palestine-banner/blob/master/banner-support.svg)](https://www.palestinercs.org/en/Donation)

---

<div align="center">

**Built with ❤️ by the ISLAMU community**

⭐️ **Star this repository** if you find it useful!

[Documentation](docs/) • [Roadmap](https://sites.plane.so/views/b8b7d9fced694f5a9d9a546e9d40d988) • [Discord](https://discord.gg/wrkY824Yv5) • [Contribute](docs/CONTRIBUTING.md)

</div>
