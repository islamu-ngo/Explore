<!-- ABOUTME: Current Senior CTO handoff for the event-location privacy workstream. -->
<!-- ABOUTME: Separates verified runtime reality from target architecture, release gates, and self-hosting obligations. -->

# Event Location Privacy — Context

Last Updated: 2026-08-18 Europe/Brussels

## Senior CTO Executive Verdict

The first-class `EventLocation` disclosure architecture is fully established and operational across Domain, Application, Persistence, Infrastructure, and API layers. The API surface is aligned with repository-wide architectural standards:
- Dedicated, capability-focused `EventLocationController` with `[ApiVersion("0.1")]`, `[EndpointClassification]`, `[PrivateNoStore]`, and RFC 7807 `ProblemDetails` error handling with typed problem descriptors (`EventLocationNotFoundProblem`, `DisclosureValidationProblem`, `RemediationValidationProblem`).
- Purpose-specific route split: anonymous public reads (`GET /api/events/{eventId}/locations`), authenticated attendee reads (`GET /api/events/{eventId}/locations/my-access`), management detail reads (`GET /api/events/{eventId}/locations/{eventLocationId}/management`), review queue reads (`GET /api/events/{eventId}/locations/review`), grouped disclosure updates (`PATCH /api/events/{eventId}/locations/{eventLocationId}/disclosure`), and explicit remediation confirmation (`POST /api/events/{eventId}/locations/{eventLocationId}/remediation/confirm`).
- Additive OpenAPI / NSwag generation (`ELP-420A`) is complete: `HalOpenApiSchemaCatalog.cs` registers `EventLocationManagementDto` schemas, and `EventApiClient.g.cs` exposes all six purpose-specific methods and strongly-typed models.
- The platform privacy erasure authority is canonicalized in `docs/PRIVACY_ERASURE.md` supporting three storage topologies (`EmbeddedSqlite`, `CoLocated`, `ExternalDatabase`); this workstream owns only the compiled, typed `IUserLocationPrivacyErasureRepository` adapter and domain-level tombstoning / review-flagging / outbox correction.

The workstream's backend and API foundation is verified. Next focus is Phase 7 Blazor UI adoption (`ELP-600` through `ELP-660`), privacy observability metrics (`ELP-540`), and the final contraction wave (`ELP-230C`, `ELP-430`, `ELP-420B`) after UI and consumer migration.

**Core architecture decision:** Approved and implemented across backend and API layers.

**Workstream decision:** Active. 40 of 59 tasks verified complete. Current focus is Blazor UI adoption and operations/observability closure.

**Release posture:** Blocked pending Phase 7 (Blazor UX adoption), Phase 8 (outbound audit & doc synchronization), and Phase 9 (final contraction & QA).

**Implementation posture:** Execute Blazor UI adoption using generated `EventApiClient` and HAL affordance gating (`edit`, `remediate-location`).

**Complexity:** XL / high risk because this spans contextual privacy, tenant isolation, authorization, migrations, OpenAPI/NSwag, Blazor, federation, transactional correction delivery, and cache convergence.

## SESSION PROGRESS (2026-08-18 Europe/Brussels)

### ✅ COMPLETED

- **API & Controller Modernization (`ELP-405`)**: Dedicated, capability-partitioned `EventLocationController` exposes public, attendee, management, review queue, PATCH disclosure, and POST remediation confirmation routes with RFC 7807 ProblemDetails and typed problem descriptors.
- **HAL Affordance Gating (`ELP-410`)**: `EventLocationLinkPolicy` and `EventLocationResourceAssembler` emit server-authorized `edit` (PATCH disclosure) and `remediate-location` (POST remediation confirm) links without client-side role inspection.
- **OpenAPI & Generated Client (`ELP-420A`)**: `HalOpenApiSchemaCatalog.cs` registers `EventLocationManagementDto` resources; `EventApiClient.g.cs` exposes strongly-typed API client methods and DTOs.
- **Batched Disclosure Service (`ELP-315`)**: `EventLocationDisclosureService` executes bounded queries (at most 1 query per surface, 1 batched management authorization) and evaluates pure `EventLocationDisclosureEvaluator` logic.
- **Backend Projections (`ELP-320`)**: Public session, session group, program summary, and agenda query handlers project through `IEventLocationDisclosureService` batch resolution; AutoMapper profiles ignore physical fields.
- **Management Authorization & Exact-Read Audit (`ELP-350`)**: `EventLocationManagementAuthorizationService` evaluates `event:view-management` in batch; `EventLocationExactReadAuditService` appends PII-free audit logs before returning decisions.
- **Policy Concurrency & Append-Only Audit (`ELP-360`)**: `UpdateEventLocationPolicyCommandHandler` enforces optimistic concurrency (`ExpectedConcurrencyStamp`, `ExpectedPolicyVersion`), appends PII-free audit records, and invalidates hybrid cache tags post-commit.
- **Calendar Route Split (`ELP-440`)**: `GetEventCalendarExportRequestHandler` and `GetAttendeeCalendarExportRequestHandler` separate anonymous public ICS from authenticated attendee ICS (`private, no-store`, `X-Calendar-Retention-Warning`).
- **Platform Erasure Adapter (`ELP-515`)**: `IUserLocationPrivacyErasureRepository` and `LocationRepository.GetOwnedPrivateHomesForGlobalErasureAsync` tombstone Home PII/rooms, flag affected `EventLocation`s with `NeedsPrivacyReview`, and emit correction outbox intents.
- **Correction Dispatch & Dead-Letter Recovery (`ELP-520`)**: `LocationPrivacyCorrectionDispatcher` handles `LocationPiiErased`, `LocationPrivacyCorrectionRequested`, `location.privacy.corrected` events, invalidates cache tags, and drives ATProto correction planning.
- **Remediation Workflow (`ELP-530`)**: `ConfirmEventLocationRemediationCommand`, `GetEventLocationReviewQueueRequestHandler`, and `EventLocationController` endpoints clear privacy reviews only on verified active physical venues or explicit TBA.
- **MCP/AI & Discovery Boundaries (`ELP-720`, `ELP-730`)**: Public MCP adapters consume sanitized projections gated by `IAiContextGateway`; Home Discovery remains strictly bounded to coarse areas without raw PII.

### 🟡 IN PROGRESS

- **Phase 7: Blazor UI Adoption (`ELP-600` - `ELP-660`)**: Migrating Blazor services (`LocationService`, `AdminService`, `LookupCacheService`) to consume generated purpose-specific contracts from `EventApiClient.g.cs`; implementing HAL-gated `EventLocation` management editor, public/attendee disclosure cards, and remediation dashboard.
- **Observability Metrics (`ELP-540`)**: Implementing privacy metrics and health counters for unclassified locations, review queue length, and correction retries.

### ⏭️ NEXT

1. Complete `ELP-600` Blazor client service migration to purpose-specific `EventApiClient` methods.
2. Implement `ELP-610` EventLocation management editor component with HAL link gating (`edit`, `remediate-location`).
3. Implement `ELP-630` / `ELP-650` public and attendee disclosure states in `EventDetail` and session components.
4. Implement `ELP-640` manager privacy review queue dashboard.
5. Add `ELP-540` Prometheus metrics and health check counters for location privacy.
6. Execute Phase 8 outbound audit (`ELP-700`, `ELP-715`, `ELP-740`) and doc synchronization.
7. Execute final contraction wave: `ELP-230C` (validate zero-gap data & contract schema) → `ELP-430` (remove obsolete generic Location endpoints) → `ELP-420B` (regenerate final contracted client) → `ELP-800`..`840` (final QA).

### ⚠️ BLOCKERS

- **Release activation blocked on UI adoption.** Blazor UI still relies partly on generic Location DTOs and Stage A redaction helpers; adoption of purpose-specific `EventLocationPublicDto` / `EventLocationAttendeeDto` / `EventLocationManagementDto` must land before schema contraction.
- **Contraction blocked on consumer migration.** Obsolete generic Location detail endpoints cannot be contracted (`ELP-430`) and schema references cannot be tightened (`ELP-230C`) until all Blazor components and external consumers migrate.
- **External platform dependency resolved.** Platform erasure architecture is documented in `docs/PRIVACY_ERASURE.md`; ELP owns only the compiled `IUserLocationPrivacyErasureRepository` adapter.

## Quick Resume

1. Read this file and `event-location-privacy-tasks.md` first.
2. Read plan Sections 2, 8–16 for architectural rules and wave dependencies.
3. Review `src/Explore.API/Controllers/EventLocationController.cs` and `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` for active API contracts.
4. Focus execution on Phase 7: Blazor UI adoption (`ELP-600`..`ELP-660`).
5. Update `tasks.md` immediately when a substantial task passes acceptance; update this context after a decision, blocker, failed validation, phase completion, or handoff.

## Current Repository Reality

| Concern | Verified current state | CTO consequence |
|---|---|---|
| Event-local authority | `src/Explore.Domain/EventLocation.cs` exists and owns event-scoped disclosure state, explicit TBA, policy version, concurrency, review state, and soft deletion. | Keep `EventLocation` as the only event-local disclosure authority; do not restore a side-table policy model. |
| Disclosure evaluation | `EventLocationDisclosureEvaluator` and `EventLocationDisclosureService.ResolveManyAsync` are implemented and verified. | All public/attendee/management projections route through the service with bounded query and authorization budgets. |
| API contract | `src/Explore.API/Controllers/EventLocationController.cs` exposes `GetPublic`, `GetMyAccess`, `GetManagement`, `GetReviewQueue`, `UpdateDisclosure` (PATCH), and `ConfirmRemediation` (POST). | API contracts are additive, standardized with RFC 7807 ProblemDetails and typed descriptors, and fully covered by integration tests. |
| HAL authorization | `EventLocationLinkPolicy` and `EventLocationResourceAssembler` emit server-authorized `edit` and `remediate-location` links. | Blazor UI must gate mutation affordances strictly from `_links`, never local role or claim checks. |
| OpenAPI & Generated Client | `HalOpenApiSchemaCatalog.cs` registers `EventLocationManagementDto` and HAL wrappers; `EventApiClient.g.cs` contains all 6 strongly-typed methods. | Blazor services consume `EventApiClient.g.cs` generated methods directly without handwritten wrappers. |
| Correction delivery | `LocationPrivacyCorrectionDispatcher` handles `LocationPiiErased`, `LocationPrivacyCorrectionRequested`, `location.privacy.corrected` events with outbox retry/dead-letter support. | Outbox processor guarantees idempotent cache invalidation and ATProto correction planning. |
| Platform-erasure adapter | `IUserLocationPrivacyErasureRepository` and `LocationRepository.GetOwnedPrivateHomesForGlobalErasureAsync` are implemented and verified in PostgreSQL. | Platform authority orchestration remains external (`docs/PRIVACY_ERASURE.md`); ELP provides only the typed disposition/correction adapter. |
| Calendar separation | `GetEventCalendarExportRequestHandler` and `GetAttendeeCalendarExportRequestHandler` provide purpose-separated ICS feeds with retention warnings. | Public ICS is public-only; attendee ICS is authenticated, `private, no-store`, and registration-gated. |
| Migration baseline | Root `20260720162943_init` contains the current EventLocation schema lane with lookup seeds, XOR constraints, and audit tables. | Expand and Backfill stages are verified; Contract stage (`ELP-230C`) is deferred to Wave 18 after consumer migration. |
| Build baseline | Solution builds cleanly with 0 errors across 26 projects. | Pre-existing package warnings are tracked separately; clean architecture and naming tests pass. |

## Workstream Ownership Boundaries

| Workstream | Owns | Must not own |
|---|---|---|
| Event Location Privacy | `EventLocation` lifecycle, field policy, registration entitlement, disclosure service, routes/HAL, location projections, policy audit, correction intents, UI adoption, and location-specific migration constraints. | A second platform-wide account-erasure saga, generic provider cleanup framework, or separate authority topology. |
| Platform Privacy Erasure Authority | User fence/receipt/status, complete PII inventory/disposition, platform transaction, provider work, topology (`EmbeddedSqlite`/`CoLocated`/`ExternalDatabase`), replay, retention, restore, and completion semantics (see `docs/PRIVACY_ERASURE.md`). | Event-specific disclosure decisions or browser affordance logic. |
| Home Discovery | Coarse governed discovery areas and any future separately consented spatial projection. | Reuse of exact `LocationPii`, `ShowCoordinates` as indexing consent, or browser-downloadable exact catalogs. |
| AI/MCP Disclosure | `IAiContextGateway` sensitivity ceiling and tool/resource contracts. | Bypassing EventLocation disclosure or treating sanitized output as authorization authority. |

## Locked Architecture Decisions

- Physical address and coordinates remain in `LocationPii`; `LocationKind` describes a place but never grants disclosure.
- `LocationPrivacyState` distinguishes `NOT_PROVIDED`, `ACTIVE`, and irreversible `ERASED`; an erased `Location` never accepts PII again.
- Public, attendee, and management are separate purposes and contracts. Public output is principal-invariant; attendee and management output is authenticated and `private, no-store`.
- Public contracts expose `EventLocationId`, not unrestricted physical `LocationId`.
- Registration authority comes from exact Event/Day/SessionSelection intent coverage and current lifecycle, never row existence.
- Server UTC controls delayed reveal. Client clocks and local claims do not influence disclosure.
- Tenant filters remain enabled for EventLocation reads/writes. The external platform adapter must supply persisted subject/tenant scope and fail closed on mismatch.
- Policy/audit/correction payloads contain bounded identifiers, versions, codes, and timestamps only; no address, coordinates, room text, provider response, or free-text error.
- Policy mutation and correction intent persist in one transaction; cache invalidation and external processing occur after commit.
- Every correction delivery and reconciliation attempt opens a fresh dependency scope, reloads persisted tenant/EventLocation ownership, and fails closed when ownership is missing or mismatched. Caller-supplied tenant or aggregate identifiers are routing hints, never authority.
- Sensitive responses are `no-store` or keyed by tenant, purpose, principal/entitlement, and policy version. Invalidation failure must not permit stale exact disclosure; it creates retryable convergence work, readiness degradation, and an operator alert.
- External correction calls never execute inside EventLocation policy transactions or migrations.
- Disclosure update uses route-ID grouped `PATCH /api/events/{eventId}/locations/{eventLocationId}/disclosure` with `ExpectedConcurrencyStamp` and `ExpectedPolicyVersion`.
- Breaking pre-v1 contracts may be deleted once consumer migration and operator upgrade guidance are complete; no compatibility shims are required.

## Control and Data Flow

### Disclosure

1. A server-owned command attaches or resolves an `EventLocation` with fail-closed defaults.
2. A query builds purpose-specific requests keyed by `EventLocationId`.
3. `EventLocationDisclosureService` performs bounded tenant-scoped reads and one purpose-appropriate authority batch.
4. `EventLocationDisclosureEvaluator` applies lifecycle, purpose ceiling, governance, authorization/registration coverage, server-time reveal, field policy, and Home redaction.
5. API/MCP/federation/calendar consumers serialize only the resulting purpose-specific DTO.

### Policy mutation and correction

1. API authorization and concurrency tokens guard the command (`ExpectedConcurrencyStamp`, `ExpectedPolicyVersion`).
2. `UpdateEventLocationPolicyCommandHandler` updates the aggregate, appends a PII-free audit, and writes the correction outbox intent transactionally.
3. Post-commit invalidation removes tenant/event/EventLocation cache tags (`CacheTags.EventLocations`, `CacheTags.EventLocationsByEvent(eventId)`); sensitive reads use `no-store` or policy-versioned keys so failed eviction cannot expose a superseded exact location.
4. `LocationPrivacyCorrectionDispatcher` creates a fresh scope, rebinds tenant and EventLocation ownership from persistence, fails closed on mismatch, performs idempotent correction, retries bounded failures, and retains dead-letter evidence for operator reconciliation.

### Platform-erasure adapter

1. The external platform workflow supplies a typed User intent plus persisted subject/tenant scope (see `docs/PRIVACY_ERASURE.md`).
2. The `IUserLocationPrivacyErasureRepository` adapter reloads ownership, fails closed on mismatch, tombstones owned Home/room labels through domain invariants, and marks affected associations `NeedsPrivacyReview`.
3. It emits stable, PII-free EventLocation correction intents with no authority, receipt, provider, replay, or topology logic.
4. Event Location owns adapter integration, correction delivery, cache convergence, and remediation tests. The authority workstream owns the surrounding transaction and platform outcome.

## Enterprise Self-Hosting Contract

| Area | Required target behavior |
|---|---|
| Required services | PostgreSQL remains required. No broker, Redis, PostGIS, or external policy service becomes mandatory solely for location privacy. |
| Configuration | Keep EventLocation field/governance settings explicit, tenant-restrictive, auditable, and fail closed. No authority connection or secret is owned here. |
| Startup | Validate EventLocation schema/contract compatibility and keep exact disclosure disabled until the selected migration/consumer gate is satisfied. |
| Health | Expose bounded migration state, correction backlog/dead letters, cache convergence, and review backlog without identifiers or location text. |
| Upgrade | Use the approved pre-v1 EventLocation migration/contract sequence; breaking contracts are allowed after consumers move, but silent data loss or renewed anonymous exact disclosure is not. |
| Rollback | Before destructive contraction, additive rollback may be possible. After irreversible Location-state or contract activation, use forward repair and never resurrect exact PII. |
| Scale | Keep bounded batch sizes, no N+1 policy calls, and no per-row external calls. Introduce a saga or new infrastructure only from measured limits and a separate approved design. |
| Observability | Use closed-vocabulary counters and failure categories. No user IDs, tenant IDs, location IDs, addresses, coordinates, room text, endpoints, secrets, or exception text in metrics. |

## Release Gates

| Gate | Status | Evidence / Notes |
|---|---|---|
| G1 — Ownership convergence | ✅ Complete | ELP plan/context/tasks contain only EventLocation work and the typed external adapter boundary; platform erasure is external (`docs/PRIVACY_ERASURE.md`). |
| G2 — Current migration integrity | ✅ Complete | Root `init` migration contains EventLocation schema, lookups, XOR constraints, and audit tables. Expand/Backfill tested in PostgreSQL. |
| G3 — Disclosure authority | ✅ Complete | Evaluator 72/72 matrix, bounded batch service, registration-scope coverage (62 cases), server-time reveal, and Home/TBA/erased states verified. |
| G4 — API/HAL/contracts | ✅ Complete | `EventLocationController` (public, my-access, management, review queue, PATCH disclosure, POST remediation), HAL link policies, additive OpenAPI generation (`HalOpenApiSchemaCatalog.cs`), and `EventApiClient.g.cs` verified. |
| G5 — Correction reliability | ✅ Complete | `LocationPrivacyCorrectionDispatcher` transactional outbox creation, retry/backoff, dead-letter visibility, cache tag invalidation, and PII-free payload tests verified. |
| G6 — Platform adapter & remediation | ✅ Complete | `IUserLocationPrivacyErasureRepository` PostgreSQL proofs, Home tombstoning, `NeedsPrivacyReview` marking, and `ConfirmEventLocationRemediationCommand` verified. |
| G7 — Consumer convergence | ✅ Partial | Public sessions, program summary, agenda items, calendars (ICS), JSON-LD, MCP/AI, federation (PDS), and Home Discovery converted. Outbound surface audit (`ELP-715`) remains to be finalized. |
| G8 — Blazor & operator UX | 🟡 In Progress | Additive generated client ready; Blazor services (`LocationService`), management editor (`ELP-610`), disclosure cards (`ELP-630`), and review dashboard (`ELP-640`) in progress. |
| G9 — Operations closure | 🟡 In Progress | EventLocation configuration, migration stages, and troubleshooting docs in place. Metrics/alerting (`ELP-540`) and final doc sync (`ELP-740`) remain. |
| G10 — Final repository gate | ⏸️ Pending | Final contraction (`ELP-230C`, `ELP-430`, `ELP-420B`), full release test pass, and dev-doc reconciliation. |

## Current Known Risks / Unknowns

| Severity | Risk | Required disposition |
|---|---|---|
| Critical | Blazor client components still use Stage A redaction helpers alongside legacy Location DTOs. | Migrate Blazor pages and services to consume generated `EventApiClient` purpose-specific methods and DTOs (`ELP-600`..`ELP-660`). |
| Major | Contraction of obsolete generic Location endpoints (`ELP-430`) must not occur prematurely. | Enforce strict wave ordering: do not contract schema (`ELP-230C`) or delete legacy endpoints until Blazor and external consumers are verified migrated. |
| Major | Observability counters for review queue and correction retry backlog are not yet wired to Prometheus. | Implement `ELP-540` using closed-vocabulary categories without PII or entity IDs. |
| Minor | Dependency security advisories (`System.Security.Cryptography.Xml`) exist in build baseline. | Track and remediate separately; does not block ELP business logic. |

## Validation Baseline

- `dotnet build --configuration Release --verbosity quiet` — 26 projects build cleanly with 0 errors.
- `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet` — all Domain tests pass (including `EventLocationTests`, `LocationPrivacyLifecycleTests`, `EventLocationPrivacyAuditTests`, `LocationPrivacyLookupContractTests`).
- `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` — all Application tests pass (including `EventLocationDisclosureEvaluatorTests`, `EventLocationDisclosureServiceTests`, `EventLocationRegistrationAccessServiceTests`, `UpdateEventLocationPolicyCommandHandlerTests`, `EventLocationManagementAuthorizationServiceTests`, `EventLocationExactReadAuditServiceTests`).
- `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` — all API tests pass (including `EventLocationControllerTests`, `EventLocationGovernanceTests`, `EventLocationPrivacyApiContractTests`, `EventLocationPrivacyPublicEligibilityTests`, `EventLocationHateoasTests`).
- `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` — all Infrastructure tests pass (including `LocationPrivacyCorrectionDispatcherTests`).
- `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet` — all Persistence integration tests pass (including `EventLocationDisclosureBatchTests`, `LocationPrivacyCorrectionOutboxPostgreSqlTests`, `GlobalLocationPrivacyErasureTests`).
- `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` — Clean Architecture, naming, and HATEOAS guardrails pass.
- `git diff --check -- dev/active/event-location-privacy` — clean format.

## Handoff Notes

### Handoff — 2026-08-18 Europe/Brussels

- **Current state:** Core EventLocation architecture and API layer are fully implemented and verified. 40 of 59 tasks are verified complete. `EventLocationController` uses `PATCH` for disclosure, `POST` for remediation confirmation, RFC 7807 ProblemDetails, and HAL link gating. `EventApiClient.g.cs` generated client is checked in and ready for UI consumption.
- **Next action:** Execute Phase 7 (Blazor UX adoption: `ELP-600` through `ELP-660`) and `ELP-540` (observability metrics).
- **Blockers:** Schema contraction (`ELP-230C`) and legacy endpoint removal (`ELP-430`) are gated until Blazor UI adoption is complete.
- **Modified files:** `dev/active/event-location-privacy/event-location-privacy-context.md`, `event-location-privacy-plan.md`, `event-location-privacy-tasks.md`.
- **Validation:** Release build passes with 0 errors across 26 projects; architecture and unit/integration test suites are green.
- **Notes for next contributor/agent:** Use `EventApiClient.g.cs` generated methods in Blazor services and gate all UI actions using `_links` HAL affordances (`edit`, `remediate-location`).

