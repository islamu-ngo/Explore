# Codebase Structure Reference

> Complete directory map for AI agents and developers.
> Lists all folders with max 2 key files per folder for quick lookup.
> Last Updated: February 2026

---

## Solution Overview

The solution uses **Clean Architecture** with CQRS (MediatR). There are **two naming prefixes**:

- **Explore.\*** - Main application projects (API, Domain, Application, Persistence, Blazor, Infrastructure) (historical naming from when the project was "Explore", were not renamed to "Event" yet)
- **Event.\*** - Test projects (for now, later all projects will be renamed to "Event" prefix)

---

## Project Dependency Graph

```
Explore.Domain          (no dependencies - pure entities & interfaces)
    ↑
Explore.Application     (depends on Domain - CQRS handlers, DTOs, contracts)
    ↑
Explore.Persistence     (depends on Application - EF Core, repositories)
Explore.Infrastructure  (depends on Application - email, storage, identity)
Explore.Secrets         (standalone - secrets management)
    ↑
Explore.API             (depends on all above - controllers, middleware, DI)
Explore.Blazor          (Blazor Server host - depends on API indirectly)
Explore.Blazor.Client   (Blazor WASM - standalone client, calls API via BFF)
    ↑
Explore.AppHost         (.NET Aspire orchestrator)
Explore.ServiceDefaults (shared Aspire defaults)
Explore.Diagnostic      (diagnostic/observability extensions)
Event.MigrationService  (EF Core migration runner)
```

---

## Directory Tree

### Explore.Domain/ — Domain Entities & Interfaces

Pure domain model. No external dependencies. All entities live at root level.

```
Explore.Domain/
├── *.cs                          — Entity classes (one per file)
│   ├── Event.cs                  — Core event entity (ITenantEntity, IAuditableEntity, ISoftDeletable)
│   └── Organization.cs           — Organization entity with appearance metadata
├── Interfaces/                   — Marker interfaces for cross-cutting concerns
│   ├── IAuditableEntity.cs       — CreatedAt/By, UpdatedAt/By audit fields
│   ├── ISoftDeletable.cs         — IsDeleted, DeletedAt/By soft delete fields
│   └── ITenantEntity.cs          — TenantId for multi-tenant isolation
├── Enums/                        — Enums mirroring lookup table IDs
│   ├── EventTypeEnum.cs          — Maps to EventType lookup table int IDs
│   └── EventStatusEnum.cs        — Maps to EventStatus lookup table int IDs
├── Constants/                    — Platform-wide constants
│   ├── GovernanceSettingKeys.cs  — SystemSetting key strings (deployment mode, branding, domains)
│   └── PlatformDefaults.cs       — Default values for platform settings
├── Federation/                   — ATProto federation entities
│   └── PdsSyncOutbox.cs          — Outbox pattern for PDS synchronization
└── Modules/                      — Module governance entities
    ├── ModuleDefinition.cs       — Module definitions (Core, Islamic, Tech)
    └── TenantCapability.cs       — Per-tenant module enablement
```

**Entity categories:**
- **Main entities** (Guid PK): Event, Organization, Actor, User, Tenant, Location, StorageObject, Category, Tag
- **Lookup tables** (int PK): EventType, EventStatus, EventFormat, AudienceAge, AudienceGender, Madhab, Language, VisibilityType, RegistrationMode, OrganizationRole, OrganizationPosition
- **Link/junction tables** (composite PK): EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker, TagTypeTags, OrganizationMember, UserRole
- **Aspect tables** (1:1 optional): EventIslamicAspect, EventTechAspect
- **Admin/onboarding**: InstanceAdministrator, InstanceBootstrapState, TenantAdministrator, TenantOnboardingState

---

### Explore.Application/ — CQRS Handlers, DTOs, Contracts

Application layer. Depends only on Domain. Contains all business logic orchestration.

```
Explore.Application/
├── Features/                     — CQRS feature slices (one folder per aggregate)
│   ├── Events/                   — Event aggregate (largest feature)
│   │   ├── Handlers/
│   │   │   ├── Commands/         — CreateEventCommandHandler.cs, UpdateEventCommandHandler.cs, etc.
│   │   │   └── Queries/          — GetEventListRequestHandler.cs, GetEventDetailsRequestHandler.cs, etc.
│   │   └── Requests/
│   │       ├── Commands/         — CreateEventCommand.cs, UpdateEventCommand.cs, etc.
│   │       └── Queries/          — GetEventListRequest.cs, GetEventDetailsRequest.cs, etc.
│   ├── Organizations/            — Organization aggregate (same Handlers/Requests structure)
│   ├── InstanceOnboarding/       — Instance-level admin onboarding
│   │   └── Common/               — Shared helpers for onboarding features
│   ├── TenantOnboarding/         — Tenant-level onboarding flow
│   ├── PublicExperience/         — Public-facing discovery (landing pages)
│   └── [30+ more feature folders] — Each follows Handlers/{Commands,Queries} + Requests/{Commands,Queries}
├── DTOs/                         — Data Transfer Objects (one folder per entity)
│   ├── Event/                    — CreateEventDto.cs, UpdateEventDto.cs, EventDto.cs, EventListDto.cs
│   ├── Organization/             — CreateOrganizationDto.cs, OrganizationDto.cs, OrganizationListDto.cs
│   ├── Onboarding/               — DTOs for instance/tenant onboarding flows
│   └── [40+ more DTO folders]    — Each entity has Create/Update/Detail/List DTOs
├── Contracts/                    — Interface definitions (dependency inversion)
│   ├── Persistence/              — Repository interfaces (one per entity)
│   │   ├── IGenericRepository.cs — Base generic repository interface
│   │   ├── IEventRepository.cs   — Event-specific repository methods
│   │   └── [50+ more interfaces] — One interface per entity/aggregate
│   ├── Infrastructure/           — Infrastructure service interfaces
│   │   ├── ITenantContext.cs     — Multi-tenant context resolution
│   │   ├── ICurrentUserService.cs— Current authenticated user info
│   │   ├── IObjectStorageService.cs — S3-compatible storage
│   │   └── ISettingsResolver.cs  — Governance settings resolution
│   ├── Identity/                 — User identity interfaces
│   │   └── IUserContext.cs       — User context abstraction
│   ├── Strategies/               — Strategy pattern interfaces
│   │   ├── IEventStrategy.cs     — Module-specific event behavior
│   │   └── IStrategyResolver.cs  — Strategy factory interface
│   └── Hateoas/                  — HATEOAS link assembly interfaces
├── Responses/                    — Shared response types
│   ├── BaseCommandResponse.cs    — Generic command response with Id, Success, Message, Errors
│   └── PaginatedResult.cs        — Pagination wrapper
├── Exceptions/                   — Custom exception types
│   ├── NotFoundException.cs      — 404 mapping
│   ├── BadRequestException.cs    — 400 mapping
│   └── ValidationException.cs    — Validation failure mapping
├── Profiles/                     — AutoMapper profiles
│   └── MappingProfile.cs         — Single file with ALL entity↔DTO mappings
├── Hateoas/                      — HAL resource models
│   ├── HalResource.cs            — Base HAL envelope
│   └── HalLink.cs                — Link representation
├── Models/                       — Infrastructure models
│   └── EmailSettings.cs          — Email configuration model
└── Requests/                     — Shared request types
    └── PaginationParams.cs       — Page number/size parameters
```

**CQRS pattern note:** Most features follow `Handlers/{Commands,Queries}` + `Requests/{Commands,Queries}`. One exception: `OrganizationReviews` uses `Commands/` and `Queries/` directly (flattened structure).

---

### Explore.Persistence/ — EF Core, Database, Repositories

Data access layer. Implements repository interfaces from Application.

```
Explore.Persistence/
├── ExploreDbContext.cs            — Main DbContext with DbSets, query filters, SaveChanges override
├── PersistenceServicesRegistration.cs — DI registration for all repositories
├── Repositories/                  — Repository implementations
│   ├── GenericRepository.cs       — Base CRUD with soft delete awareness
│   ├── EventRepository.cs         — Event-specific queries with eager loading
│   └── [55+ more repositories]    — One per entity, extends GenericRepository
├── Configurations/
│   └── Entities/                  — EF Core entity configurations (Fluent API)
│       ├── EventConfiguration.cs  — Event table mapping, relationships, indexes
│       ├── OrganizationConfiguration.cs — Organization table with JSON columns
│       └── [40+ more configs]     — One per entity; lookup tables include HasData() seeding
├── Migrations/                    — EF Core migration files
│   ├── ExploreDbContextModelSnapshot.cs — Current model state
│   └── [timestamped migration files]    — Chronological schema changes
├── QueryFilters/                  — Named query filter constants
│   └── QueryFilterNames.cs        — "Tenant" and "SoftDelete" filter name constants
├── Seed/                          — Database seeding logic
│   ├── DatabaseSeeder.cs          — Orchestrates seeding (calls LookupTableSeeder + SeedData)
│   ├── SeedData.cs                — Runtime seed data (tenants, users, organizations, events)
│   ├── SeedIds.cs                 — Deterministic UUIDv7 GUIDs for seed data
│   └── LookupTableSeeder.cs       — Ensures lookup table values match enum definitions
└── ValueGenerators/               — Custom EF Core value generators
```

---

### Explore.API/ — ASP.NET Core Web API

API layer. Wires everything together via DI. Contains controllers, middleware, and configuration.

```
Explore.API/
├── Program.cs                     — Application entry point, all DI registration
├── appsettings.json               — Configuration (connection strings, auth, storage)
├── Controllers/                   — API controllers (one per entity/aggregate)
│   ├── EventController.cs         — Event CRUD endpoints
│   ├── OrganizationController.cs  — Organization endpoints
│   ├── InstanceOnboardingController.cs — Instance admin bootstrap
│   ├── TenantOnboardingController.cs   — Tenant setup wizard
│   └── [40+ more controllers]     — One per entity; GET=AllowAnonymous, POST/PUT/DELETE=Authorize
├── Services/                      — API-layer services
│   └── TenantContext.cs           — Multi-tenant resolution (header → custom domain → subdomain → default)
├── Middleware/                     — HTTP pipeline middleware
│   ├── ExceptionMiddleware.cs     — Global exception handler (maps exceptions to HTTP status codes)
│   └── PreferHeaderMiddleware.cs  — Content negotiation via Prefer header
├── Extensions/                    — DI and configuration extensions
│   └── ServiceCollectionExtensions.cs — Service registration helpers
├── Filters/                       — Action filters
│   └── BlockInSingleTenantAttribute.cs — Blocks multi-tenant endpoints in single-tenant mode
├── Hateoas/                       — HATEOAS link generation
│   ├── HateoasLinkGenerator.cs    — Generates HAL _links for API responses
│   └── RouteNames.cs              — Named route constants
├── OpenApi/                       — Scalar/OpenAPI customization
│   └── HalSchemaTransformer.cs    — Transforms OpenAPI schema for HAL format
├── BackgroundServices/            — Hosted background workers
│   └── PdsSyncWorker.cs           — ATProto PDS synchronization worker
├── Static/                        — Static file serving configuration
├── swagger.json                   — Generated OpenAPI specification
└── Properties/
    └── launchSettings.json        — Development server URLs and profiles
```

---

### Explore.Blazor/ — Blazor Server Host

Server-side host for the Blazor application. Handles SSR, authentication, and proxies to API.

```
Explore.Blazor/
├── Program.cs                     — Server-side host configuration, OIDC auth setup, BFF proxy
├── Components/
│   ├── App.razor                  — Root component (HTML head, body, Blazor script tags)
│   └── Routes.razor               — Router configuration with render modes
├── Extensions/
│   └── ConfigurationExtension.cs  — Configuration helpers for Blazor Server
├── Services/
│   └── CircuitAccessTokenService.cs — Captures OIDC tokens for circuit-lifetime use
└── wwwroot/                       — Static assets (CSS, JS, images)
```

---

### Explore.Blazor.Client/ — Blazor WebAssembly Client

Interactive WASM client. Contains all pages, components, and service proxies.

```
Explore.Blazor.Client/
├── Program.cs                     — WASM host builder, service registration, HttpClient setup
├── Pages/                         — Routable page components
│   ├── Event/                     — Event pages
│   │   ├── EventList.razor/.cs    — Event listing/discovery page
│   │   ├── EventDetail.razor/.cs  — Single event detail view
│   │   ├── CreateEvent.razor/.cs  — Event creation wizard
│   │   └── MyEvents.razor/.cs     — User's own events
│   ├── Organization/              — Organization pages
│   │   ├── OrganizationDetails.razor/.cs — Organization detail view
│   │   ├── CreateOrganization.razor/.cs  — Organization creation
│   │   └── OrganizationProfile.razor/.cs — Organization profile management
│   ├── Admin/                     — Admin pages
│   │   ├── LookupTables.razor/.cs — Admin panel for all lookup tables
│   │   ├── Instance/              — Instance-level admin settings
│   │   └── Tenant/                — Tenant-level admin settings
│   ├── Auth/                      — Authentication pages
│   ├── Landing/                   — Public landing page
│   ├── Onboarding/                — Instance/tenant onboarding wizard pages
│   └── User/                      — User profile and settings
├── Services/                      — API proxy services (one per domain area)
│   ├── EventService.cs            — Event API calls
│   ├── OrganizationService.cs     — Organization API calls
│   ├── BffClient.cs               — BFF HTTP client with XSRF token handling
│   ├── AuthStateService.cs        — Centralized authentication state
│   ├── LookupCacheService.cs      — Client-side cache for lookup table data
│   └── ImageStorageService.cs     — S3 presigned URL upload handling
├── Components/                    — Reusable UI components
│   ├── ImageUpload.razor          — Image upload with S3 presigned URLs
│   ├── S3Image.razor              — Image display from S3 storage
│   ├── ReviewDialog.razor         — Generic review/rating dialog
│   └── Loading.razor              — Loading spinner component
├── Layout/                        — Layout components
│   ├── MainLayout.razor/.cs       — Main page layout with drawer navigation
│   ├── NavMenu.razor/.cs          — Navigation menu component
│   └── Footer.razor               — Site footer
├── Clients/                       — Generated API clients
│   ├── EventApiClient.cs          — NSwag-generated client interface + implementation
│   └── EventApiClient.g.cs        — Auto-generated client code from OpenAPI spec
├── Models/                        — Client-side models
│   ├── UserInfo.cs                — Deserialized user info from BFF
│   ├── TenantContext.cs           — Client-side tenant state
│   └── Responses/
│       ├── BaseCommandResponse.cs — Client-side mirror of API response
│       └── ServiceResult.cs       — Service operation result wrapper
├── Helpers/                       — UI helper utilities
│   ├── EventAppearanceMetadataHelper.cs     — Event display metadata
│   └── OrganizationAppearanceMetadataHelper.cs — Organization display metadata
├── Validators/                    — Client-side FluentValidation validators
│   └── CreateEventDtoValidator.cs — Client-side event creation validation
├── Configuration/                 — Client configuration models
│   └── TenantConfiguration.cs     — Tenant config for WASM
├── Constants/                     — UI constants
├── Routing/
│   └── Guards/                    — Route guard implementations
│       ├── AuthenticatedRouteGuard.cs — Requires authentication
│       └── AdminRouteGuard.cs     — Requires admin role
└── wwwroot/                       — Static web assets
```

---

### Explore.Infrastructure/ — External Services

Infrastructure implementations for Application contracts.

```
Explore.Infrastructure/
├── InfrastructureServicesRegistration.cs — DI registration
├── DeploymentSettings.cs          — Deployment mode configuration model
├── Identity/
│   └── UserContext.cs             — User identity from HttpContext claims
├── Mail/
│   └── EmailSender.cs            — Email sending implementation
├── Services/
│   ├── CurrentUserService.cs     — Current user ID extraction with claim fallback
│   ├── ObjectStorageService.cs   — S3-compatible object storage (Hetzner)
│   ├── ModuleService.cs          — Module governance checks
│   └── SettingsResolver.cs       — SystemSetting/TenantSetting resolution
└── Strategies/
    ├── StrategyResolver.cs       — Resolves event strategy by module type
    ├── IslamicEventStrategy.cs   — Islamic module-specific event behavior
    └── TechEventStrategy.cs      — Tech module-specific event behavior
```

---

### Explore.Secrets/ — Secrets Management Library

Standalone library for multi-provider secret management.

```
Explore.Secrets/
├── Abstractions/                  — Core interfaces and types
│   ├── ISecretProvider.cs         — Secret retrieval interface
│   └── IEncryptionService.cs      — Encryption abstraction
├── Providers/                     — Secret provider implementations
│   ├── EnvironmentSecretProvider.cs  — Environment variable secrets
│   └── InfisicalSecretProvider.cs    — Infisical secret manager integration
├── Services/                      — Secret services
│   ├── SecretProviderFactory.cs   — Creates provider based on configuration
│   ├── SecretRefreshService.cs    — Background secret rotation
│   └── AesEncryptionService.cs    — AES encryption implementation
├── Configuration/                 — Options and configuration models
│   └── SecretProviderOptions.cs   — Provider selection configuration
├── Extensions/                    — DI registration
│   └── ServiceCollectionExtensions.cs — AddSecrets() extension method
├── Validation/
│   └── RequiredSecretsValidator.cs — Validates required secrets exist at startup
└── Observability/
    ├── SecretProviderHealthCheck.cs — Health check for secret providers
    └── SecretRefreshMetrics.cs    — Metrics for secret refresh operations
```

---

### Test Projects

All test projects use **TUnit** as the test framework (not xUnit/NUnit).

```
Event.Application.UnitTests/       — Application layer unit tests
├── Features/                      — Tests organized by feature (mirrors Application/Features)
├── Common/                        — Shared test utilities and mocks
└── Hateoas/                       — HATEOAS assembly tests

Event.Domain.UnitTests/            — Domain entity unit tests

Event.Architecture.Tests/          — Architecture fitness tests
├── CleanArchitectureTests.cs      — Validates dependency rules between layers
├── CqrsPatternTests.cs            — Validates CQRS naming and structure conventions
└── NamingConventionTests.cs       — Enforces naming patterns across codebase

Event.Persistence.IntegrationTests/— Database integration tests
├── Fixtures/                      — Test fixtures (in-memory or containerized DB)
└── Repositories/                  — Repository-level integration tests

Event.API.IntegrationTests/        — API endpoint integration tests
├── Fixtures/                      — WebApplicationFactory setup
└── Features/                      — Tests organized by feature

Explore.Blazor.Client.Tests/       — Blazor UI component tests (bUnit)
├── Pages/                         — Page component tests
├── Services/                      — Service unit tests
├── Layout/                        — Layout component tests
├── Integration/                   — Integration-level UI tests
└── Common/                        — Shared test helpers

Explore.Secrets.UnitTests/         — Secrets library tests
├── Providers/                     — Provider implementation tests
├── Services/                      — Service tests
├── Configuration/                 — Configuration tests
├── Validation/                    — Validator tests
└── Observability/                 — Health check and metrics tests
```

---

### Supporting Projects

```
Explore.AppHost/                   — .NET Aspire orchestrator
├── AppHost.cs                     — Defines service topology (API, Blazor, PostgreSQL, etc.)

Explore.ServiceDefaults/           — Shared Aspire service defaults
├── Extensions.cs                  — OpenTelemetry, health checks, resilience defaults

Explore.Diagnostic/                — Observability extensions
├── DiagnosticServiceCollectionExtensions.cs — Serilog, metrics, tracing setup

Event.MigrationService/            — EF Core migration runner (worker service)
├── Program.cs                     — Host builder
├── Worker.cs                      — Migration execution logic
└── Extensions/
    └── ConfigurationExtensions.cs — Connection string resolution
```

---

### Non-Code Directories

```
docs/                              — Project documentation (ARCHITECTURE.md, API.md, BLAZOR.md, etc.)
dev/                               — Development context and task tracking
├── active/                        — Current work-in-progress task docs
│   └── [task-name]/               — Plan, context, and tasks files per active work item
├── _journal/                      — Session journal and decisions log
└── archive/                       — Completed task documentation

schemas/                           — Database schema documentation
├── islamu-event.md                — Full database schema reference

docker/                            — Docker configurations
├── keycloak/                      — Keycloak IdP configuration
└── minio/                         — MinIO S3-compatible storage config

lexicons/                          — ATProto lexicon schema definitions
scripts/                           — Utility scripts
images/                            — Documentation images
inbox/                             — Temporary staging area

_bmad/                             — BMAD methodology templates
_bmad-output/                      — BMAD workflow outputs
```

---

## Quick File Lookup by Concern

| Need to work on... | Look in... |
|---|---|
| Add/modify a domain entity | `Explore.Domain/EntityName.cs` |
| Add a lookup table enum | `Explore.Domain/Enums/EntityNameEnum.cs` |
| Create/update DTO | `Explore.Application/DTOs/EntityName/` |
| Create CQRS command/query | `Explore.Application/Features/EntityNames/Requests/` |
| Create command/query handler | `Explore.Application/Features/EntityNames/Handlers/` |
| Add repository interface | `Explore.Application/Contracts/Persistence/IEntityNameRepository.cs` |
| Implement repository | `Explore.Persistence/Repositories/EntityNameRepository.cs` |
| Add EF configuration | `Explore.Persistence/Configurations/Entities/EntityNameConfiguration.cs` |
| Add API controller | `Explore.API/Controllers/EntityNameController.cs` |
| Add Blazor page | `Explore.Blazor.Client/Pages/AreaName/PageName.razor` |
| Add client service | `Explore.Blazor.Client/Services/EntityNameService.cs` |
| Register DI services | `Explore.Persistence/PersistenceServicesRegistration.cs` (repos) or `Explore.Blazor.Client/Program.cs` (client) |
| Add seed data | `Explore.Persistence/Seed/SeedData.cs` (runtime) or entity configuration `HasData()` (lookup) |
| Multi-tenancy logic | `Explore.API/Services/TenantContext.cs` + `Explore.Persistence/ExploreDbContext.cs` |
| Authentication | `Explore.Blazor/Program.cs` (OIDC) + `Explore.Infrastructure/Services/CurrentUserService.cs` |
| Secret management | `Explore.Secrets/` (standalone library) |
| Database migrations | `dotnet ef` via `Event.MigrationService/` |
| Architecture tests | `Event.Architecture.Tests/` |
