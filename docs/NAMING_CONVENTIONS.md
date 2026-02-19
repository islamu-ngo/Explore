# Naming Conventions

> Comprehensive naming patterns used across the codebase.
> Follow these exactly when creating new files, classes, or members.
> Last Updated: February 2026

---

## Project Naming

| Project | Pattern | Purpose |
|---|---|---|
| `Explore.Domain` | `Explore.{Layer}` | Core domain entities |
| `Explore.Application` | `Explore.{Layer}` | Business logic / CQRS |
| `Explore.Persistence` | `Explore.{Layer}` | Data access / EF Core |
| `Explore.Infrastructure` | `Explore.{Layer}` | External services |
| `Explore.API` | `Explore.{Layer}` | Web API host |
| `Explore.Blazor` | `Explore.Blazor` | Blazor Server host |
| `Explore.Blazor.Client` | `Explore.Blazor.Client` | Blazor WASM client |
| `Explore.Secrets` | `Explore.{Library}` | Standalone library |
| `Event.Application.UnitTests` | `Event.{Layer}.{TestType}` | Test projects (legacy "Event" prefix) |
| `Event.Architecture.Tests` | `Event.Architecture.Tests` | Architecture fitness tests |

**Note:** Main projects use the `Explore.*` prefix (historical naming, will be renamed to `Event.*` in the future). Test projects already use `Event.*` prefix.

---

## File Naming

All source files use **PascalCase** matching the primary class/type name:

| File type | Pattern | Example |
|---|---|---|
| Entity | `{EntityName}.cs` | `Event.cs`, `Organization.cs` |
| Enum | `{EntityName}Enum.cs` | `EventTypeEnum.cs`, `EventStatusEnum.cs` |
| Interface | `I{Name}.cs` | `IEventRepository.cs`, `ITenantContext.cs` |
| DTO | `{Prefix}{EntityName}Dto.cs` | `CreateEventDto.cs`, `EventListDto.cs` |
| Command | `{Verb}{EntityName}Command.cs` | `CreateEventCommand.cs` |
| Query | `Get{EntityName}{Suffix}Request.cs` | `GetEventListRequest.cs` |
| Handler | `{CommandOrQuery}Handler.cs` | `CreateEventCommandHandler.cs` |
| Repository interface | `I{EntityName}Repository.cs` | `IEventRepository.cs` |
| Repository implementation | `{EntityName}Repository.cs` | `EventRepository.cs` |
| EF Configuration | `{EntityName}Configuration.cs` | `EventConfiguration.cs` |
| Controller | `{EntityName}Controller.cs` | `EventController.cs` |
| Blazor page | `{PageName}.razor` + `{PageName}.razor.cs` | `EventList.razor` |
| Blazor CSS | `{PageName}.razor.css` | `EventList.razor.css` |
| Client service | `{EntityName}Service.cs` | `EventService.cs` |
| Validator | `{Prefix}{EntityName}DtoValidator.cs` | `CreateEventDtoValidator.cs` |

---

## Namespace Conventions

**File-scoped namespaces** are required (C# 10+ style):

```
namespace Explore.Application.Features.Events.Handlers.Commands;
```

Namespace structure mirrors folder structure exactly:

| Layer | Namespace |
|---|---|
| Domain entities | `Explore.Domain` |
| Domain enums | `Explore.Domain.Enums` |
| Domain interfaces | `Explore.Domain.Interfaces` |
| Application DTOs | `Explore.Application.DTOs.{EntityName}` |
| Application features | `Explore.Application.Features.{EntityNames}.Handlers.{Commands\|Queries}` |
| Application requests | `Explore.Application.Features.{EntityNames}.Requests.{Commands\|Queries}` |
| Application contracts | `Explore.Application.Contracts.{Persistence\|Infrastructure\|Identity}` |
| Persistence repos | `Explore.Persistence.Repositories` |
| Persistence configs | `Explore.Persistence.Configurations.Entities` |
| API controllers | `Explore.API.Controllers` |
| API services | `Explore.API.Services` |
| Blazor client services | `Explore.Blazor.Client.Services` |
| Blazor client pages | `Explore.Blazor.Client.Pages.{Area}` |

---

## Entity Naming

### Primary Entities (Guid PK)

Singular noun, PascalCase. One class per file at `Explore.Domain/` root.

- `Event`, `Organization`, `Actor`, `User`, `Tenant`, `Location`, `StorageObject`, `Category`, `Tag`

### Lookup Tables (int PK)

Singular noun. These are reference data that rarely changes.

- `EventType`, `EventStatus`, `EventFormat`, `AudienceAge`, `AudienceGender`
- `Madhab`, `Language`, `VisibilityType`, `RegistrationMode`
- `Role`, `OrganizationPosition`, `ActorType`, `FileType`, `DidCustodyType`

### Link/Junction Tables (Composite PK)

Named as `{Parent}{Child}` (plural for many-to-many collections):

- `EventCategories` — Event ↔ Category
- `EventTags` — Event ↔ Tag
- `EventSessionLanguage` — EventSession ↔ Language
- `EventSessionSpeaker` — EventSession ↔ Speaker (User)
- `TagTypeTags` — TagType ↔ Tag
- `OrganizationMember` — Organization ↔ User (with Role via `RoleId`)
- `TenantUser` — Tenant ↔ User (with Role via `RoleId`)
- `RolePermission` — Role ↔ Permission

**Note:** Some junction table names are plural (`EventCategories`, `EventTags`) while others are singular (`OrganizationMember`, `EventSessionLanguage`). Follow the existing pattern when extending.

### Aspect Tables (1:1 Optional Extension)

Named as `{Parent}{Module}Aspect`:

- `EventIslamicAspect` — Islamic-specific fields for Event
- `EventTechAspect` — Tech-specific fields for Event

### Settings Entities

- `SystemSetting` — Instance-wide settings (global)
- `TenantSetting` — Per-tenant setting overrides
- `TenantSettings` — Tenant settings snapshot entity (different from TenantSetting)
- `AppSetting` — Application-level settings

---

## Enum Naming

Enums are named `{EntityName}Enum` and placed in `Explore.Domain/Enums/`. They mirror the int IDs used in lookup tables:

```
EventTypeEnum.cs       → values: Conference = 1, Workshop = 2, ...
EventStatusEnum.cs     → values: Draft = 1, Published = 2, ...
AudienceAgeEnum.cs     → values: AllAges = 1, Children = 2, ...
```

Each enum value maps to a lookup table row's int `Id`.

---

## DTO Naming

DTOs follow a strict prefix/suffix pattern per entity:

| DTO Purpose | Pattern | Example |
|---|---|---|
| Full details (read) | `{EntityName}Dto` | `EventDto`, `OrganizationDto` |
| List view (read) | `{EntityName}ListDto` | `EventListDto`, `OrganizationListDto` |
| Create payload (write) | `Create{EntityName}Dto` | `CreateEventDto` |
| Update payload (write) | `Update{EntityName}Dto` | `UpdateEventDto` |

DTOs live in `Explore.Application/DTOs/{EntityName}/` — one folder per entity, all DTOs for that entity in the same folder.

---

## CQRS Naming

### Commands (Write Operations)

| Component | Pattern | Example |
|---|---|---|
| Command request | `{Verb}{EntityName}Command` | `CreateEventCommand` |
| Command handler | `{Verb}{EntityName}CommandHandler` | `CreateEventCommandHandler` |

Common verbs: `Create`, `Update`, `Delete`, `Add`, `Remove`

### Queries (Read Operations)

| Component | Pattern | Example |
|---|---|---|
| Query request | `Get{EntityName}{Suffix}Request` | `GetEventListRequest`, `GetEventDetailsRequest` |
| Query handler | `Get{EntityName}{Suffix}RequestHandler` | `GetEventListRequestHandler` |

Common suffixes: `List`, `Details`, `ById`, `By{RelatedEntity}`

### Folder Structure

Features are pluralized: `Events/`, `Organizations/`, `AudienceAges/`

```
Features/{EntityNames}/
├── Requests/
│   ├── Commands/    — {Verb}{EntityName}Command.cs
│   └── Queries/     — Get{EntityName}{Suffix}Request.cs
└── Handlers/
    ├── Commands/    — {Verb}{EntityName}CommandHandler.cs
    └── Queries/     — Get{EntityName}{Suffix}RequestHandler.cs
```

---

## Repository Naming

| Component | Pattern | Example |
|---|---|---|
| Interface | `I{EntityName}Repository` | `IEventRepository` |
| Implementation | `{EntityName}Repository` | `EventRepository` |
| Generic base | `IGenericRepository<T, TKey>` | `GenericRepository<T, TKey>` |

Interfaces in: `Explore.Application/Contracts/Persistence/`
Implementations in: `Explore.Persistence/Repositories/`

---

## Controller Naming

Controllers are named `{EntityName}Controller` and placed in `Explore.API/Controllers/`. They map 1:1 with domain aggregates:

| Controller | Route |
|---|---|
| `EventController` | `api/event` |
| `OrganizationController` | `api/organization` |
| `EventSessionController` | `api/eventsession` |

Route convention: `api/[controller]` (controller name lowercased, no hyphens).

---

## Blazor Page Naming

Pages use PascalCase and are organized by area:

| Area | Page pattern | Example |
|---|---|---|
| Event pages | `Pages/Event/{Action}.razor` | `EventList.razor`, `CreateEvent.razor` |
| Organization | `Pages/Organization/{Action}.razor` | `OrganizationDetails.razor` |
| Admin | `Pages/Admin/{Area}.razor` | `LookupTables.razor` |

Each page has a code-behind: `{PageName}.razor.cs` (partial class).
Optional scoped CSS: `{PageName}.razor.css`.

---

## Service Naming (Blazor Client)

Client services follow `{EntityName}Service` with interface `I{EntityName}Service`:

| Service | Purpose |
|---|---|
| `EventService` / `IEventService` | Event API calls |
| `OrganizationService` / `IOrganizationService` | Organization API calls |
| `LookupCacheService` / `ILookupCacheService` | Caches lookup table data |
| `AuthStateService` / `IAuthStateService` | Centralized auth state |
| `BffClient` | Low-level BFF HTTP calls with XSRF |
| `ImageStorageService` / `IImageStorageService` | S3 presigned URL uploads |

---

## Database Column Naming

EF Core is configured with **snake_case naming convention** (via `UseSnakeCaseNamingConvention()`):

| C# Property | Database Column |
|---|---|
| `EventTypeId` | `event_type_id` |
| `CreatedAt` | `created_at` |
| `IsDeleted` | `is_deleted` |
| `TenantId` | `tenant_id` |

Table names are also snake_case plurals of the entity name.

---

## Seed Data ID Conventions

- **Lookup tables** (int PK): IDs defined in enum files, seeded via `HasData()` in EF configurations
- **Main entities** (Guid PK): IDs defined in `SeedIds.cs` using deterministic **UUIDv7** format
- UUIDv7 pattern: `018e4e5c-xxxx-7xxx-8xxx-xxxxxxxxxxxx` (time-ordered, unique per entity type)
- Seed IDs must never change once used in production

---

## Test Naming

### Test Projects

| Project | Test type | Framework |
|---|---|---|
| `Event.Application.UnitTests` | Unit | TUnit |
| `Event.Domain.UnitTests` | Unit | TUnit |
| `Event.Architecture.Tests` | Architecture | TUnit |
| `Event.Persistence.IntegrationTests` | Integration | TUnit |
| `Event.API.IntegrationTests` | Integration | TUnit |
| `Explore.Blazor.Client.Tests` | Component | bUnit + TUnit |
| `Explore.Secrets.UnitTests` | Unit | TUnit |

### Test Class Naming

Test classes mirror the class under test with a `Tests` suffix:

- `CreateEventCommandHandlerTests` — Tests `CreateEventCommandHandler`
- `EventRepositoryTests` — Tests `EventRepository`
- `CleanArchitectureTests` — Architecture rule tests

### Test Method Naming

Methods use descriptive names: `{Method}_{Scenario}_{ExpectedResult}`

---

## Configuration Naming

### EF Core Configuration

| Component | Pattern | Location |
|---|---|---|
| Entity config class | `{EntityName}Configuration` | `Configurations/Entities/` |
| Implements | `IEntityTypeConfiguration<{EntityName}>` | — |

### DI Registration

| Registration file | Pattern |
|---|---|
| `PersistenceServicesRegistration.cs` | `CongfigurePersistenceServices()` (note: typo is intentional/existing) |
| `InfrastructureServicesRegistration.cs` | `ConfigureInfrastructureServices()` |
| `Explore.Blazor.Client/Program.cs` | Direct service registration |
| `Explore.API/Program.cs` | Calls all registration methods |

---

## Constants and Settings Key Naming

Governance setting keys use dot-separated hierarchical names:

```
Deployment.Mode
Events.MaxSessionsPerEvent
Events.RequireApproval
Modules.Islamic.Enabled
Modules.Tech.Enabled
Branding.DisplayName
Domains.InstanceBaseDomain
```

Defined in `Explore.Domain/Constants/GovernanceSettingKeys.cs`.

---

## Summary: Quick Rules

1. **Entities** = singular PascalCase, one per file at Domain root
2. **Enums** = `{Entity}Enum` in `Domain/Enums/`
3. **DTOs** = `{Prefix}{Entity}Dto` in `Application/DTOs/{Entity}/`
4. **Commands** = `{Verb}{Entity}Command` in `Features/{Entities}/Requests/Commands/`
5. **Queries** = `Get{Entity}{Suffix}Request` in `Features/{Entities}/Requests/Queries/`
6. **Handlers** = mirror command/query name + `Handler` suffix
7. **Repositories** = `I{Entity}Repository` (interface) / `{Entity}Repository` (impl)
8. **Controllers** = `{Entity}Controller`, route `api/[controller]`
9. **Blazor pages** = `{Action}{Entity}.razor` or `{Entity}{Action}.razor` (varies by area)
10. **Client services** = `{Entity}Service` / `I{Entity}Service`
11. **DB columns** = snake_case (automatic via EF Core convention)
12. **Namespaces** = file-scoped, mirror folder structure exactly
