ABOUTME: Complete directory map showing all projects, folders, and key files.
ABOUTME: Helps AI agents and developers locate code quickly without full-repo scanning.

# Codebase Structure Reference

> Complete directory map for AI agents and developers.
> Lists all folders with max 2 key files per folder for quick lookup.
> Last Updated: July 2026

---

## Solution Overview

The solution uses **Clean Architecture** with CQRS (MediatR). There are **two naming prefixes** during the Explore-to-Event transition:

- **Explore.\*** - Main application projects (API, Domain, Application, Persistence, Blazor, Infrastructure) (historical naming from when the project was "Explore", were not renamed to "Event" yet)
- **Event.\*** - Test projects plus newer shared/runtime projects such as shared BFF hosting and the control-plane UI.

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
Event.Web.BffHosting    (shared BFF auth/proxy/header/admin-host primitives)
Event.ControlPlane.Client (shared host-neutral control-plane RCL)
Explore.Blazor          (public Blazor BFF host; can embed control-plane shell on admin hosts)
Explore.Blazor.Client   (public interactive client, calls API via BFF)
Event.ControlPlane.Blazor (optional separate Interactive Server-only control-plane BFF)
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
├── OutboxMessage.cs              — General-purpose transactional outbox entity (UUID v7, JSONB payload)
├── OutboxMessageStatus.cs        — Enum: Pending, Processing, Completed, Failed, DeadLettered
├── TenantFooterLinkGroup.cs      — Footer link group entity (per-tenant, ordered)
├── TenantFooterLink.cs           — Footer link entity (belongs to group, ordered)
├── Federation/                   — ATProto federation entities
│   └── PdsSyncOutbox.cs          — Outbox pattern for PDS synchronization
└── Modules/                      — Module governance entities
    ├── ModuleDefinition.cs       — Module definitions (Core, Islamic, Tech)
    └── TenantCapability.cs       — Per-tenant module enablement
```

**Entity categories:**
- **Main entities** (Guid PK): Event, Organization, Actor, User, Tenant, Location, StorageObject, Category, Tag
- **Lookup tables** (int PK): EventType, EventStatus, EventFormat, AudienceAge, AudienceGender, Madhab, Language, VisibilityType, RegistrationMode, Role, OrganizationPosition
- **Link/junction tables** (composite PK): EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker, TagTypeTags, OrganizationMember, RolePermission
- **Aspect tables** (1:1 optional): EventIslamicAspect, EventTechAspect
- **Admin/onboarding**: InstanceAdministrator, InstanceBootstrapState, TenantUser, TenantUserRoleGrant, TenantOnboardingState

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
│   ├── Footer/                    — Footer link groups, links, settings, governance
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
│   │   ├── IOutboxRepository.cs    — Outbox message retrieval and status management
│   │   └── [50+ more interfaces] — One interface per entity/aggregate
│   ├── Outbox/                    — Outbox pattern contracts
│   │   └── IOutboxMessageDispatcher.cs — Interface for dispatching outbox messages
│   ├── Infrastructure/           — Infrastructure service interfaces
│   │   ├── ITenantContext.cs     — Multi-tenant context resolution
│   │   ├── ICurrentUserService.cs— Current authenticated user info
│   │   ├── IObjectStorageService.cs — S3-compatible storage operations
│   │   ├── IS3ConfigResolver.cs  — S3 config from cascading settings (per-tenant)
│   │   ├── ISmtpConfigResolver.cs— SMTP config from cascading settings (per-tenant)
│   │   └── ISettingsResolver.cs  — Governance settings resolution
│   ├── Identity/                 — User identity interfaces
│   │   └── IUserContext.cs       — User context abstraction
│   ├── Services/                 — Application service interfaces
│   │   └── IEventActorResolver.cs — Actor resolution contract (org/group/personal with permissions)
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
├── Services/                     — Application-layer services
│   ├── EventActorResolver.cs    — Resolves event actor (org/group/personal) with permission checks
│   └── SlugGenerator.cs         — Static URL slug generation utility
├── Profiles/                     — AutoMapper profiles (10 domain-specific files)
│   ├── EventMappingProfile.cs   — Event, EventSeries, EventDay, EventAgendaItem, EventTags, EventCategories, Aspects
│   ├── OrganizationMappingProfile.cs — Organization, Group, Members, ApprovalStatus, Reviews
│   ├── CustomPropertyMappingProfile.cs — All custom property definitions, templates, options, values
│   └── [7 more profiles]        — Tenant, EventSession, User, Registration, ActorFederation, Lookup, Notification
├── Hateoas/                      — HAL resource models
│   ├── HalResource.cs            — Base HAL envelope
│   ├── HalLink.cs                — Link representation
│   └── LinkDefinition.cs         — Rich link definition with permission metadata
├── Specifications/               — Query specification pattern
│   ├── IQuerySpecification.cs    — Base specification interface (composes filters + sorts)
│   ├── IFilterSpecification.cs   — Individual filter interface (Expression<Func<T,bool>>)
│   ├── ISortSpecification.cs     — Sort specification interface
│   ├── EventQuerySpecification.cs — Immutable fluent builder for event queries
│   ├── EventFilter.cs            — Core event field filters
│   ├── EventSort.cs              — Event sort specifications
│   ├── EventSubqueryFilter.cs    — Junction table + JSONB filters
│   ├── IslamicAspectFilter.cs    — Module-conditional Islamic filters
│   ├── TechAspectFilter.cs       — Module-conditional Tech filters
│   └── AspectPresenceFilter.cs   — HasIslamicAspect/HasTechAspect presence filters
├── Behaviors/                    — MediatR pipeline behaviors
│   ├── PerformanceBehavior.cs    — Logs requests >500ms as warnings
│   └── AuthorizationBehavior.cs  — Resource-level auth via IAuthorizedRequest/[AuthorizeResource]
├── Authorization/                — Authorization contracts and attributes
│   ├── IAuthorizedRequest.cs     — Interface for commands requiring authorization
│   ├── AuthorizeResourceAttribute.cs — Declarative resource-level auth attribute
│   └── ISecureRequest.cs         — Dynamic resource context for permission checks
├── Models/                       — Infrastructure models
│   ├── EmailMessage.cs           — Rich email message DTO (To, CC, BCC, HTML, attachments)
│   ├── EmailAttachment.cs        — Email attachment with inline image support
│   ├── EmailResult.cs            — Email send result with success/failure and duration
│   └── SmtpConfiguration.cs      — SMTP config POCO resolved from cascading settings
└── Requests/                     — Shared request types
    └── PaginationParams.cs       — Page number/size parameters
```

**CQRS pattern note:** Most features follow `Handlers/{Commands,Queries}` + `Requests/{Commands,Queries}`. One exception: `OrganizationReviews` uses `Commands/` and `Queries/` directly (flattened structure).

---

### Explore.Persistence/ — EF Core, Database, Repositories

Data access layer. Implements repository interfaces from Application.

```
Explore.Persistence/
├── ExploreDbContext.cs            — Main DbContext (partial class — constructor, OnModelCreating)
├── ExploreDbContext.DbSets.cs    — 170 DbSet property declarations (partial)
├── ExploreDbContext.QueryFilters.cs — 48 named query filter registrations (partial)
├── ExploreDbContext.SaveChanges.cs — SaveChangesAsync override (audit, soft delete, concurrency) (partial)
├── PersistenceServicesRegistration.cs — DI registration for all repositories
├── Repositories/                  — Repository implementations
│   ├── GenericRepository.cs       — Base CRUD with soft delete awareness
│   ├── EventRepository.cs         — Event-specific queries with eager loading
│   ├── OutboxRepository.cs        — Outbox message batch retrieval, optimistic locking, dead-letter
│   ├── TenantFooterLinkGroupRepository.cs — Footer link group CRUD
│   ├── TenantFooterLinkRepository.cs      — Footer link CRUD
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
├── Extensions/                    — IQueryable extension methods for Include chains
│   ├── EventQueryExtensions.cs   — IncludeStandardDetails() for Event queries (15 includes)
│   ├── EventSessionQueryExtensions.cs — IncludeStandardDetails() for EventSession queries
│   └── NotificationQueryExtensions.cs — IncludeStandardDetails() for Notification queries
├── Projections/                   — Shared projection infrastructure
│   └── ProjectionInfrastructure.cs — Advisory locks, FNV-1a hash, batch chunking (used by both projection updaters)
└── ValueGenerators/               — Custom EF Core value generators
```

---

### Explore.API/ — ASP.NET Core Web API

API layer. Wires everything together via DI. Contains controllers, middleware, HATEOAS, and configuration.

```
Explore.API/
├── Program.cs                     — Application entry point, DI registration, middleware pipeline (slimmed via extensions)
├── appsettings.json               — Configuration (connection strings, auth, storage, rate limiting, timeouts)
├── Controllers/                   — API controllers (one per entity/aggregate)
│   ├── ExploreControllerBase.cs   — Abstract base with IUserContext (CurrentUserId, RequiredUserId)
│   ├── EventController.cs         — Event CRUD with specification pattern filtering via [FromQuery] EventFilterRequest
│   ├── OrganizationController.cs  — Organization endpoints
│   ├── InstanceOnboardingController.cs — Instance admin bootstrap
│   ├── TenantOnboardingController.cs   — Tenant setup wizard
│   ├── ControlPlaneController.cs — Multi-tenant instance console API (`[RequireMultiTenant]`)
│   ├── FooterController.cs         — Footer link groups, links, settings, governance (11 endpoints)
│   └── [40+ more controllers]     — Inherit ExploreControllerBase; GET=AllowAnonymous, POST/PUT/DELETE=Authorize
├── Models/                        — API transport models (not DTOs)
│   └── EventFilterRequest.cs      — 42-property filter model for [FromQuery] binding
├── Services/                      — API-layer services
│   └── TenantContext.cs           — Legacy/request tenant context bridge; middleware is the tenant-resolution authority
├── Middleware/                     — HTTP pipeline middleware
│   ├── SecurityHeadersMiddleware.cs   — X-Content-Type-Options, X-Frame-Options, CSP, etc.
│   ├── ApiTenantResolutionMiddleware.cs — Tenant resolution (BFF hint → admin-host exclusion → custom domain → subdomain)
│   ├── ApiTenantPostAuthenticationMiddleware.cs — Final API-key tenant binding after auth
│   ├── CorrelationIdMiddleware.cs     — X-Correlation-ID / X-Request-ID propagation to Serilog
│   ├── ETagMiddleware.cs              — SHA256-based weak ETags, 304 Not Modified
│   ├── RequestLoggingMiddleware.cs    — Structured logging: method, path, status, duration, userId
│   └── PreferHeaderMiddleware.cs      — RFC 7240 Prefer header (return=minimal strips _links)
├── Extensions/                    — DI and configuration extensions (extracted from Program.cs)
│   ├── AuthenticationExtensions.cs    — Multi-auth (JWT Bearer + API Key) with PolicyScheme dispatch
│   ├── CachingExtensions.cs           — OutputCache (5 policies) + HybridCache (L1+L2) configuration
│   ├── CorsExtensions.cs             — 5 CORS policies (InternalApp, ExternalApp, InternalWeb, ExternalWeb, Dev)
│   ├── RateLimitingExtensions.cs      — 7-tier rate limiting (global, authenticated, write, public ingestion, setup secret, analytics relay, AI assistant)
│   ├── RequestTimeoutExtensions.cs    — 3-tier timeouts (default, lookup, complex)
│   ├── ApiVersioningExtensions.cs     — Media-type versioning (Accept header v parameter)
│   ├── HateoasServiceExtensions.cs    — HATEOAS DI registration (assemblers, policies, evaluator)
│   ├── ExceptionHandlingExtensions.cs — Chained IExceptionHandler registration
│   ├── ServiceCollectionExtensions.cs — Service registration helpers
│   └── ConfigurationExtensions.cs     — Configuration binding helpers
├── ExceptionHandling/             — Exception handler chain
│   ├── ValidationExceptionHandler.cs  — FluentValidation → 400 with errors dict
│   └── GlobalExceptionHandler.cs      — Maps known exceptions to HTTP status codes
├── Filters/                       — Action filters
│   ├── BlockInSingleTenantAttribute.cs   — Returns 404 in single-tenant mode (hides endpoint)
│   ├── RequireMultiTenantAttribute.cs    — Returns 403 in single-tenant mode (informs client)
│   └── SetupSecretRequiredAttribute.cs   — Gates endpoints behind X-Setup-Secret header
├── Hateoas/                       — HATEOAS link generation infrastructure
│   ├── ResourceAssemblerBase.cs       — Base class with batch authorization evaluation
│   ├── HateoasLinkGenerator.cs        — Resolves URLs from named routes
│   ├── HateoasAuthorizationEvaluator.cs — Batch evaluates link permissions (fail-closed)
│   ├── HateoasConstants.cs            — Link relation names, media types
│   ├── RouteNames.cs                  — 100+ named route constants
│   ├── Assemblers/                    — 19 entity-specific assemblers (EventAssembler, OrganizationAssembler, etc.)
│   └── Policies/                      — 19 entity-specific link policies (EventLinkPolicy, etc.)
├── OpenApi/                       — Scalar/OpenAPI customization and build-time contract transformers
│   ├── HalDtoSchemaTransformer.cs — Native OpenAPI HAL schema shaping
│   ├── EndpointClassificationTransformer.cs — Emits endpoint tenant-mode/admin metadata
│   └── HalSchemaFilter.cs         — Swashbuckle transition HAL schema shaping
├── BackgroundServices/            — Hosted background workers
│   ├── OutboxProcessor.cs         — Polls outbox_messages, dispatches events, retry + dead-letter
│   └── PdsSyncWorker.cs           — ATProto PDS synchronization (outbox pattern, exponential backoff)
├── Static/                        — Static file serving configuration
├── schemas/openapi.json           — Generated OpenAPI specification
└── Properties/
    └── launchSettings.json        — Development server URLs and profiles
```

---

### Explore.Blazor/ — Public Blazor BFF Host

Server-side BFF host for the public Blazor application. Handles SSR/interactive rendering, authentication, proxying to API, tenant host resolution, and configured admin-host shell selection.

```
Explore.Blazor/
├── Program.cs                     — Server-side host configuration, OIDC auth setup, BFF proxy
├── Components/
│   ├── App.razor                  — Root component (HTML head, body, Blazor script tags)
│   └── Routes.razor               — Router configuration with render modes
│   └── ControlPlane/              — Embedded admin-host shell backed by Event.ControlPlane.Client
├── Extensions/
│   └── ConfigurationExtension.cs  — Configuration helpers for Blazor Server
├── Services/
│   └── CircuitAccessTokenService.cs — Captures OIDC tokens for circuit-lifetime use
└── wwwroot/                       — Static assets (CSS, JS, images)
```

---

### Explore.Blazor.Client/ — Public Blazor Client

Public interactive client. Contains pages, components, route guards, generated/HAL API client boundary, and public/tenant/legacy instance-admin surfaces.

```
Explore.Blazor.Client/
├── Program.cs                     — WASM host builder, service registration, HttpClient setup
├── Routes.razor                   — Route map, including embedded Event.ControlPlane.Client routes
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
│   │   ├── Instance/              — Instance-level settings and multi-tenant console host pages
│   │   └── Tenant/                — Tenant-level admin settings
│   ├── Auth/                      — Authentication pages
│   ├── Landing/                   — Public landing page
│   ├── Onboarding/                — Instance/tenant onboarding wizard pages
│   └── User/                      — User profile and settings
├── Services/                      — API proxy services (one per domain area)
│   ├── EventService.cs            — Event API calls
│   ├── OrganizationService.cs     — Organization API calls
│   ├── FooterAdminService.cs      — Footer link groups, links, settings admin API calls
│   ├── PublicExperienceService.cs — Public-facing discovery + footer config API calls
│   ├── BffClient.cs               — BFF HTTP client with XSRF token handling
│   ├── AuthStateService.cs        — Centralized authentication state
│   ├── LookupCacheService.cs      — Client-side cache for lookup table data
│   ├── ImageStorageService.cs     — S3 presigned URL upload handling
│   ├── DialogOptionsFactory.cs    — Static dialog preset factory (Small, Medium, Confirmation, Editor)
│   ├── Accessibility/             — Accessibility service implementations
│   │   ├── AccessibilityAnnouncerService.cs — ARIA live region announcements via JS interop
│   │   └── AccessibilityFocusService.cs     — Focus management via JS interop
│   └── AppearanceThemeService.cs  — WCAG AA compliant color palette management
├── Components/                    — Reusable UI components
│   ├── Common/                    — MudBlazor wrapper components (consistent defaults)
│   │   ├── AppButton.razor/.css   — Filled/Primary/Elevation=0 button wrapper
│   │   ├── AppCard.razor/.css     — Elevation=0/border card wrapper
│   │   ├── AppTextField.razor/.css— Outlined text field wrapper (generic <T>)
│   │   ├── AppIconButton.razor/.css — Icon button wrapper
│   │   └── AppDialogShell.razor/.css — Dialog shell (header/body/actions BEM)
│   ├── ImageUpload.razor          — Image upload with S3 presigned URLs
│   ├── S3Image.razor              — Image display from S3 storage
│   ├── ReviewDialog.razor         — Generic review/rating dialog
│   └── Loading.razor              — Loading spinner component
├── Layout/                        — Layout components
│   ├── MainLayout.razor/.cs       — Main page layout (skip-link, landmarks, ARIA live regions)
│   ├── NavMenu.razor/.cs          — Navigation menu component
│   └── Footer.razor               — Site footer (template-based, tenant-configurable)
├── Shared/                        — Shared editor components
│   └── AppearanceEditor.razor/.css — Appearance editor (color picker, effects, image URL)
├── Clients/                       — API client boundary
│   ├── EventApiClient.cs          — Hand-written interface/extensions around generated client behavior
│   └── EventApiClient.g.cs        — NSwag-generated client code from OpenAPI spec
├── Models/                        — Client-side models
│   ├── UserInfo.cs                — Deserialized user info from BFF
│   ├── TenantContext.cs           — Client-side tenant state
│   └── Responses/
│       ├── BaseCommandResponse.cs — Client-side mirror of API response
│       └── ServiceResult.cs       — Service operation result wrapper
├── Contracts/Services/Accessibility/ — Accessibility service interfaces
│   ├── IAccessibilityAnnouncerService.cs — ARIA live region announcement contract
│   └── IAccessibilityFocusService.cs     — Focus management contract
├── Helpers/                       — UI helper utilities
│   ├── AppearanceStyleBuilder.cs  — Builds inline CSS for actor/event appearance (overlays, blur, gradient)
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
│       ├── AdminRouteGuard.cs     — Requires admin role
│       └── MultiTenantControlPlaneRouteGuard.cs — Suppresses Instance Console routes outside multi-tenant BFF status
└── wwwroot/                       — Static web assets
```

### Event.Web.BffHosting/ — Shared BFF Hosting Primitives

Shared authentication, YARP, header-sanitization, host-profile, admin-host classification, and control-plane authorization primitives used by `Explore.Blazor` and `Event.ControlPlane.Blazor`.

### Event.ControlPlane.Client/ — Shared Control-Plane RCL

Host-neutral Razor Class Library for Instance Console routes, contracts, local design primitives, and fail-closed service interfaces. It must not depend on API/Application/Domain/Infrastructure/Persistence or browser token storage.

### Event.ControlPlane.Blazor/ — Optional Separate Control-Plane BFF

Interactive Server-only Blazor BFF using the `EventBffHostProfile.ControlPlane` profile, dedicated Keycloak confidential client, dedicated cookie, and `Event.ControlPlane.Client` UI surface.

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
│   ├── SmtpEmailService.cs       — MailKit SMTP email sender with Polly retry
│   ├── SmtpConfigResolver.cs     — Resolves SMTP config from cascading settings per tenant
│   └── EmailResiliencePipelines.cs — Polly v8 retry pipeline for transient SMTP failures
├── Storage/
│   └── S3ConfigResolver.cs       — Resolves S3 config from cascading settings per tenant
├── Services/
│   ├── CurrentUserService.cs     — Current user ID extraction with claim fallback
│   ├── ObjectStorageService.cs   — S3-compatible object storage (per-tenant via IS3ConfigResolver)
│   ├── ModuleService.cs          — Module governance checks
│   ├── SettingsResolver.cs       — SystemSetting/TenantSetting resolution
│   ├── FallbackAuthorizationService.cs — Main dispatch + safe-mode latch (partial class)
│   ├── FallbackAuthorizationService.Batch.cs — Batch auth evaluation with authority profile (partial)
│   ├── FallbackAuthorizationService.Evaluators.cs — 14 async resource-family evaluators (partial)
│   ├── TenantPolicySettingService.cs — Constants, helpers (partial class)
│   ├── TenantPolicySettingService.Read.cs — Read effective tenant settings (partial)
│   └── TenantPolicySettingService.Apply.cs — Apply tenant settings with notifications (partial)
├── Localization/
│   └── Resilience/
│       └── TmsResiliencePipelineConfigurator.cs — Shared Polly v8 pipeline for TMS providers
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
├── NamingConventionTests.cs       — Enforces naming patterns across codebase
├── AccessibilityConventionTests.cs — Page shell, landmark, heading, CSS direction conventions
└── AuthorizationParityTests.cs    — Resource kind ↔ Cerbos policy parity checks

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
assets/                            — Documentation assets (images, gif...)
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
| Wrapper components | `Explore.Blazor.Client/Components/Common/App*.razor` |
| Accessibility services | `Explore.Blazor.Client/Services/Accessibility/` |
| Accessibility contracts | `Explore.Blazor.Client/Contracts/Services/Accessibility/` |
| Appearance styling | `Explore.Blazor.Client/Helpers/AppearanceStyleBuilder.cs` |
| Footer management | `Explore.Blazor.Client/Services/FooterAdminService.cs` + `Explore.API/Controllers/FooterController.cs` |
| Outbox processing | `Explore.API/BackgroundServices/OutboxProcessor.cs` + `Explore.Domain/OutboxMessage.cs` |
| Dialog presets | `Explore.Blazor.Client/Services/DialogOptionsFactory.cs` |
| CSS tokens/layers | `Explore.Blazor/wwwroot/css/tokens.css` + `layers.css` |
