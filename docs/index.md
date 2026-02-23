# ISLAMU Explore - Documentation Index

> **Master Reference for AI-Assisted Development**
>
> This index provides comprehensive navigation to all project documentation, optimized for both human developers and AI assistants.
>
> **Last Generated**: February 2026 | **Scan Level**: Exhaustive

---

## Quick Navigation

| Category | Key Documents |
|----------|---------------|
| **Getting Started** | [README.md](../README.md) • [CONTRIBUTING.md](CONTRIBUTING.md) |
| **Architecture** | [ARCHITECTURE.md](ARCHITECTURE.md) • [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| **Development** | [GOVERNANCE.md](GOVERNANCE.md) • [CLAUDE.md](../CLAUDE.md) |
| **Domain** | [DOMAIN.md](DOMAIN.md) • [Schema](../schemas/islamu-event.md) |
| **Operations** | [OPERATIONS.md](OPERATIONS.md) • [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |

---

## Project Overview

### Classification

| Aspect | Details |
|--------|---------|
| **Repository Type** | Monolith |
| **Architecture** | Clean Architecture + CQRS + BFF |
| **Primary Language** | C# |
| **Framework** | .NET 10 (API + Blazor) |
| **Frontend** | Blazor Web App (InteractiveAuto, Server + WASM) |
| **Database** | PostgreSQL 18 with EF Core 10 |
| **Authentication** | Keycloak OIDC/OAuth2 |

### Project Structure

```
Explore/
├── 📁 Core (Domain & Application)
│   ├── Explore.Domain/           # 65+ domain entities, interfaces
│   └── Explore.Application/      # CQRS handlers, DTOs, validators
│
├── 📁 Infrastructure
│   ├── Explore.Persistence/      # EF Core, repositories, migrations
│   └── Explore.Infrastructure/   # Email, storage, external services
│
├── 📁 Presentation
│   ├── Explore.API/              # REST API, 39 controllers, HATEOAS
│   ├── Explore.Blazor/           # Blazor Server BFF with YARP
│   └── Explore.Blazor.Client/    # Blazor WASM components
│
├── 📁 Testing
│   ├── Event.Application.UnitTests/
│   ├── Event.Domain.UnitTests/
│   ├── Event.Architecture.Tests/
│   ├── Event.API.IntegrationTests/
│   ├── Event.Persistence.IntegrationTests/
│   └── Explore.Blazor.Client.Tests/
│
├── 📁 Tooling
│   ├── Explore.AppHost/          # .NET Aspire orchestration
│   ├── Explore.ServiceDefaults/  # Shared service configuration
│   ├── Explore.Diagnostic/       # Diagnostic utilities
│   └── Event.MigrationService/   # Database migration worker
│
├── 📁 Documentation
│   ├── docs/                     # Architecture, API, operations
│   ├── schema/                   # Database schema reference
│   ├── dev/active/               # Active development context
│   └── .repomix/                 # AI-optimized codebase snapshots
│
└── 📁 AI Development Support
    ├── CLAUDE.md                 # AI agent instructions
    ├── .claude/agents/           # 12 specialized AI agents
    ├── .claude/skills/           # 8 domain skills with resources
    ├── .claude/commands/         # Slash commands
    └── _bmad/                    # BMAD methodology framework
```

---

## Core Documentation

### Architecture & Design

| Document | Purpose |
|----------|---------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Clean Architecture layers, CQRS patterns, BFF design |
| [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | 12 critical rules, code patterns, common fixes |
| [GOVERNANCE.md](GOVERNANCE.md) | Naming conventions, file organization, design principles |
| [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md) | Placeholder syntax for project-agnostic docs |

### Platform Architecture

| Document | Purpose |
|----------|---------|
| [MULTI_TENANCY.md](MULTI_TENANCY.md) | Two-tier admin model, cascading settings, data isolation |
| [EXTENSIBILITY.md](EXTENSIBILITY.md) | Aspect-based modules, metadata-driven architecture |
| [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) | Authority model, permission boundaries, delegation |
| [DEPLOYMENT_MODES.md](DEPLOYMENT_MODES.md) | Single vs multi-tenant, runtime mode switching |
| [RENDER_POLICIES.md](RENDER_POLICIES.md) | Policy-based Blazor render modes |

### Domain & Data

| Document | Purpose |
|----------|---------|
| [DOMAIN.md](DOMAIN.md) | Entity relationships, business rules, domain model |
| [MODULAR_EVENTS.md](MODULAR_EVENTS.md) | Aspect-based event customization (Islamic, Tech, etc.) |
| [schema/islamu-event.md](../schema/islamu-event.md) | Database schema, tables, constraints |

### API & Frontend

| Document | Purpose |
|----------|---------|
| [API.md](API.md) | REST endpoints, authentication, HATEOAS |
| [BLAZOR.md](BLAZOR.md) | Blazor Server/WASM, component patterns, state |
| [SECURITY.md](SECURITY.md) | AuthN/AuthZ, Keycloak integration, JWT handling |
| [FEDERATION.md](FEDERATION.md) | ATProto/ActivityPub integration plans |

### Operations

| Document | Purpose |
|----------|---------|
| [CONFIGURATION.md](CONFIGURATION.md) | Environment variables, settings, secrets |
| [OPERATIONS.md](OPERATIONS.md) | Deployment, CI/CD, monitoring |
| [TROUBLESHOOTING.md](TROUBLESHOOTING.md) | Common issues, debugging, solutions |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Development workflow, PR guidelines |
| [PROJECT.md](PROJECT.md) | Project vision, roadmap, milestones |

---

## Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 (Preview) | Runtime & framework |
| ASP.NET Core | 10.0 | Web API framework |
| Entity Framework Core | 10.0 | ORM & data access |
| MediatR | 12.x | CQRS mediator |
| FluentValidation | 11.x | Input validation |
| AutoMapper | 12.x | Object mapping |
| Serilog | 4.x | Structured logging |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| Blazor Server | 9.0 | Server-side rendering, BFF |
| Blazor WASM | 9.0 | Client interactivity |
| MudBlazor | 8.x | UI component library |
| YARP | 2.x | Reverse proxy for BFF |

### Infrastructure

| Technology | Purpose |
|------------|---------|
| PostgreSQL 18 | Primary database |
| Keycloak 26 | Identity provider |
| MinIO | Object storage |
| Docker | Containerization |
| Coolify | Deployment platform |
| GitHub Actions | CI/CD pipeline |

---

## AI Development Resources

### Claude Code Integration

The project is fully optimized for AI-assisted development:

| Resource | Location | Purpose |
|----------|----------|---------|
| **Agent Instructions** | [CLAUDE.md](../CLAUDE.md) | Comprehensive AI agent guidelines |
| **Quick Reference** | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) | Fast lookup for rules |
| **Dev Context** | [dev/active/](../dev/active/) | Persistent session context |

### Specialized Agents (.claude/agents/)

| Agent | Purpose |
|-------|---------|
| `auth-route-debugger` | Debug OIDC/JWT authentication issues |
| `auth-route-tester` | Test authentication/authorization |
| `auto-error-resolver` | Resolve C#/.NET compilation errors |
| `blazor-component-architect` | Blazor component design |
| `code-architecture-reviewer` | Clean Architecture compliance |
| `code-refactor-master` | CQRS pattern enforcement |
| `documentation-architect` | XML docs, Swagger annotations |
| `frontend-error-fixer` | Blazor/MudBlazor debugging |
| `plan-reviewer` | Review development plans |
| `refactor-planner` | Strategic refactoring plans |
| `web-research-specialist` | .NET ecosystem research |
| `clean-code-architect` | Code quality enforcement |

### Skills (.claude/skills/)

| Skill | Resources |
|-------|-----------|
| `blazor-bff-patterns` | BFF config, auth state, token forwarding, service layer |
| `blazor-ui-conventions` | MudBlazor, BEM, theming, components, state, render modes |
| `clean-architecture-rules` | Dependency rules, layer responsibilities, violations |
| `cqrs-mediatr-guidelines` | Commands, queries, handlers, validation |
| `dotnet-efcore-guidelines` | DbContext, configs, repos, migrations, queries |
| `error-tracking` | Sentry config, exception handling, monitoring |
| `auth-patterns` | User ID extraction patterns |
| `prd` | Product requirements documentation |

### BMAD Framework (_bmad/)

Business Methodology for AI Development with specialized agents:
- `analyst` - Business analysis & requirements
- `architect` - Technical architecture
- `dev` - Development execution
- `pm` - Project management
- `sm` - Scrum master
- `ux-designer` - UX/UI design
- `tech-writer` - Technical documentation

---

## Domain Model Summary

### Core Entities

| Entity | Purpose | Relationships |
|--------|---------|---------------|
| `Event` | Primary aggregate | EventSessions, Categories, Tags, Location |
| `EventSession` | Session within event | AgendaItems, Speakers, Languages |
| `Organization` | Event organizer | Members, Reviews, Events |
| `Actor` | People/groups | Can be User or external |
| `Location` | Physical/virtual venues | Events |
| `Category` | Event categorization | Events (many-to-many) |
| `Tag` | Flexible tagging | Events (many-to-many) |

### Lookup Tables

`EventType` • `EventStatus` • `EventFormat` • `AudienceAge` • `AudienceGender` • `Madhab` • `Language` • `ActorType` • `ApprovalStatus` • `VisibilityType` • `RegistrationMode` • `Role` • `OrganizationPosition` • `FileType` • `TagType` • `DidCustodyType`

### Multi-Tenancy

All entities implement `ITenantEntity` with `TenantId` for data isolation:
- `Tenant` - Tenant configuration
- `TenantSettings` - Per-tenant settings
- `TenantUser` - User-tenant membership

**See**: [MULTI_TENANCY.md](MULTI_TENANCY.md) for the complete tenant isolation model, and [ADMIN_HIERARCHY.md](ADMIN_HIERARCHY.md) for the authority model.

### Federation (ATProto)

- `IndexedDid` - Indexed DIDs from network
- `AtprotoRecord` - Federated records
- `ActorKeyStore` - Cryptographic keys
- `SyncState` - Synchronization tracking

---

## API Reference

### Endpoint Categories

| Category | Controller | Routes |
|----------|------------|--------|
| **Events** | `EventController` | `/api/event` |
| **Sessions** | `EventSessionController` | `/api/eventsession` |
| **Organizations** | `OrganizationController` | `/api/organization` |
| **Actors** | `ActorController` | `/api/actor` |
| **Locations** | `LocationController` | `/api/location` |
| **Categories** | `CategoryController` | `/api/category` |
| **Tags** | `TagController` | `/api/tag` |
| **Users** | `UserController` | `/api/user` |
| **Tenants** | `TenantController` | `/api/tenant` |
| **Storage** | `StorageObjectController` | `/api/storageobject` |

### HATEOAS Support

All major endpoints support HAL+JSON hypermedia:
- Use `Accept: application/hal+json` header
- Embedded resources and link relations
- Self-documenting API responses

---

## Development Workflow

### Build Commands

```bash
# Restore & Build
dotnet restore
dotnet build

# Run Tests
dotnet test

# Run API
dotnet run --project Explore.API

# Run Blazor
dotnet run --project Explore.Blazor

# Docker Compose (full stack)
docker compose up -d
```

### Key Files for Development

| File | Purpose |
|------|---------|
| `Explore.sln` | Solution file |
| `docker-compose.yml` | Local development stack |
| `.github/workflows/deploy-coolify.yml` | CI/CD pipeline |
| `appsettings.json` | Application configuration |

---

## Repomix Snapshots

AI-optimized codebase snapshots in `.repomix/`:

| File | Content |
|------|---------|
| `repomix-api.md` | API layer codebase |
| `repomix-blazor.md` | Blazor frontend codebase |

---

## Related Resources

### External Documentation

- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)
- [MudBlazor](https://mudblazor.com/)
- [Keycloak](https://www.keycloak.org/documentation)
- [ATProto Specification](https://atproto.com/)

### MCP Server Tools

Available for AI-assisted development:
- **Context7** - Library documentation retrieval
- **Sequential Thinking** - Multi-step reasoning
- **Tavily** - Web scraping & data extraction
- **Perplexity** - Technical research
- **At-Explore** - ATProto integration

---

## Maintenance

### Document Updates

| Document | Update Frequency |
|----------|------------------|
| CLAUDE.md | On rule changes |
| QUICK_REFERENCE.md | On pattern changes |
| ARCHITECTURE.md | On structural changes |
| index.md (this file) | On major project changes |

### Generated By

This index was generated using the BMAD Document Project workflow:
- **Workflow**: `_bmad/bmm/workflows/document-project/workflow.yaml`
- **Scan Level**: Exhaustive (read all source files)
- **State File**: `docs/project-scan-report.json`

---

*Last Updated: February 2026*
