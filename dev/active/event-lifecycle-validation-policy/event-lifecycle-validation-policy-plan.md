<!-- ABOUTME: Implementation plan for nullable lifecycle-friendly event/session storage and command-specific validation. -->
<!-- ABOUTME: Grounds the event validation policy workstream in current Event aggregate, EF Core, CQRS, API, and tests. -->

# Event Lifecycle Validation Policy — Implementation Plan

Last Updated: 2026-06-25 Europe/Brussels

## 0. Planning Metadata

- **Request:** Re-baseline the plan after Senior CTO review so database-level event/session fields can be nullable where lifecycle requires it, while API/Application commands enforce stricter or looser rules per command, lifecycle state, import/archive source, and publication validation. Preserve a single `Event` aggregate, add an `EventSessionStatus` lookup implemented like `EventStatus`, make `EventSession.StartTime`/`EndTime` nullable for draft sessions, and allow a published `Event` to own hidden draft `EventSession` rows.
- **Task directory:** `dev/active/event-lifecycle-validation-policy/`
- **Planning status:** User-reviewed / CTO re-baselined
- **Related prior research:** `dev/active/Event Draft Lifecycle Architecture Consultation.md`
- **Matched intents:** No single exact intent. Compound mapping:
  - `add-ef-migration` - schema/nullability/status lookup changes.
  - `add-cqrs-handler` - lifecycle commands, validators, readiness services.
  - `add-write-endpoint` - import/archive/session transition endpoints.
  - `openapi-contract-change` - request/response DTO and route changes.
  - `update-repository-query` - public/internal event and session visibility filtering.
  - `add-hal-link` - lifecycle affordances must be surfaced through HAL links.
- **Intent contract summary:**
  - `add-ef-migration`
    - must read: `docs/QUICK_REFERENCE.md`, `docs/DOMAIN.md`
    - skills: `dotnet-efcore-guidelines`
    - rules: `.claude/rules/efcore-migrations.md`
    - paths: `Explore.Persistence/Migrations/**/*.cs`, `Explore.Domain/**/*.cs`
    - minimum tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`
    - docs: `schemas/islamu-event.md`
    - acceptance: reversible migration, lookup seed sync
    - forbidden: destructive `Down()` that silently loses data
  - `add-cqrs-handler`
    - must read: `docs/ARCHITECTURE.md`, `docs/QUICK_REFERENCE.md`
    - skills: `cqrs-mediatr-guidelines`
    - rules: `.claude/rules/application-layer.md`
    - paths: `Explore.Application/Features/**/*.cs`
    - minimum tests: `Event.Application.UnitTests`, `Event.Architecture.Tests`
    - acceptance: pipeline behaviors respected
    - forbidden: cross-feature coupling
  - `add-write-endpoint`
    - must read: `docs/API.md`, `docs/QUICK_REFERENCE.md`, `docs/SECURITY-MODEL.md`
    - skills: `cqrs-mediatr-guidelines`, `auth-patterns`
    - rules: `.claude/rules/api-controllers.md`
    - paths: `Explore.API/Controllers/**/*.cs`, `Explore.Application/Features/**/Commands/**/*.cs`
    - minimum tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`
    - docs: `docs/API_CHANGELOG.md`
    - acceptance: rate limiting/idempotency considered, HAL policies updated
    - forbidden: removing `[Authorize]` from writes
  - `openapi-contract-change`
    - must read: `docs/API.md`, `docs/QUICK_REFERENCE.md`
    - rules: `.claude/rules/api-controllers.md`
    - paths: `Explore.API/Controllers/**/*.cs`, `docs/API_CHANGELOG.md`
    - minimum tests: `Event.API.IntegrationTests`, `Event.Architecture.Tests`
    - docs: `docs/API_CHANGELOG.md`
    - forbidden: breaking change without explicit approval
    - current approval: user explicitly approved breaking changes for this development-mode lifecycle rework
  - `update-repository-query`
    - must read: `docs/QUICK_REFERENCE.md`
    - skills: `dotnet-efcore-guidelines`
    - rules: `.claude/rules/efcore-persistence.md`
    - paths: `Explore.Persistence/Repositories/**/*.cs`
    - minimum tests: `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`
    - acceptance: immutable `EventQuerySpecification`, explicit navigation loading
    - forbidden: broad `IgnoreQueryFilters()` without safety test
  - `add-hal-link`
    - must read: `docs/API.md`, `docs/QUICK_REFERENCE.md`
    - rules: `.claude/rules/api-hateoas.md`
    - paths: `Explore.API/Hateoas/**/*.cs`, `Explore.Blazor.Client/**/*.razor`
    - minimum tests: `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`
    - acceptance: detail/collection link policies remain separate
    - forbidden: UI role/claim checks for per-resource affordances
- **Relevant skills loaded:** `clean-architecture-rules`, `dotnet-efcore-guidelines`, `cqrs-mediatr-guidelines`, `auth-patterns`, `senior-cto-feedback`
- **Relevant external docs checked:** EF Core docs via Context7 `/dotnet/entityframework.docs` for nullable CLR/reference types, optional columns, `IsRequired()`, required FK properties, and migration nullability impact; FluentValidation docs via Context7 `/fluentvalidation/fluentvalidation` for RuleSets, `Include`, conditional rules, and manual invocation. Tavily MCP research was used to cross-check state-based draft lifecycle patterns, public visibility filtering, expand/contract migrations, partial indexes, and audit/operations expectations. Project rules still require manual validator instantiation and entity-returning repositories.
- **Relevant rules loaded:** `.claude/rules/domain.md`, `.claude/rules/application-layer.md`, `.claude/rules/efcore-persistence.md`, `.claude/rules/efcore-migrations.md`, `.claude/rules/api-controllers.md`, `.claude/rules/api-hateoas.md`
- **Primary layers touched:** Domain, Application, Persistence, API, HATEOAS, Docs, Tests. Blazor is follow-on only unless HAL/client affordances are implemented in the same workstream.
- **Estimated complexity:** XL. This changes lifecycle semantics across persistence constraints, aggregate state, command validation, authorization-aware API contracts, query visibility, outbox/publication behavior, and existing tests.

## 1. Executive Summary

Implement a soft-validation lifecycle model: the database stores structurally valid but lifecycle-incomplete event/session rows, while the Application layer enforces command-specific and state-specific completeness rules before public exposure, publication, import acceptance, archiving, and session transitions.

The current Event model already follows most of this pattern. `Event` stores many business fields as nullable, uses a required `EventStatusId`, and already supports draft creation through the canonical create path. The main missing parts are:

- `EventSession` has no lifecycle status.
- `EventSession.StartTime`, `EndTime`, and local projection columns are currently required, which blocks schedule-later session drafts.
- Publish readiness is intentionally minimal and not policy-aware.
- Current status updates can set arbitrary `EventStatusId` through a generic endpoint instead of explicit lifecycle transitions.
- Public session queries do not yet have session-status visibility rules.
- Import/archive use cases do not have first-class command contracts or provenance-aware validation.

The recommended implementation keeps one `Event` aggregate and one `EventSession` entity. It does not create separate draft tables. An "event session draft" is a normal `EventSession` row with `EventSessionStatusId = Draft`, nullable schedule fields, and public-query/HAL rules that keep it internal until it is scheduled and published. This explicitly supports adding a draft session to an already published event without exposing it to anonymous/public program surfaces.

Breaking changes are acceptable in this workstream because the platform is pre-v1 and in active development. Do not keep weak compatibility shims, duplicate status endpoints, or old DTO shapes merely to preserve immature contracts.

Out of scope for the first implementation slice:

- Full Blazor UX redesign.
- Generic rules-engine authoring.
- Preserving obsolete pre-v1 compatibility paths. Weak lifecycle contracts should be replaced, documented, and tested rather than kept as shims.
- Federation protocol expansion beyond ensuring draft/internal states are not emitted as published events.

Current implemented session lifecycle API subset is limited to command-backed transitions: create draft, schedule/reschedule, and publish. Submit, approve, and archive session routes should only be added when corresponding Application commands and policies exist.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| The repo uses Clean Architecture, CQRS/MediatR, API/BFF, EF Core, PostgreSQL, and HAL. | Verified: `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/CODEBASE_STRUCTURE.md` | High | The plan must keep Domain/Application/Persistence/API boundaries intact. |
| `AGENTS.md` references `RTK.md`, but no `RTK.md` was found under the repo search path. | Verified by search: `command find /home/amir/ISLAMU/Github/Event -name RTK.md -print` returned no paths | High | Record as missing context; do not assume its contents. |
| The baseline build is green before planning edits. | Verified: `dotnet build --configuration Release --verbosity quiet` returned 25 projects, 0 errors, existing warnings | High | Warnings are pre-existing and numerous. |
| Broad solution build now requires local Blazor WebAssembly tooling to be installed for the pinned SDK. | Verified 2026-06-25: SDK `10.0.300`, no installed workloads, `Explore.Blazor.Client` Release build fails in `ComputeWasmBuildAssets`, and official ASP.NET Core docs require `dotnet workload install wasm-tools`. | High | Installing the workload failed locally with `Inadequate permissions` against `/usr/share/dotnet`; use focused lifecycle verification until the SDK/workload prerequisite is repaired. |
| `Event` already has a required title, tenant, actor, status, visibility, and format, while many business fields are nullable. | Verified: `Explore.Domain/Event.cs`, `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` | High | This matches the soft-validation direction. |
| `EventStatusEnum` already has Draft, Published, Cancelled, Completed, and Archived. | Verified: `Explore.Domain/Enums/EventStatusEnum.cs` | High | No separate EventDraft table is needed. |
| Event statuses are seeded with stable IDs and codes. | Verified: `Explore.Persistence/Seed/LookupTableSeeder.cs` | High | `DRAFT`, `PUBLISHED`, `CANCELLED`, `COMPLETED`, `ARCHIVED`. |
| Event create currently enters through a draft-oriented API request. | Verified: `Explore.API/Controllers/EventController.cs::Create`, `Explore.Application/DTOs/Event/CreateEventDraftRequestDto.cs` | High | `CreateEventDraftRequestDto.ToCreateEventRequest()` feeds `CreateEventCommand`. |
| The canonical create validator accepts minimal title-only draft/import-shaped events. | Verified: `Explore.Application/DTOs/Event/Validators/CreateEventRequestValidator.cs`, `Event.Application.UnitTests/Features/Events/Validators/CreateEventRequestValidatorTests.cs` | High | Test `Validate_WithMinimalDraftRequest_ReturnsTrue` protects this. |
| Creating a published event already triggers readiness and outbox creation. | Verified: `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`, `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs` | High | Published create requires at least current readiness. |
| Existing publish readiness is minimal. | Verified: `Explore.Application/Services/EventPublishReadinessEvaluator.cs` | High | It blocks cancelled/archived status, missing title, and missing `FirstSessionStartUtc`. |
| Publish writes two transactional outbox messages. | Verified: `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`, `docs/OUTBOX_PATTERN.md` | High | External `EventPublished` plus internal notification fanout. |
| Public event lists hide draft and archived events by default. | Verified: `Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs`, `Explore.Application/Specifications/Events/EventFilter.cs`, `Event.API.IntegrationTests/Features/EventVisibilityContractTests.cs` | High | Explicit status filter can override in query path. |
| Draft event detail is hidden from anonymous users unless current user is creator. | Verified: `Explore.Application/Features/Events/Handlers/Queries/GetEventDetailsRequestHandler.cs` | Medium | Uses cached DTO then repository re-fetch for creator check. |
| `EventSession` currently has no status field and requires non-null `StartTime`/`EndTime`. | Verified: `Explore.Domain/EventSession.cs`, `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` | High | This blocks unscheduled session drafts. |
| `EventSessionStatus` must mirror the existing `EventStatus` lookup implementation pattern. | Verified: `Explore.Domain/EventStatus.cs`, `Explore.Domain/Enums/EventStatusEnum.cs`, `Explore.Persistence/Configurations/Entities/EventStatusConfiguration.cs`, `Explore.Persistence/Seed/LookupTableSeeder.cs`, `Explore.Application/Contracts/Persistence/IEventStatusRepository.cs`, `Explore.Persistence/Repositories/EventStatusRepository.cs`, `Explore.API/Controllers/EventStatusController.cs` | High | The new lookup should use the same entity/enum/config/seed/repository/query/controller pattern, with new-file `ABOUTME` headers. |
| Session validators require concrete start/end times. | Verified: `Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs`, `UpdateEventSessionDtoValidator.cs` | High | `NotEmpty` and `GreaterThan`. |
| Session schedule constraints and overlap exclusion assume non-null times. | Verified: `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`, `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs` | High | GiST exclusion uses `tstzrange(start_time, end_time, '[)')`. |
| Public session list/detail endpoints currently have no session-status visibility model. | Verified: `Explore.API/Controllers/EventSessionController.cs`, `Explore.Application/Features/EventSessions/Handlers/Queries/GetEventSessionListRequestHandler.cs`, `GetSessionsByEventRequestHandler.cs`, `Explore.Persistence/Repositories/EventSessionRepository.cs` | High | Adding session drafts must update query visibility, not only schema. |
| Public session list/detail/by-event endpoints now use explicit public-read repository methods. | Verified: `IEventSessionRepository.GetPublic*`, `EventSessionRepository.BuildPublicSessionQuery()`, `GetEventSessionListRequestHandler`, `GetSessionsByEventRequestHandler`, `GetEventSessionDetailsRequestHandler`, `EventSessionVisibilityContractTests` | High | Public reads require published scheduled sessions under publicly discoverable parent events. |
| Existing event policy model is about submission toggles and event card behavior, not required-field profiles. | Verified: `Explore.Domain/Policies/EventPolicy.cs`, `Explore.Application/DTOs/Instance/EventPolicyDto.cs`, `Explore.Application/Services/TenantPolicySettingService.Read.cs` | High | New validation profiles need extension or a separate controlled policy model. |
| There is no `EventSessionStatus` today. | Verified by search: `EventSessionStatus`, `event_session_status`, `session status` under `Explore.*` and tests | High | New lookup/entity/config/seed/repository/DTO work is required. |
| EF Core treats nullable reference type annotations as model nullability unless overridden; `IsRequired()` is the explicit DB-required signal. | Verified via Context7 EF Core docs | High | Align C# nullable types and EF config before generating migrations. |
| FluentValidation supports RuleSets, `Include`, and conditional rules, but repo rules require manual validator instantiation. | Verified via Context7 FluentValidation docs and `docs/QUICK_REFERENCE.md` | High | Prefer command-specific validators plus shared included rules, manually instantiated. |
| State-based draft workflows should keep incomplete fields nullable while enforcing completeness at explicit transition/publish boundaries. | Verified via Tavily MCP research on enterprise draft lifecycle/state-machine patterns and PostgreSQL/EF Core migration guidance | Medium | Use as external architecture corroboration; repository rules remain authoritative. |
| Existing active work mentions the same architecture direction. | Verified: `dev/active/Event Draft Lifecycle Architecture Consultation.md` | High | Use as research input; this plan is the operational workstream. |

### 2.2 Existing Implementation

#### Domain

- `Explore.Domain/Event.cs` is the event aggregate root. It stores `EventStatusId` as a required lookup FK and has nullable optional data such as `Description`, `Content`, `EventTypeId`, audience lookups, media, URLs, price, schedule rollups, template metadata, and series fields.
- `Explore.Domain/EventSession.cs` is the scheduled child content entity. It has required `StartTime` and `EndTime`, private-set local projection columns, optional location/room/media/kind/registration fields, and no status.
- `Explore.Domain/Enums/EventStatusEnum.cs` mirrors `EventStatus` lookup IDs.
- No `EventSessionStatus` enum, entity, navigation, or lookup exists.

#### Persistence

- `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` marks `Event.Title` as required, keeps draft-flexible event fields nullable, configures required status/visibility/format relationships, adds listing indexes, and enforces always-valid check constraints such as non-negative price and non-blank timezone.
- `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` configures session relationships, indexes, check constraints, and PostgreSQL room-overlap exclusion constraints. It assumes schedule fields and local projection fields are always present.
- `Explore.Persistence/Seed/LookupTableSeeder.cs` seeds event statuses and other lookups.
- `Explore.Persistence/Repositories/EventRepository.cs` returns domain entities and applies specifications. Public event discoverability is applied at the Application specification level.
- `Explore.Persistence/Repositories/EventSessionRepository.cs` returns domain entities and performs overlap checks against required time ranges.

#### Application

- `CreateEventCommandHandler` validates with `CreateEventRequestValidator`, resolves the actor, creates the event graph, assigns `EventOwner`, creates sessions/days/rooms/agenda items, and writes outbox messages only when the event is published.
- `CreateEventRequestValidator` permits title-only/minimal event creation and validates optional FK existence only when values are present.
- `UpdateEventDraftCommandHandler` updates scalar event shell fields and preserves status, actor, tenant, and schedule-derived projection fields.
- `PublishEventCommandHandler` validates concurrency, evaluates `EventPublishReadinessEvaluator`, sets `EventStatusId = Published`, updates the repository, creates outbox messages, and invalidates cache.
- `UpdateEventCommandHandler` can update status through `UpdateEventStatusDto`, but it does not encode explicit lifecycle transitions.
- `CreateEventSessionCommandHandler` and `UpdateEventSessionCommandHandler` require scheduled sessions and call `EventSession.Reschedule`.

#### API and HAL

- `Explore.API/Controllers/EventController.cs` exposes:
  - `GET /api/Event` public list
  - `GET /api/Event/{id}` public detail
  - `POST /api/Event` authenticated create using `CreateEventDraftRequestDto`
  - `GET /api/Event/{id}/publish-readiness`
  - `POST /api/Event/{id}/publish`
  - `PUT /api/Event/{id}` scalar draft update
  - `PUT /api/Event/{id}/status` generic status update
- `Explore.API/Controllers/EventSessionController.cs` exposes public session list/detail/by-event and authenticated create/update/delete, but no session lifecycle endpoints.
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` and `EventSessionLinkPolicy.cs` own action affordances and must remain the source of truth for UI action gating.

### 2.3 Existing Tests And Verification Coverage

- `Event.Application.UnitTests/Features/Events/Validators/CreateEventRequestValidatorTests.cs`
  - Validates minimal draft request behavior and optional lookup handling.
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs`
  - Covers initial owner assignment, published create readiness, and outbox creation.
- `Event.Application.UnitTests/Features/Events/Commands/PublishEventCommandHandlerTests.cs`
  - Covers draft publish, concurrency conflict, readiness failure, outbox messages, and cache invalidation.
- `Event.API.IntegrationTests/Features/EventVisibilityContractTests.cs`
  - Covers draft/archived event visibility in public list/detail responses.
- `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs`
  - Covers event/session/agenda scheduling constraints and room overlap behavior.
- Existing gaps:
  - No tests for `EventSessionStatus`.
  - No tests for unscheduled session drafts.
  - No policy-aware event readiness tests.
  - No import/archive command tests.
  - No public session visibility tests for draft/approved/published session states.
  - No transition matrix tests for valid/invalid event/session lifecycle changes.

### 2.4 Existing Documentation And Contracts

- `docs/ARCHITECTURE.md` documents Clean Architecture, CQRS, authorization, HAL, event data layers, outbox, and current event publication outbox behavior.
- `docs/DOMAIN.md` documents core aggregates, Event data layers, schedule source of truth, persistence-enforced rules, and current Event/EventSession scheduling constraints.
- `docs/API.md` documents middleware order, idempotency, rate limiting, caching, auth, HAL, and controller conventions.
- `docs/SECURITY-MODEL.md` and `docs/AUTHORIZATION.md` document Keycloak, BFF, MediatR authorization, Cerbos/local fallback, fail-closed behavior, and HAL authorization.
- `docs/MULTI_TENANCY.md` documents fail-closed tenant filters and query-filter bypass restrictions.
- `docs/API_CHANGELOG.md` documents current public API behavior and must be updated for contract changes.
- `docs/TESTING.md` contains the concrete per-project test commands, even though `AGENTS.md` points to `docs/OPERATIONS.md#full-project-test-list`.

### 2.5 Current Pain Points / Improvement Areas

1. **Session drafts are not representable without fake times.**
   - Evidence: `EventSession.StartTime` and `EndTime` are non-null, validators require both, and DB constraints/exclusion assume both.
   - Why it matters: a published event cannot safely accept incomplete future sessions or speaker submissions without storing misleading schedule data.

2. **Event publication readiness is too narrow for product quality and policy-driven deployments.**
   - Evidence: `EventPublishReadinessEvaluator` checks only status, title, and first session start.
   - Why it matters: hosted ISLAMU, lightweight self-hosters, imports, archives, and bots need different strictness without changing schema.

3. **Generic event status update bypasses explicit lifecycle semantics.**
   - Evidence: `UpdateEventCommandHandler` accepts any valid `EventStatusId` through `UpdateEventStatusDto`.
   - Why it matters: archive/cancel/publish/import/reopen should have distinct checks, side effects, authorization actions, and audit semantics.

4. **Public session surfaces now distinguish draft/internal sessions from public program items.**
   - Evidence: `GetEventSessionListRequestHandler`, `GetSessionsByEventRequestHandler`, and `GetEventSessionDetailsRequestHandler` call explicit public repository methods backed by `EventSessionRepository.BuildPublicSessionQuery()`.
   - Current behavior: anonymous list/detail/by-event responses require `EventSessionStatusEnum.Published`, non-null schedule, public parent visibility, and parent event status not Draft/Moderated/Archived.

5. **Policy infrastructure exists but does not model required-field profiles.**
   - Evidence: `EventPolicy` and `EventPolicyDto` contain submission toggles/card behavior, not field requirements.
   - Why it matters: command-specific optionality must be centralized, auditable, and tenant/instance-aware.

6. **Create/update DTOs mix syntactic validation with lifecycle completeness.**
   - Evidence: create validator permits minimal drafts, while publish readiness enforces only a small semantic subset.
   - Why it matters: implementations risk reintroducing rigid DB/DTO requiredness instead of expressing completeness at transition points.

7. **Session schedule rollups currently treat all sessions as active schedule contributors.**
   - Evidence: `Event.RecalculateScheduleSummaryFromSessions()` uses all non-deleted sessions.
   - Why it matters: draft or rejected unscheduled sessions must not affect event dates, exports, notifications, or search.

### 2.6 Unknowns After Investigation

- **Exact import provenance contract:** Not found. Implementation must decide minimum fields such as `SourceSystem`, `ExternalId`, `SourceUrl`, import actor, trust level, and whether imported events can auto-publish.
- **Archive semantics:** No first-class `ArchiveEventCommand` found. Decide whether archived means historical imported content, retired organizer workflow, or both.
- **Session status taxonomy naming:** No current `EventSessionStatus`. This re-baseline approves the initial stable lookup shape and IDs, but display names/descriptions may still be refined before the migration is generated.
- **Whether event publication should allow zero public sessions:** Current code requires `FirstSessionStartUtc` because outbox payloads depend on schedule. Changing that is separate and higher risk.
- **Policy storage shape:** Existing governance settings can support simple field/profile toggles, but a dedicated validation policy table may be needed if profiles become complex or audited separately.
- **Generated client update command:** The repo has API client generation, but the exact command was not needed during planning. Implementation must verify before changing OpenAPI.

## 3. Proposed Future State

### Target Model

Keep one canonical `Event` row and one canonical `EventSession` row. Draftness, event-session draftness, import tolerance, archived historical records, publication, and review are lifecycle states, not separate tables. A published event can own both public sessions and draft/internal sessions; public visibility is determined by parent event state plus child session state.

Database constraints enforce structural invariants only:

- tenant ownership,
- aggregate parent links,
- lookup FK integrity,
- required title/identity fields,
- non-negative numeric values,
- conditional schedule consistency,
- tenant-safe uniqueness/indexes,
- room overlap only for scheduled, active, room-bound sessions.

Application and Domain enforce lifecycle/business invariants:

- command-specific required fields,
- source-specific import/archive tolerance,
- event/session transition matrix,
- policy-aware publication readiness,
- tenant/instance governance locks,
- public/private/internal visibility,
- outbox side effects only when publishing public content.

### Lifecycle Sketch

```text
Event
  DRAFT -> PUBLISHED -> COMPLETED
        -> ARCHIVED
        -> CANCELLED

Imported/archived historical events:
  accepted with minimal required structure + provenance
  not public unless Publish readiness passes

EventSession
  DRAFT -> SUBMITTED -> UNDER_REVIEW -> APPROVED -> PUBLISHED
        -> REJECTED
        -> CANCELLED
        -> ARCHIVED

Published Event with future work-in-progress program:
  Event = PUBLISHED
  EventSession = DRAFT/SUBMITTED/UNDER_REVIEW/APPROVED
  Result: visible only through authorized internal organizer/speaker/reviewer paths,
          never through anonymous public session/program/export/search surfaces.
```

### Validation Profiles

Use named validation profiles, not ad hoc `if` statements spread across handlers:

| Profile | Purpose | Typical strictness |
|---|---|---|
| `event.draft.create` | Native progressive draft shell | Minimal: title, tenant, owner Actor, status/defaults |
| `event.native.submit` | User/platform-created event submission | Configurable, often stricter on hosted instance |
| `event.import.create` | External import/bot/backfill | Tolerant but provenance required |
| `event.archive.create` | Historical/past-event archive | Tolerant, not publication-ready by default |
| `event.publish` | Public event publication | Strict, policy-aware, public/export/federation safe |
| `session.draft.create` | Draft/proposal session under an event | Minimal: parent event, tenant, title or placeholder label, status |
| `session.schedule` | Assign time/room/day | Requires valid times and room conflict checks |
| `session.publish` | Public program item publication | Requires status, title, schedule, visibility, parent event compatibility |

### EventSession Status and Nullable Scheduling

Add `EventSessionStatus` as a required lookup FK on `EventSession`. Implement it with the same lookup pattern as `EventStatus`:

- new `Explore.Domain/EventSessionStatus.cs` with `Id`, `MasterCode`, `FullName`, and nullable `Description`;
- new `Explore.Domain/Enums/EventSessionStatusEnum.cs` with stable `int` IDs;
- new `Explore.Persistence/Configurations/Entities/EventSessionStatusConfiguration.cs` using `ValueGeneratedNever()`, required `MasterCode`/`FullName`, and the same max lengths as `EventStatusConfiguration`;
- new `DbSet<EventSessionStatus> EventSessionStatuses` in `Explore.Persistence/ExploreDbContext.DbSets.cs`;
- new idempotent seed path in `Explore.Persistence/Seed/LookupTableSeeder.cs`, invoked beside `SeedEventStatusesAsync`;
- new `IEventSessionStatusRepository`, `EventSessionStatusRepository`, and DI registration in `PersistenceServicesRegistration`;
- new lookup DTOs/query handlers/controller/route names mirroring `EventStatusController`, unless implementation proves the existing lookup API convention has changed globally.

Baseline lookup IDs:

| Id | Enum | MasterCode | Public meaning |
|---:|---|---|---|
| 1 | `Draft` | `DRAFT` | Internal, incomplete, not public, schedule may be null. |
| 2 | `Submitted` | `SUBMITTED` | Proposed by speaker/import/user, waiting for review, not public. |
| 3 | `UnderReview` | `UNDER_REVIEW` | Actively reviewed, not public. |
| 4 | `Approved` | `APPROVED` | Accepted for the program, still not public until published. |
| 5 | `Published` | `PUBLISHED` | Public program session; schedule is required. |
| 6 | `Rejected` | `REJECTED` | Internal/rejected, not public. |
| 7 | `Cancelled` | `CANCELLED` | Cancelled program item; public visibility must be an explicit product decision. |
| 8 | `Archived` | `ARCHIVED` | Historical/internal archive, not public by default. |

Make schedule columns nullable for draft-capable sessions:

- `StartTime` -> `DateTimeOffset?`
- `EndTime` -> `DateTimeOffset?`
- local projection fields -> nullable equivalents

Use conditional constraints:

- either both `start_time` and `end_time` are null, or `end_time > start_time`;
- local projection fields are all null when unscheduled, and complete/consistent when scheduled;
- room overlap exclusion applies only when `room_id IS NOT NULL`, not deleted, and both times are present;
- scheduled indexes must place null-safe status/tenant/event predicates first enough to keep public list/program queries efficient;
- scheduled public/approved statuses require schedule data through Application validation, not a DB status-specific check unless the status set is stable enough for DB enforcement.

### Policy-Aware Readiness

Replace the static `EventPublishReadinessEvaluator.Evaluate(Event)` with an application service that accepts an effective policy:

```csharp
EventLifecycleReadiness readiness =
    evaluator.Evaluate(@event, profile, effectivePolicy);
```

The result should preserve machine-readable errors:

- `Code`
- `FieldPath`
- `Message`
- `Severity`
- `Source` such as `HardInvariant`, `DomainRule`, `InstancePolicy`, `TenantPolicy`, `CommandProfile`

Do not rely on thrown exceptions for normal readiness UI. Throw only for forbidden transitions, security violations, concurrency conflicts, and impossible states.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs.
- Mapping remains in handlers/profiles, not repositories.
- Validators are manually instantiated in handlers/services.
- Domain cannot reference EF Core, MediatR, ASP.NET Core, AutoMapper, Persistence, API, or Blazor.
- Application cannot depend on `ExploreDbContext`.
- GET endpoints are anonymous only when they are safe public reads; writes stay `[Authorize]`.
- Resource-level auth stays in MediatR via `IAuthorizedRequest`, `[AuthorizeResource]`, or `ISecureRequest`.
- HAL links are the UI source of truth for action affordances.
- Tenant isolation stays API-authoritative and EF-filter-backed; no broad `IgnoreQueryFilters()`.
- Commands return `BaseCommandResponse<Guid>` where local patterns do.
- All new C# files start with two `ABOUTME:` lines and use file-scoped namespaces.
- `Guid` remains aggregate ID type; `int` remains lookup ID type.
- Do not introduce a generic rules engine for required fields in the first slice.
- Do not create `EventDraft` or `EventSessionDraft` tables. "Event session draft" means `EventSession.EventSessionStatusId = Draft` on the canonical `event_sessions` table.
- Do not publish/import/federate incomplete draft content as public content.
- Do not silently destructive-rollback nullable session schedules in migration `Down()`.
- Do not preserve pre-v1 compatibility shims when they conflict with the cleaner lifecycle model. Delete or replace weak routes/DTOs after documenting the breaking change.

## 5. Architecture And Design Decisions

### Decision 1: Keep Single Event Aggregate

- **Decision:** Use `EventStatusId` and explicit lifecycle commands on the existing `Event`.
- **Why:** Current code already supports draft rows, draft creation, public filtering, publish readiness, and outbox publication.
- **Alternatives considered:** Separate `EventDraft` table/entity.
- **Consequences:** Less duplication, no clone/merge logic, but lifecycle rules must be explicit.
- **Files/layers affected:** `Explore.Domain/Event.cs`, `Explore.Application/Features/Events/**`, `Explore.API/Controllers/EventController.cs`, event tests.

### Decision 2: Add EventSessionStatus, Not EventSessionDraft

- **Decision:** Add a required `EventSessionStatusId` FK plus seeded lookup rows, implemented like the existing `EventStatus` lookup across Domain enum/entity, EF configuration, DbSet, lookup seed, repository, DI registration, Application lookup DTO/query flow, and API lookup surface.
- **Why:** A published event can have unpublished/draft sessions without duplicating session-related entities.
- **Alternatives considered:** separate `EventSessionDraft` table; reuse `EventStatus`.
- **Consequences:** Requires query visibility rules, migration backfill, DTO/API/HAL updates, and transition commands.
- **Files/layers affected:** `Explore.Domain/EventSession.cs`, new `Explore.Domain/EventSessionStatus.cs`, new `Explore.Domain/Enums/EventSessionStatusEnum.cs`, new EF config, `ExploreDbContext.DbSets.cs`, `LookupTableSeeder.cs`, new repository/interface/DI registration, lookup DTOs/query handlers/controller, session DTOs, session validators, and tests.

### Decision 3: Make Session Schedule Nullable For Draft-Capable Sessions

- **Decision:** Change `EventSession.StartTime`, `EventSession.EndTime`, and cached local projection columns to nullable so draft sessions can be saved without fake schedule data.
- **Why:** Fake schedule values corrupt public search, room conflicts, reminders, exports, and event rollups.
- **Alternatives considered:** Require provisional times for all session drafts.
- **Consequences:** More persistence work and query guard logic, but correct product model.
- **Files/layers affected:** `EventSession`, `EventSessionConfiguration`, migrations, scheduling tests, command handlers.

### Decision 3A: Published Events May Own Draft Sessions

- **Decision:** Permit `EventStatus = Published` with child sessions in `Draft`, `Submitted`, `UnderReview`, or `Approved` status.
- **Why:** Organizers need to build future/private program items without unpublishing the event or creating duplicate event/session records.
- **Alternatives considered:** force event unpublish/re-draft before editing program; separate draft session table.
- **Consequences:** Every public session/program/export/search/readiness path must filter by session status and parent event visibility; HAL links must expose draft-session creation only to authorized actors.
- **Files/layers affected:** event-session queries, `EventProgram` queries, calendar export, AI reference/search, HAL policies, event/session API tests.

### Decision 4: Centralize Required-Field Profiles in Application

- **Decision:** Add controlled validation/readiness profiles in Application. Use FluentValidation for syntactic/FK rules and a lifecycle readiness service for semantic/policy completeness.
- **Why:** Requiredness differs by command/source/state and should not be encoded as DB `NOT NULL`.
- **Alternatives considered:** DB constraints for every business requirement; one huge FluentValidation RuleSet class; EAV/custom-property policies.
- **Consequences:** More explicit service layer, easier tests, no generic rules engine.
- **Files/layers affected:** new Application services/contracts under `Explore.Application/Services` or `Features/Events/Lifecycle`, validators, handler tests.

### Decision 5: Replace Generic Status Mutation With Explicit Transitions

- **Decision:** Keep or deprecate `PUT /api/Event/{id}/status` only if it delegates to transition rules; prefer explicit publish/archive/cancel/reopen/import commands.
- **Why:** Status transitions have different validation, authorization, side effects, cache invalidation, and outbox behavior.
- **Alternatives considered:** Continue arbitrary status update by lookup ID.
- **Consequences:** API contract change and possible changelog entry, but safer lifecycle.
- **Files/layers affected:** `EventController`, `UpdateEventCommandHandler`, new commands, HAL policies, API tests.

### Decision 6: Public Session Visibility Must Be Status-Aware

- **Decision:** Public session list/detail/by-event endpoints should return only public session statuses for public parent events unless caller is authorized for internal event management.
- **Why:** Session drafts under a published event must not leak.
- **Alternatives considered:** Let UI hide draft sessions after receiving them.
- **Consequences:** Repository/query changes and tests are mandatory before enabling draft sessions.
- **Files/layers affected:** `EventSessionRepository`, event-session query handlers, API integration tests, HAL policies.

## 6. Implementation Phases

### Phase 0: Plan Review And Final Decisions

- **Goal:** Record the user-approved lifecycle direction and leave only non-blocking provenance/display-name details for implementation.
- **Depends on:** This draft plan.
- **Relevant files:** `dev/active/event-lifecycle-validation-policy/*`
- **Acceptance criteria:**
  - Session draft nullability decision is recorded as approved: `StartTime`/`EndTime` nullable for draft sessions.
  - Initial `EventSessionStatus` lookup IDs/codes are recorded.
  - Breaking-change position is recorded: no compatibility shims for immature lifecycle contracts.
  - Import/archive minimum provenance scope remains tracked but does not block Phase 1 persistence work.
- **Verification:** Docs updated to User-reviewed / CTO re-baselined.
- **Rollback / failure handling:** If implementation discovers a real conflict, re-baseline these dev docs before changing code.

#### Task 0.1: Review lifecycle taxonomy

- **Type:** investigate / docs
- **Layer:** Docs
- **Files:** existing `event-lifecycle-validation-policy-plan.md`
- **Description:** Confirm event and session status names and meanings before migration.
- **Acceptance Criteria:**
  - [x] Status codes are final enough for stable lookup IDs.
  - [x] Session visibility mapping is accepted: only `Published` sessions are public by default.
  - [x] Nullable `StartTime`/`EndTime` for draft sessions is accepted.
  - [ ] Event publish with zero public sessions is accepted or rejected explicitly.
- **Dependencies:** none
- **Effort:** S
- **Required Skills/Rules:** senior-cto-feedback
- **Validation:** user approval note in context file

### Phase 1: Persistence Foundation

- **Goal:** Add `EventSessionStatus` and nullable draft-session schedule support without changing public API behavior yet.
- **Depends on:** Phase 0 re-baseline.
- **Relevant files:**
  - existing `Explore.Domain/EventSession.cs`
  - new `Explore.Domain/EventSessionStatus.cs`
  - new `Explore.Domain/Enums/EventSessionStatusEnum.cs`
  - existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
  - new `Explore.Persistence/Configurations/Entities/EventSessionStatusConfiguration.cs`
  - existing `Explore.Persistence/Seed/LookupTableSeeder.cs`
  - existing `Explore.Persistence/ExploreDbContext.DbSets.cs`
  - new `Explore.Application/Contracts/Persistence/IEventSessionStatusRepository.cs`
  - new `Explore.Persistence/Repositories/EventSessionStatusRepository.cs`
  - existing `Explore.Persistence/PersistenceServicesRegistration.cs`
  - new migration under `Explore.Persistence/Migrations/`
- **Acceptance criteria:**
  - `EventSessionStatusId` is required and backfilled.
  - Existing scheduled sessions remain valid.
  - Draft sessions can be persisted without schedule.
  - A published event can own draft/internal sessions without changing the parent event status.
  - Conditional schedule constraints are enforced.
  - Room overlap exclusion ignores unscheduled sessions.
  - Tenant filters and composite FKs remain intact.
- **Verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Migration `Down()` must not silently coerce unscheduled sessions into fake times. It should either restore schema only when no unscheduled rows exist or document a manual cleanup requirement.

#### Task 1.1: Add session status lookup model

- **Type:** create / modify
- **Layer:** Domain / Application / Persistence / API lookup
- **Files:**
  - new `Explore.Domain/EventSessionStatus.cs`
  - new `Explore.Domain/Enums/EventSessionStatusEnum.cs`
  - existing `Explore.Domain/EventSession.cs`
  - existing `Explore.Persistence/ExploreDbContext.DbSets.cs`
  - new `Explore.Persistence/Configurations/Entities/EventSessionStatusConfiguration.cs`
  - existing `Explore.Persistence/Seed/LookupTableSeeder.cs`
  - new `Explore.Application/Contracts/Persistence/IEventSessionStatusRepository.cs`
  - new `Explore.Persistence/Repositories/EventSessionStatusRepository.cs`
  - existing `Explore.Persistence/PersistenceServicesRegistration.cs`
  - new `Explore.Application/DTOs/EventSessionStatus/*`
  - new `Explore.Application/Features/EventSessionStatuses/**`
  - new `Explore.API/Controllers/EventSessionStatusController.cs`
- **Description:** Add lookup with stable IDs/codes: `DRAFT`, `SUBMITTED`, `UNDER_REVIEW`, `APPROVED`, `PUBLISHED`, `REJECTED`, `CANCELLED`, `ARCHIVED`, mirroring `EventStatus` implementation patterns unless a local lookup convention has superseded them.
- **Acceptance Criteria:**
  - [ ] Lookup IDs are `int` and `ValueGeneratedNever`.
  - [ ] `EventSession.EventSessionStatusId` is required.
  - [ ] `EventSession.EventSessionStatus` navigation exists.
  - [ ] Lookup seed is idempotent and stable.
  - [ ] Repository/interface/DI registration exist if validators or lookup APIs need status existence checks.
  - [ ] Lookup controller/query DTOs mirror `EventStatusController` unless implementation proves a newer global lookup convention supersedes it.
- **Dependencies:** Task 0.1
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, dotnet-efcore-guidelines, domain, efcore-persistence
- **Validation:** build plus persistence lookup tests

#### Task 1.2: Make session scheduling nullable for drafts

- **Type:** modify
- **Layer:** Domain / Persistence
- **Files:**
  - existing `Explore.Domain/EventSession.cs`
  - existing `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs`
  - existing `Explore.Persistence/Repositories/EventSessionRepository.cs`
  - existing `Explore.Domain/Event.cs`
- **Description:** Make `StartTime`, `EndTime`, and cached local projections nullable. Add a domain method such as `ClearSchedule()` or explicit draft constructor path so draft sessions can intentionally remain unscheduled, while `Reschedule()` remains the only path to assign a real schedule and derived local projections.
- **Acceptance Criteria:**
  - [ ] Unscheduled draft session can exist with null start/end/local schedule projections.
  - [ ] Scheduled session still uses `Reschedule`.
  - [ ] Event schedule summary excludes unscheduled sessions.
  - [ ] Overlap checks are skipped for unscheduled sessions.
  - [ ] Public/session-publish validation requires non-null schedule before `EventSessionStatus = Published`.
- **Dependencies:** Task 1.1
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules, dotnet-efcore-guidelines
- **Validation:** domain unit tests and persistence integration tests

#### Task 1.3: Add EF migration and constraint tests

- **Type:** create / test
- **Layer:** Persistence
- **Files:**
  - new migration under `Explore.Persistence/Migrations/`
  - existing `Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs`
  - existing `Event.Persistence.IntegrationTests/Repositories/SchedulingConstraintTests.cs`
  - possible new `Event.Persistence.IntegrationTests/Repositories/EventSessionStatusRepositoryTests.cs`
- **Description:** Generate migration for status lookup, backfill, nullable schedule columns, conditional check constraints, indexes, and updated exclusion metadata.
- **Acceptance Criteria:**
  - [ ] Existing rows are backfilled deterministically.
  - [ ] Draft sessions under draft/archived parent events receive safe status.
  - [ ] Existing sessions under `Published`/`Completed` parent events receive `PUBLISHED`.
  - [ ] Published parent events can later receive newly created `DRAFT` sessions.
  - [ ] Conditional constraints reject partial schedule shapes.
  - [ ] Exclusion constraint still rejects overlapping scheduled room sessions.
- **Dependencies:** Task 1.2
- **Effort:** L
- **Required Skills/Rules:** dotnet-efcore-guidelines, efcore-migrations
- **Validation:** `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`

### Phase 2: Lifecycle Policy And Readiness

- **Goal:** Centralize command/source/state validation without hard-coding every requirement into database nullability.
- **Depends on:** Phase 1.
- **Relevant files:**
  - new Application lifecycle policy/readiness services
  - existing `Explore.Application/Services/EventPublishReadinessEvaluator.cs`
  - existing event/session validators and command handlers
  - existing policy DTO/settings services if extended
- **Acceptance criteria:**
  - Hard invariants, domain rules, and policy-required fields are distinguishable.
  - Draft/import/archive create paths are tolerant but bounded.
  - Publish and session publish are strict enough for public discovery/outbox/federation.
  - Validators remain manually instantiated.
- **Verification:**
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

#### Task 2.1: Add validation profile model

- **Type:** create
- **Layer:** Application
- **Files:** new under `Explore.Application/Features/Events/Lifecycle/` or `Explore.Application/Services/`
- **Description:** Define controlled profile identifiers and field keys for event/session validation.
- **Acceptance Criteria:**
  - [ ] Profiles include draft, native submit, import, archive, publish, session draft, session schedule, session publish.
  - [ ] Field keys are product concepts, not raw reflection property names.
  - [ ] No arbitrary executable rules are introduced.
- **Dependencies:** Phase 1
- **Effort:** M
- **Required Skills/Rules:** cqrs-mediatr-guidelines, application-layer
- **Validation:** Application unit tests

#### Task 2.2: Replace static publish readiness with policy-aware evaluator

- **Type:** modify
- **Layer:** Application
- **Files:**
  - existing `Explore.Application/Services/EventPublishReadinessEvaluator.cs`
  - existing `Explore.Application/DTOs/Event/EventPublishReadinessDto.cs`
  - existing `Explore.Application/Features/Events/Handlers/Queries/GetEventPublishReadinessRequestHandler.cs`
  - existing `Explore.Application/Features/Events/Handlers/Commands/PublishEventCommandHandler.cs`
  - existing `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- **Description:** Accept effective profile/policy and return richer readiness errors without breaking normal ProblemDetails handling.
- **Acceptance Criteria:**
  - [ ] Current readiness behavior is preserved as baseline.
  - [ ] Policy-required fields can be added without changing DB nullability.
  - [ ] Event publish blocks incomplete policy-required public data.
  - [ ] Outbox creation happens only after readiness passes.
- **Dependencies:** Task 2.1
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns
- **Validation:** `PublishEventCommandHandlerTests`, new readiness tests

#### Task 2.3: Add explicit event lifecycle commands

- **Type:** create / modify
- **Layer:** Application
- **Files:**
  - new `ArchiveEventCommand`, `CancelEventCommand`, optional `ImportEventCommand`
  - existing `Explore.Application/Features/Events/Requests/Commands/UpdateEventCommand.cs`
  - existing `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs`
- **Description:** Move lifecycle transitions away from arbitrary status ID mutation and into explicit commands with authorization, validation profile, concurrency, cache invalidation, and side-effect rules.
- **Acceptance Criteria:**
  - [ ] Archive command is tolerant and does not publish or emit public outbox events.
  - [ ] Import command requires provenance and owner/tenant but allows missing publication-quality fields.
  - [ ] Generic status update is removed, constrained, or delegated to transition service.
  - [ ] Invalid transitions return safe validation/concurrency errors.
- **Dependencies:** Task 2.2
- **Effort:** L
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns, application-layer
- **Validation:** Application command tests

#### Task 2.4: Add session lifecycle commands

- **Type:** create / modify
- **Layer:** Application
- **Files:**
  - existing `Explore.Application/Features/EventSessions/**`
  - existing `Explore.Application/DTOs/EventSession/**`
  - new session lifecycle command/DTO/validator files
- **Description:** Add `CreateEventSessionDraft`, `ScheduleEventSession`, `SubmitSessionForReview`, `ApproveSession`, `PublishSession`, `RejectSession`, `ArchiveSession`, or a minimal first subset approved in Phase 0.
- **Acceptance Criteria:**
  - [ ] Draft session can be created under a published event without public visibility.
  - [ ] Scheduling is a separate validation path when times/room are supplied.
  - [ ] Publishing a session requires schedule and parent event compatibility.
  - [ ] Speaker/reviewer/organizer permissions are enforced server-side.
- **Dependencies:** Task 1.2, Task 2.1
- **Effort:** XL
- **Required Skills/Rules:** cqrs-mediatr-guidelines, auth-patterns, application-layer
- **Validation:** Application unit tests plus API integration tests in Phase 4

### Phase 3: Repository Visibility And Query Semantics

- **Goal:** Keep draft/internal events and sessions out of public discovery while allowing authorized organizer/speaker workflows.
- **Depends on:** Phase 1 and Phase 2.
- **Relevant files:**
  - `Explore.Application/Specifications/Events/EventFilter.cs`
  - `Explore.Application/Specifications/EventSessions/**`
  - `Explore.Application/Features/EventSessions/Handlers/Queries/**`
  - `Explore.Persistence/Repositories/EventRepository.cs`
  - `Explore.Persistence/Repositories/EventSessionRepository.cs`
- **Acceptance criteria:**
  - Public event queries remain draft-safe.
  - Public session queries exclude draft/submitted/review/rejected/archived sessions.
  - Authorized internal queries can intentionally include draft sessions.
  - Cache keys include tenant/status/profile dimensions where required.
- **Verification:**
  - `Event.Persistence.IntegrationTests`
  - `Event.API.IntegrationTests`

#### Task 3.1: Add session public visibility filters ✅ COMPLETE

- **Type:** modify / test
- **Layer:** Application / Persistence
- **Files:**
  - existing `Explore.Application/Features/EventSessions/Handlers/Queries/GetEventSessionListRequestHandler.cs`
  - existing `Explore.Application/Features/EventSessions/Handlers/Queries/GetSessionsByEventRequestHandler.cs`
  - existing `Explore.Persistence/Repositories/EventSessionRepository.cs`
  - possible new `Explore.Application/Specifications/EventSessions/EventSessionFilter.cs`
- **Description:** Ensure public session list/detail/by-event endpoints return only public session states and public parent events.
- **Acceptance Criteria:**
  - [ ] Draft session under published event is hidden anonymously.
  - [ ] Published session under draft/archived/private parent event is hidden anonymously.
  - [ ] Authorized organizer internal query can see draft sessions through an explicit endpoint/query.
- **Dependencies:** Task 1.1
- **Effort:** L
- **Required Skills/Rules:** dotnet-efcore-guidelines, efcore-persistence, cqrs-mediatr-guidelines
- **Validation:** API and persistence tests; focused public session visibility API contract tests passed

#### Task 3.2: Add authorized internal session query path if needed

- **Type:** modify / test
- **Layer:** Application / API / Persistence
- **Files:**
  - new authorized session query/handler/API endpoint if organizer or speaker UX needs draft sessions
- **Description:** Add an explicit authorized path for internal session management only if current UI/API workflows need draft-session reads beyond existing command paths.
- **Acceptance Criteria:**
  - [ ] Internal query is authenticated and tenant-safe.
  - [ ] Anonymous endpoints remain public-only.
  - [ ] HAL/UI consumers do not infer internal affordances from claims.
- **Dependencies:** Task 3.1
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, cqrs-mediatr-guidelines, auth-patterns
- **Validation:** API integration tests for allowed and denied callers

#### Task 3.3: Update event schedule rollups ✅ COMPLETE

- **Type:** modify / test
- **Layer:** Domain / Application
- **Files:**
  - existing `Explore.Domain/Event.cs`
  - existing `Explore.Domain/EventSession.cs`
  - existing session lifecycle command handlers
- **Description:** Ensure event public schedule summaries use only published, scheduled sessions.
- **Acceptance Criteria:**
  - [x] Draft sessions do not move event first/last session dates.
  - [x] Rejected/cancelled/archived/unscheduled sessions do not move event first/last session dates.
  - [x] Scheduling/publishing lifecycle commands refresh parent event rollups after successful mutation.
  - [x] Direct published create marks initial sessions as published; draft create keeps initial sessions internal and out of public rollups.
- **Dependencies:** Task 1.5
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules, cqrs-mediatr-guidelines
- **Validation:** Domain/Application unit tests; build and architecture tests

#### Task 3.4: Verify export/search/notification/federation public boundaries

- **Type:** modify / test
- **Layer:** Application / Persistence
- **Files:**
  - existing calendar/export/event program query handlers
  - existing event/session repository tests
  - existing outbox factories and publish fanout source data
- **Description:** Ensure calendar export, AI references, notification/federation source data, and program summaries ignore unscheduled/internal sessions unless explicitly intended.
- **Acceptance Criteria:**
  - [x] Calendar export remains public/published-only.
  - [x] AI reference search remains public-discoverable only.
  - [x] Program summary/public agenda projections do not leak internal sessions.
  - [x] Publish outbox/federation fanout payloads do not derive from hidden sessions.
- **Completion notes (2026-06-25):** Calendar export, program summary, and public agenda projection now use `GetPublicSessionsByEventAsync(...)`, with program/agenda also rejecting non-published/non-public parent events. Public event-list location/language/registration-mode session facets now apply a published/scheduled session predicate whenever the specification includes `PubliclyDiscoverable()`. AI reference search was verified as event-only public-discoverable filtering, and publish outbox/fanout payloads rely on the Phase 3.3 public schedule rollups.
- **Dependencies:** Task 3.1, Task 3.3
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules, cqrs-mediatr-guidelines, dotnet-efcore-guidelines
- **Validation:** Domain/Application/Persistence/API tests

### Phase 4: API Contracts, HAL, And Error Semantics

- **Goal:** Expose lifecycle commands through explicit API contracts with HAL affordances and RFC 7807 errors.
- **Depends on:** Phase 2 and Phase 3.
- **Relevant files:**
  - `Explore.API/Controllers/EventController.cs`
  - `Explore.API/Controllers/EventSessionController.cs`
  - `Explore.API/Hateoas/RouteNames.cs`
  - `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
  - `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
  - `docs/API_CHANGELOG.md`
- **Acceptance criteria:**
  - Writes are `[Authorize]`, classified, route-named, response-typed, and rate/idempotency-aware.
  - HAL links expose only allowed lifecycle actions.
  - ProblemDetails/ValidationProblemDetails are consistent.
  - OpenAPI/client generation impact is documented and tested.
- **Verification:**
  - `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

#### Task 4.1: Add explicit event lifecycle endpoints

- **Type:** modify / create
- **Layer:** API / Application
- **Files:** existing `EventController.cs`, `RouteNames.cs`, new/updated command DTOs
- **Description:** Add or update endpoints for import/archive/cancel/publish/readiness through explicit route names and command contracts.
- **Acceptance Criteria:**
  - [ ] No controller contains business validation logic.
  - [ ] Routes have explicit templates and names.
  - [ ] Writes are authorized and return command validation/conflict ProblemDetails.
  - [ ] `docs/API_CHANGELOG.md` captures breaking changes.
- **Dependencies:** Task 2.3
- **Effort:** L
- **Required Skills/Rules:** api-controllers, auth-patterns, cqrs-mediatr-guidelines
- **Validation:** API integration tests

#### Task 4.2: Add session lifecycle endpoints and HAL links

- **Type:** modify / create
- **Layer:** API / HATEOAS
- **Files:** existing `EventSessionController.cs`, `EventSessionLinkPolicy.cs`, `EventLinkPolicy.cs`, `RouteNames.cs`
- **Description:** Add state transition endpoints and HAL affordances for session draft/schedule/submit/approve/publish/archive as approved.
- **Acceptance Criteria:**
  - [ ] Detail and collection link policies remain separate.
  - [ ] Links use `yield return`.
  - [ ] Authorization-aware links fail closed.
  - [ ] UI can gate actions by `_links` without role checks.
- **Dependencies:** Task 2.4, Task 3.1
- **Effort:** L
- **Required Skills/Rules:** api-hateoas, api-controllers, auth-patterns
- **Validation:** API/HAL integration tests, Blazor client tests if UI affordances are touched

### Phase 5: Documentation, Operations, And Hardening

- **Goal:** Document lifecycle rules, migration behavior, self-hosting implications, and verification evidence.
- **Depends on:** Phases 1-4.
- **Relevant files:** `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `schemas/islamu-event.md`, this dev-doc workstream.
- **Acceptance criteria:**
  - Docs describe DB nullability vs API validation responsibilities.
  - Migration/backfill/rollback caveats are clear for self-hosters.
  - Testing commands and evidence are updated.
  - Dev docs reflect final implemented state.
- **Verification:** docs review plus architecture/context tests if agent context files are changed.

#### Task 5.1: Update canonical docs and schema notes

- **Type:** docs
- **Layer:** Docs / Operations
- **Files:** `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, `schemas/islamu-event.md`
- **Description:** Document session status, nullable schedule semantics, lifecycle command contracts, and policy/readiness behavior.
- **Acceptance Criteria:**
  - [ ] Docs distinguish structural DB requirements from publication requirements.
  - [ ] API changelog names breaking route/DTO/visibility changes.
  - [ ] Schema doc includes new lookup and migration notes.
- **Dependencies:** Phases 1-4
- **Effort:** M
- **Required Skills/Rules:** dev-docs contract, api-controllers, efcore-migrations
- **Validation:** doc review, build

#### Task 5.2: Final verification and dev-doc refresh

- **Type:** test / docs
- **Layer:** Cross-cutting
- **Files:** this plan/context/tasks workstream
- **Description:** Run intent-derived tests, update dev docs with final state, and record remaining risks.
- **Acceptance Criteria:**
  - [ ] Build passes.
  - [ ] Domain/Application/Persistence/API/Architecture tests pass as scoped.
  - [ ] Dev docs match actual implementation.
  - [ ] Remaining work is explicit.
- **Dependencies:** all implementation phases
- **Effort:** M
- **Required Skills/Rules:** source-command-check if invoked by implementer, senior-cto-feedback
- **Validation:** exact test commands in Section 14

## 7. Testing Strategy

| Requirement | Test layer | Likely files |
|---|---|---|
| Event minimal draft remains valid | Application unit | existing `CreateEventRequestValidatorTests` |
| Event publish readiness is policy-aware | Application unit | new/updated readiness evaluator tests |
| Event publish still writes outbox only after readiness | Application unit | existing `PublishEventCommandHandlerTests`, `CreateEventCommandHandlerTests` |
| Import/archive are tolerant but bounded | Application unit/API integration | new command handler tests, new EventController tests |
| EventSessionStatus is seeded and stable | Persistence integration | new lookup/seed test |
| Unscheduled draft sessions persist | Persistence integration | `SchedulingConstraintTests` |
| Partial session schedule is rejected | Persistence integration | `SchedulingConstraintTests` |
| Room overlap ignores unscheduled drafts but blocks scheduled conflicts | Persistence integration | `SchedulingConstraintTests` |
| Published event can own unscheduled draft sessions without public leakage | Domain/Application/API integration | new session draft tests and session visibility tests |
| Public event/session reads hide internal states | API integration | existing `EventVisibilityContractTests`, new session visibility tests |
| HAL exposes lifecycle actions only when allowed | API integration | HATEOAS tests under `Event.API.IntegrationTests/Features/Hateoas` |
| Authorization fails closed | Application/API integration | `AuthorizationBehaviorTests`, `AuthorizationIntegrationTests`, policy parity tests |
| Tenant isolation preserved | Persistence/API integration | tenant isolation and cross-tenant tests |
| Architecture rules preserved | Architecture tests | `Event.Architecture.Tests` |

Minimum commands for full implementation completion:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

If the solution build fails in Blazor WebAssembly `ComputeWasmBuildAssets` on SDK `10.0.300`, first verify `dotnet workload list` includes `wasm-tools`. Install it with `dotnet workload install wasm-tools` using an SDK location the developer can write to, then rerun broad verification. Until then, use the focused lifecycle verification lane from `docs/TESTING.md` for API/Application/HAL-only lifecycle changes.

Do not run solution-level `dotnet test`.

## 8. Documentation, Configuration, And Operations Impact

- `docs/DOMAIN.md`: update persistence-enforced Event/EventSession rules, session status, nullable schedule semantics, and publication readiness ownership.
- `docs/API.md`: update lifecycle endpoint contracts, idempotency/retry expectations, and HAL action behavior.
- `docs/API_CHANGELOG.md`: record any breaking changes to status update, event/session DTOs, public visibility, OpenAPI, or generated client.
- `schemas/islamu-event.md`: document new lookup/status schema and nullable fields.
- `docs/OPERATIONS.md` or `docs/TESTING.md`: document migration/backfill/verification commands if behavior changes are operator-visible.
- Configuration impact is likely limited if policy profiles are hard-coded first. If profiles become tenant/instance configurable in this workstream, update governance setting docs and self-hosting defaults.
- No new external service is required.
- Database migrations are required.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Write endpoints must remain `[Authorize]`.
- Lifecycle commands should implement resource authorization through `[AuthorizeResource]`, `IAuthorizedRequest`, or `ISecureRequest`.
- Import/bot commands should require scoped `events:write` authority for API keys if exposed to machine callers.
- Do not trust client-supplied tenant IDs, status IDs, actor IDs, source trust level, or publication status.
- Public APIs must not leak draft session data under published events.
- HAL affordances must be omitted when state or authorization disallows an action.
- Idempotency should be considered for import and publish/transition commands to protect against retry duplication.
- Outbox messages must only be created for successful public publication transitions.
- Logs/metrics should not include imported raw content, PII, tokens, source secrets, or unbounded descriptions.
- Validation errors should be safe and user-actionable without leaking cross-tenant existence.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

- **Multi-tenancy:** Applicable. Tenant filters must remain active. New policy profile resolution must be tenant-aware and respect instance locks if configurable.
- **Federation:** Applicable. Draft/import/review/session-draft changes must not emit public federation/outbox events. Publication remains the side-effect boundary.
- **Localization:** Applicable but not first slice. Readiness error messages should be machine-coded so localization can be added later.
- **Accessibility:** Not directly applicable unless Blazor UI is updated. If UI is touched, readiness errors must be presented in accessible form and actions gated by HAL.
- **Product:** Highly applicable. This work supports drafts, imports, archives, self-hosted validation flexibility, and conference-style program workflows.

## 11. Observability And Operations

- Add structured logs around lifecycle transition attempts with bounded fields: tenant id, event id, session id, profile, result code, status from/to.
- Consider counters for readiness failures by profile/source/error code. Avoid high-cardinality field paths or raw titles/descriptions as metric tags.
- Publish/import/archive side effects should be observable via outbox status and existing request logs.
- No new health check is needed unless configurable policy storage depends on a new service. Database migration health remains covered by existing readiness.
- Operator-facing failure modes:
  - migration fails due to existing inconsistent schedule data;
  - publish blocked by policy-required fields;
  - import source disabled by instance policy;
  - session publish blocked because parent event is not publishable/public.

## 12. Migration And Compatibility Plan

### Data Migration

1. Add `event_session_statuses` lookup and seed stable rows.
2. Add `event_session_status_id` to `event_sessions`.
3. Backfill existing session statuses:
  - sessions under `Published`/`Completed` events -> `PUBLISHED`;
   - sessions under `Draft` events -> `DRAFT`;
   - sessions under `Cancelled` events -> `CANCELLED`;
   - sessions under `Archived` events -> `ARCHIVED`;
   - ambiguous cases logged in migration notes.
4. Make `event_session_status_id` non-null with FK.
5. Alter session schedule/projection columns nullable.
6. Replace check constraints with conditional versions.
7. Update PostgreSQL exclusion constraint metadata and preflight SQL.
8. Add indexes for `(tenant_id, event_id, event_session_status_id)` and public schedule queries.

### Compatibility

This project is pre-v1, so remove or tighten weak contracts instead of preserving bad shims. However:

- Update `docs/API_CHANGELOG.md` for public API shape or behavior changes.
- Regenerate OpenAPI/generated clients if controller DTOs/routes change.
- Do not keep a generic arbitrary status endpoint as a hidden bypass.
- Existing data must be migrated safely.

### Rollback

Downgrading after unscheduled session drafts exist cannot be safely automatic because old schema requires non-null schedule fields. Migration `Down()` must not silently invent fake times. Acceptable options:

- fail with a clear SQL guard if unscheduled rows exist;
- require manual cleanup/deletion/scheduling before downgrade;
- document that downgrade is unsupported after using unscheduled session drafts.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Draft sessions leak through public session endpoints | Medium | Critical | Add session status filters before/with session draft API | API visibility tests fail | Task 3.1 |
| Nullable schedule breaks room overlap constraints | Medium | High | Conditional constraints and focused PostgreSQL tests | `SchedulingConstraintTests` fail or migration preflight fails | Task 1.2/1.3 |
| Event rollups include draft/unscheduled sessions | Medium | High | Update rollup methods and tests | Incorrect `FirstSessionStartUtc` in tests | Task 3.2 |
| Generic status endpoint bypasses lifecycle policy | High | High | Replace/constrain endpoint, add transition tests | Status update test can archive/publish without policy | Task 2.3/4.1 |
| Policy model becomes an untestable rules engine | Medium | Medium | Controlled profiles and field keys only | Too many arbitrary JSON/property rules | Task 2.1 |
| Migration `Down()` loses unscheduled draft data | Low | High | Guard or document manual cleanup; no silent fake times | Migration review | Task 1.3 |
| Import provenance is under-specified | Medium | Medium | Phase 0 decision and DTO tests | Import accepted without source identity | Task 0.1/2.3 |
| Tenant policy loosening bypasses instance governance | Medium | High | Use existing settings lock model if configurable | Tenant can publish without locked requirements | Task 2.1/2.2 |
| Outbox publishes incomplete imported/draft events | Low | Critical | Keep outbox creation behind publish readiness only | Outbox tests fail | Task 2.2 |

## 14. Success Metrics And Definition Of Done

Functional success:

- Event drafts remain normal `Event` rows with `EventStatus = Draft`.
- Imported and archived events can be stored with tolerant requirements and provenance.
- Publication is blocked by policy-aware readiness until public-quality requirements pass.
- A published event can contain draft/internal sessions that do not appear in public session/event program responses.
- Authorized internal session reads use a separate management route and parent-event `view-management` authorization instead of weakening public session routes.
- Session drafts can be unscheduled without fake schedule data.
- Session publish requires schedule, parent event compatibility, and authorization.

Quality gates:

- No Clean Architecture dependency violations.
- No repository returns DTOs.
- Validators remain manually instantiated.
- HAL links remain authoritative for UI action affordances.
- Tenant isolation tests remain green.
- Migration is reviewable, backfilled, and non-silent on rollback data loss.

Required verification before implementation completion:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Documentation gates:

- Update `docs/DOMAIN.md`, `docs/API.md`, `docs/API_CHANGELOG.md`, and `schemas/islamu-event.md`.
- Keep this plan, context, and tasks updated after every implementation slice.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future agents implementing this plan MUST follow this contract:

1. Before starting any implementation slice, read this plan, `event-lifecycle-validation-policy-context.md`, and `event-lifecycle-validation-policy-tasks.md`.
2. Start from the highest-priority incomplete task unless user instruction overrides it.
3. After completing each meaningful task or discovering new scope, update:
   - this plan if architecture/scope/phases/risks changed;
   - `event-lifecycle-validation-policy-context.md` with current state, decisions, files changed, blockers, validation, and next step;
   - `event-lifecycle-validation-policy-tasks.md` by checking completed items and adding discovered tasks.
4. Do not report "done" unless docs reflect the actual current state.
5. Every implementation summary to the user must include:
   - what was implemented, explained as a developer teaching summary rather than an abstract status line;
   - which architecture/design patterns, libraries, infrastructure components, protocols, and project abstractions were used;
   - which important files/classes/interfaces/handlers/components changed and what each is responsible for;
   - the relevant data/control flow through the implementation;
   - which project conventions or industry best practices were followed and why;
   - what was verified;
   - what remains;
   - what should be worked on next.
6. If validation fails, update context/tasks with the failure, root cause if known, and next recovery action.
7. Before pausing, context reset, handoff, or PR creation, refresh all three dev docs and add/refresh a handoff section.

## 16. Progress Reporting Contract

When an implementation agent finishes a slice, its final response should use:

- **Implemented:** medium-sized developer teaching summary naming patterns, libraries/infrastructure, files/classes, and data/control flow.
- **Verified:** commands and results.
- **Remaining:** explicit incomplete tasks or risks.
- **Next:** next recommended slice.
- **Docs updated:** yes/no with reason.

## 17. Potential Risks & Unknowns

The hardest part is not making columns nullable. The hard part is preventing nullable storage from becoming nullable product semantics everywhere. `EventSession` schedule nullability touches local projection columns, room overlap constraints, event rollups, public program queries, publish readiness, calendar export, outbox payload assumptions, HAL links, and API clients. The safest implementation starts with status and persistence tests, then adds policy-aware application transitions, and only then exposes new API affordances.
