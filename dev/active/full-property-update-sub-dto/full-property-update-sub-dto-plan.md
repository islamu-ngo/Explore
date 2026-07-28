<!-- ABOUTME: Re-baselined implementation plan for consistent partial updates and settings autosave. -->
<!-- ABOUTME: Defines remaining migration slices, exclusions, contracts, tests, and handoff rules. -->

# Full Property Update Sub-DTO Pattern - Implementation Plan

Last Updated: 2026-07-28 Europe/Brussels

## 0. Planning Metadata

- **Request:** Standardize every update-eligible API entity on the existing Event/EventSession grouped partial-update convention and make settings, especially policy toggles, save at the point of change.
- **Task directory:** `dev/active/full-property-update-sub-dto/`
- **Planning status:** Implementation complete at 20/20; final verification is blocked by recorded unrelated concurrent and Docker debt.
- **Compatibility:** Breaking changes are intentional. Do not retain old DTOs, routes, overloads, client methods, or tests.
- **Matched intents:** `add-write-endpoint`, `add-cqrs-handler`, `openapi-contract-change`, `add-hal-link`, `blazor-component-affordance`; `add-ef-migration` only when an update-eligible aggregate lacks required concurrency state.
- **Relevant skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `auth-patterns`, `blazor-ui-conventions`, `blazor-bff-patterns`, `dotnet-efcore-guidelines`.
- **Relevant rules:** `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md`, matching Blazor rules, and `.claude/rules/efcore-migrations.md` when persistence changes.
- **Primary layers:** Application, API, Blazor Client, tests, OpenAPI/generated client, and documentation. Domain/Persistence only where concurrency or an existing invariant requires it.
- **Complexity:** XL. Current scans found 59 `Update*Dto.cs` files, 71 matching update command-handler files, 104 public `PUT`/`PATCH` endpoints across 53 controllers, and 46 update-named tests. The exhaustive inventory assigns every one a final disposition.

## Re-baseline - 2026-07-28 Europe/Brussels

- **Reason:** Tasks 3.4 and 3.5 implementation and verification moved ahead of the active records during context compaction and recovery.
- **What changed:** Task 3.4 completed the seven program/aspect/relationship surfaces. Task 3.5 migrated shared, Event, and EventSession custom-property definitions to grouped PATCH with persisted tenant binding, strong concurrency, presence-aware atomic option replacement, and transactional runtime projection refresh.
- **Plan impact:** Scope and sequencing are unchanged. Tasks 3.4 and 3.5 passed their focused gates; the completed count is 11/20.
- **Remaining work:** Resolve or explicitly waive recorded verification blockers, rerun canonical and phase gates, then close the workstream. No implementation task remains.

## 1. Executive Summary

The repository already has the target entity-update architecture in `UpdateEventDto` and `UpdateEventSessionDto`: route-owned identity, nullable logical groups, `OptionalUpdate<T>` for explicit clear, one MediatR command/handler, manual validation, concurrency, one save, and bounded cache invalidation. Every update-eligible API resource and Application update surface is now individually listed in `full-property-update-sub-dto-inventory.md`; no entity may be omitted because it was hidden behind a family wildcard.

Settings are not all entity patches. `SettingsController` already exposes exact-key and category-batch writes with scope, lock, and validation semantics. Tenant autosave will reuse the exact-key endpoint for independent controls and the batch endpoint only for coupled values. Instance governance sub-resources currently accept complete read DTOs through `PUT`; editable policy sections will receive dedicated partial request DTOs and `PATCH` endpoints so one changed toggle cannot overwrite stale sibling fields.

The UI will save switches and selects immediately. Text inputs will save on blur or a bounded debounce. Secrets, destructive changes, deployment transitions, coupled operations, and actions with external side effects retain explicit submit/confirm flows.

Non-goals:

- converting lifecycle or workflow commands into property updates;
- adding compatibility shims for development-only contracts;
- creating a generic update framework, reflection mapper, or service-per-field abstraction;
- exposing provider-owned, audit, projection, outbox, or credential state through generic PATCH;
- replacing exact-key setting `PUT` routes that already represent complete replacement of one key.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Event is the primary grouped partial-update reference. | `src/Explore.Application/DTOs/Event/UpdateEventDto.cs`; `src/Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs`. | High | Nullable groups and `OptionalUpdate<T>` are already established. |
| EventSession proves grouped updates across relationships and transactional child state. | `src/Explore.Application/DTOs/EventSession/UpdateEventSessionDto.cs`; `src/Explore.Application/Features/EventSessions/Handlers/Commands/UpdateEventSessionCommandHandler.cs`. | High | Use when a group changes related state or projections. |
| The update surface remains broad. | Repository scan: 59 `Update*Dto.cs`; 71 matching update-handler files; 104 public PUT/PATCH endpoints in 53 controllers; 46 update-named tests. | High | All rows are individually registered and assigned to a task. |
| Tenant settings already support one-key and category writes. | `src/Explore.API/Controllers/SettingsController.cs` exposes tenant key and category endpoints backed by `UpdateSettingCommand` and `UpdateSettingBatchCommand`. | High | Reuse instead of adding one endpoint per policy toggle. |
| Instance governance writes can overwrite unchanged sibling values. | `src/Explore.API/Controllers/InstanceSettingsController.cs` accepts full `ModuleSettingsDto`, `EventPolicyDto`, and `OrganizationPolicyDto` through `PUT`. | High | Replace editable policy writes with partial request DTOs. |
| Legacy tenant policy switches do not autosave. | `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantPoliciesSection.razor` uses `@bind-Value` for eight policy switches. | High | Parent layout currently performs broad saves. |
| ATProto controls already demonstrate scoped autosave. | The same component uses `ValueChanged` callbacks and `IAtprotoFederationSettingsService`. | High | Reuse interaction/error patterns, not a new autosave framework. |
| Completed public resources already use route-ID PATCH and concurrency. | User, Actor, Category, Location, LocationRoom, Organization, Group, Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, and selected relationship handlers/controllers. | High | Treat as completed reference surfaces; harden only if final audit finds drift. |

### 2.2 Existing Implementation

- **Application:** CQRS/MediatR commands and handlers own validation, entity loading, authorization, mapping, save, and cache invalidation. Validators are manually instantiated.
- **API:** Public update contracts are mixed between grouped `PATCH`, broad `PUT`, exact-key settings writes, and specialized action endpoints.
- **Blazor:** Admin layouts hold broad section models and bottom Save flows. Some controls, especially ATProto settings, already save through `ValueChanged` callbacks.
- **Persistence:** `IConcurrencyAware`, `IUnitOfWork`, EF concurrency mapping, audit timestamps, and configuration change logs already exist. Reuse them; do not add parallel abstractions.
- **Contracts:** API builds generate OpenAPI; NSwag generates `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`.

### 2.3 Existing Tests And Coverage Gaps

Relevant projects:

- `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj`
- `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj`
- `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`
- `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` when persistence changes
- `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj`

Existing anchors include `UpdateEventSessionCommandHandlerTests`, `UpdateInstanceSubResourceCommandHandlerTests`, `TenantPoliciesSectionTests`, and `TenantSettingsBroadWriteAbsenceTests`. Remaining verification centers on exhaustive semantic exceptions and final detection of public broad update DTOs.

### 2.4 Documentation And Generated Contracts

Implementation must keep these synchronized when affected:

- `docs/API_CHANGELOG.md`
- `docs/API.md`
- `docs/API_CONTRACT_INVENTORY.md`
- `schemas/openapi.json`
- `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- active workstream plan/context/tasks/inventory
- self-hosting or upgrade notes only when a persistence migration changes deployment state

### 2.5 Current Pain Points

- Broad update DTOs allow stale UI state to overwrite unrelated fields.
- `PUT` is used for handlers that apply only selected changes.
- Tenant policy switches appear editable but persist only through a later broad Save.
- Instance policy writes use read DTOs as write contracts.
- Existing settings endpoints and entity updates are conceptually different but were previously grouped into one blanket migration.
- The stale workstream became an implementation diary instead of a forward execution plan.
- The first re-baseline still grouped unnamed resources under phrases such as "other simple CRUD" and therefore did not prove full API coverage.

### 2.6 Unknowns After Investigation

- The current inventory is exhaustive for the 59/71/104 scans. Each phase must rerun those scans and add any newly introduced surface before completion.
- Each settings screen needs classification as independent, coupled, secret, destructive, or provider-owned before changing save behavior. Tasks 1.1-1.3 and 5.2 own this.
- Concurrency additions are decided per aggregate from existing domain configuration; they are not assumed globally.

## 3. Proposed Future State

### Entity And Sub-Resource Updates

- `PATCH /api/{resource}/{id}` for partial entity updates.
- Route ID is authoritative; update bodies contain no entity ID.
- Request DTO contains nullable independently saveable groups.
- Missing group means no change.
- Present group must contain an operation.
- Clearable values use `OptionalUpdate<T>`.
- One command and one handler load once, authorize before mutation, apply explicit groups, save once, then invalidate caches.
- Strong `If-Match` is required where the resource exposes a concurrency stamp.

### Settings Autosave

- Independent tenant setting controls call the existing exact-key setting write.
- Coupled settings use one category batch request.
- Instance policy sub-resources use partial `PATCH` request DTOs rather than full read DTOs.
- Switches and selects save on `ValueChanged`.
- Text inputs save on blur or bounded debounce.
- Controls are disabled while their request is pending; failed writes restore or reload the last server value and show an accessible status/error.
- HAL links and server-provided lock state determine whether a control is editable.
- Explicit Save remains only for secrets, destructive actions, multi-field invariants, and externally consequential operations.

## 4. Non-Negotiable Constraints

- Repositories return entities, never DTOs or `IQueryable`.
- Validators are manually instantiated.
- Writes remain authorized; tenant and instance scope come from trusted server context.
- HAL links are the UI source of truth for affordances; do not infer authorization from claims in components.
- Tenant IDs and resource IDs are never trusted from request bodies.
- Link/junction mutations remain repository-mediated.
- All validation and authorization complete before mutation; mixed payloads fail atomically.
- Multi-repository mutations use existing `IUnitOfWork`.
- Cache invalidation occurs only after successful persistence.
- Sensitive values are not logged or copied into broad audit payloads.
- Old development contracts are removed atomically; no aliases or adapters remain unless required only inside a local UI form model.
- New and touched source files start with two `ABOUTME` lines.

## 5. Architecture And Design Decisions

### Decision 1: Event/EventSession Are The Canonical Entity Pattern

- **Decision:** Reuse their grouped DTO, route-ID command, explicit apply, concurrency, and save flow.
- **Why:** The pattern is implemented and tested; another abstraction would duplicate it.
- **Alternatives considered:** JSON Patch, generic reflection patcher, one command per field.
- **Consequences:** DTOs remain explicit and business-readable; handlers may have several small apply methods.
- **Files/layers affected:** Application DTOs/handlers, API controllers, generated clients, tests.

### Decision 2: Exact-Key Setting PUT Is A Valid Exception

- **Decision:** Keep existing exact-key and category settings endpoints. A single key body is a complete replacement, not an entity partial update.
- **Why:** Reusing the current hierarchy, lock, audit, and validation pipeline is smaller and safer than new policy endpoints.
- **Alternatives considered:** PATCH every key, one endpoint per control, broad tenant policy DTO.
- **Consequences:** Entity update uniformity does not force incorrect HTTP semantics onto setting-key resources.
- **Files/layers affected:** `SettingsController`, settings services, tenant admin components.

### Decision 3: Instance Policies Need Partial Write DTOs

- **Decision:** Replace full read-DTO writes for modules/event/organization policies with dedicated nullable-group request DTOs and `PATCH`.
- **Why:** Autosave must not overwrite stale sibling values.
- **Alternatives considered:** Send the full current model after every toggle.
- **Consequences:** OpenAPI and generated clients break intentionally; handlers apply only present groups.
- **Files/layers affected:** Instance DTOs, commands/handlers, controller, Blazor service/layout, tests.

### Decision 4: Autosave Is Control-Specific, Not A Framework

- **Decision:** Add direct callbacks and minimal shared status state in the owning component/layout.
- **Why:** Existing ATProto controls prove the pattern; a generic autosave engine is unnecessary.
- **Alternatives considered:** reflection-based form tracker, event bus, per-setting component hierarchy.
- **Consequences:** Less infrastructure; repeated code may be extracted only after real duplication appears.
- **Files/layers affected:** Tenant/instance admin components and existing services.

### Decision 5: Keep Domain Actions Explicit

- **Decision:** Secret rotation, provider configuration, publish/archive, role/member changes, approvals, synchronization, purge/reset, and deployment transitions remain dedicated commands.
- **Why:** They have distinct authorization, audit, confirmation, idempotency, or external side effects.
- **Alternatives considered:** Include them as update groups.
- **Consequences:** Final inventory labels them excluded rather than incomplete.
- **Files/layers affected:** Inventory and final architecture assertions; existing action endpoints remain.

### Decision 6: Exhaustive Registers Are Normative Scope

- **Decision:** The 59 DTO rows, 71 handler-file rows, and 104 API endpoint rows in the inventory are mandatory acceptance inputs, not supporting notes.
- **Why:** A family-level plan can silently omit an entity or controller-local request contract.
- **Alternatives considered:** Broad directory tasks with a final search; listing only known noncompliant controllers.
- **Consequences:** Every phase names inventory rows; any new scan result blocks completion until added, classified, and implemented.
- **Files/layers affected:** All implementation phases, architecture/contract tests, plan/context/tasks/inventory.

## 6. Implementation Phases

Every task owns the exact D/H/A rows listed below. A task is incomplete if any owned row is skipped, even when the named family compiles.

### Phase 1: All Settings Contracts And Policy Autosave

- **Goal:** Replace every broad tenant/instance settings write with exact-key replacement or grouped partial PATCH, then prove autosave on policy controls.
- **Depends on:** User approval.
- **Owned API rows:** A-005-A-009, A-026-A-042, A-054, A-068, A-075, A-093-A-097.
- **Acceptance criteria:** Every owned row reaches its inventory disposition; no broad settings DTO remains a write contract; UI save boundaries match control semantics.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Migrate each sub-resource atomically with OpenAPI/client callers; never restore a broad duplicate route.
- **Verification sequencing:** The canonical build and selected Blazor suite passed. On 2026-07-26 the user explicitly deferred the two unavailable Docker-backed Task 1.3 runtime lanes so implementation could continue; those tests remain required before final workstream completion.

#### Task 1.1: Standardize hierarchical and tenant-policy settings writes
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** `SettingsController`, `TenantOnboardingController`, tenant policy handler/DTO/service/component/tests.
- **Description:** Retain and harden exact-key/category PUT rows A-093-A-097, then autosave the eight tenant policy switches through registered keys instead of the broad onboarding write.
- **Acceptance Criteria:** D-038/D-039 and H-007/H-008 remain exact replacements; every independent policy control writes one key and recovers on failure; no policy switch calls the broad onboarding write. H-019/A-003 are removed in Task 5.3 after their remaining non-policy callers migrate.
- **Dependencies:** none.
- **Effort:** L
- **Required Skills/Rules:** Settings hierarchy, Blazor, HAL, auth.

#### Task 1.2: Migrate every tenant settings document/sub-resource
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** tenant branding document, tenant storage settings, tenant footer settings DTOs/handlers/controllers/services/tests.
- **Description:** Implement grouped PATCH and autosave for A-054, A-068, and A-075; preserve lock, concurrency, validation, audit, and cache behavior.
- **Acceptance Criteria:** One group changes without sibling overwrite; old replacement operations are absent; explicit storage validation remains deliberate.
- **Dependencies:** 1.1.
- **Effort:** L
- **Required Skills/Rules:** CQRS, tenant isolation, Blazor, API.

#### Task 1.3: Migrate all instance settings and onboarding duplicates
- **Type:** modify / delete
- **Layer:** Application / API / Blazor
- **Files:** all instance settings DTOs, `UpdateInstanceSubResourceCommand.cs`, `UpdateInstanceSubResourceHandlers.cs`, provider/storage/SMTP/resolver handlers, `InstanceSettingsController`, onboarding controller, service/layout/tests.
- **Description:** Migrate A-005/A-006 and all 17 A-026-A-042 instance settings endpoints to grouped PATCH; remove H-006 and duplicate onboarding writes. Grant dual active-setup-secret or instance-admin authority only to the exact canonical auth/authz GET and PATCH routes, leaving unrelated routes excluded. Route setup-secret traffic through one shared IP-keyed 5-per-60-second quota for exact provider GET/PATCH requests carrying `X-Setup-Secret`; bearer GETs remain outside that bucket and bearer PATCH writes use the per-user Write quota. Apply sparse module capability changes only to the default tenant in `SingleTenant`, preserve resolver cache isolation, and defer every notification produced by the six transaction-owned PATCH handlers until commit succeeds.
- **Acceptance Criteria:** Every named instance sub-resource has a dedicated partial request contract and secret/coupled groups retain explicit UI submission. Only exact `GET`/`PATCH` auth-provider and authz-provider routes accept setup-secret or instance-admin authority; unrelated routes do not gain setup-secret access. Auth/authz GET/PATCH operations expose typed 429; PATCH operations retain `Write` metadata. Existing setup endpoints and exact provider GET/PATCH requests carrying `X-Setup-Secret` share one `setup:{ip}` 5-per-60-second window without duplicate limiter state, while bearer GETs remain outside that bucket. Module sync touches only supplied leaves, runs only for the SingleTenant default tenant, and propagates cancellation. Resolver reads return copies. The six transaction-owned handlers defer value, mixed value-lock, and lock-only notifications until successful commit, with lock transitions sourced as `SystemLocked` or `SystemDefault`.
- **Dependencies:** 1.1.
- **Effort:** XL
- **Required Skills/Rules:** CQRS, auth, security, UoW, Blazor.

### Phase 2: Verify Every Existing Canonical Or Focused Surface

- **Goal:** Prove that every already-migrated entity and focused PATCH/local update surface still meets its assigned contract.
- **Depends on:** Phase 1.
- **Owned API rows:** A-002, A-019, A-022, A-024, A-025, A-043, A-052, A-056, A-060, A-066, A-067, A-070, A-072, A-076.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Fix drift in place; do not add second operations or compatibility overloads.

#### Task 2.1: Verify all public canonical grouped entity PATCH endpoints
- **Type:** investigate / modify
- **Layer:** Application / API / Blazor
- **Files:** User, Actor, Category, Location, LocationRoom, Organization, Group, Event, EventSession, EventAgendaItem, EventDay, EventSeries, EventRegistration, and EventSessionLanguage DTOs/handlers/controllers/HAL/clients/tests.
- **Description:** Check D/H/A canonical rows against the full checklist and repair any drift.
- **Acceptance Criteria:** All 14 named public entity surfaces use route authority, nullable groups, explicit clear, manual validation, concurrency where defined, one save, cache invalidation, HAL, and generated clients.
- **Dependencies:** Phase 1.
- **Effort:** L
- **Required Skills/Rules:** CQRS, API, HAL, concurrency.

#### Task 2.2: Verify all Application-only canonical relationship updates
- **Type:** investigate / modify
- **Layer:** Application
- **Files:** EventCategories and EventTags DTOs/commands/handlers/tests.
- **Description:** Verify D-010/D-011 and H-032/H-033 remain grouped and tenant-safe without inventing public controllers.
- **Acceptance Criteria:** Route-style command identity, concurrency, duplicate prevention, repository-mediated links, and parent cache invalidation remain covered.
- **Dependencies:** 2.1.
- **Effort:** M
- **Required Skills/Rules:** CQRS, repositories, tenant isolation.

#### Task 2.3: Verify the local Event draft update contract
- **Type:** investigate / modify
- **Layer:** Application / API
- **Files:** Local Event draft update DTO/handler/callers/tests.
- **Description:** Keep D-030/H-042 as a local draft workflow and ensure it is not mistaken for an omitted public entity migration.
- **Acceptance Criteria:** Local-only ownership and draft workflow semantics are explicit in tests and inventory.
- **Dependencies:** 2.2.
- **Effort:** S
- **Required Skills/Rules:** API semantics, CQRS.

### Phase 3: Migrate Every Remaining Domain Entity And Definition

- **Goal:** Convert all ordinary entity, relationship, preference-matrix, aspect, template, and definition updates to grouped PATCH.
- **Depends on:** Phase 2.
- **Owned API rows:** A-001, A-010, A-011, A-013, A-014, A-020, A-045, A-046, A-050, A-055, A-058, A-061, A-073, A-074, A-078, A-079, A-084-A-086, A-088-A-090, A-101, A-104.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Keep one resource family per atomic contract/client change; do not combine lifecycle/sync actions into property groups.

#### Task 3.1: Migrate simple tenant/catalog/control-plane entities
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** Tag, Tenant, TenantNavigationLink, FooterLinkGroup, FooterLink, ControlPlaneTenantPlanVersionDraft DTOs/handlers/controllers/HAL/clients/tests.
- **Description:** Implement D-033/D-034/D-036 and H-025/H-050/H-062/H-063 with grouped route-ID PATCH.
- **Acceptance Criteria:** A-011, A-073, A-074, A-085, A-086, A-089 reach `M`; navigation reorder A-087 remains a separate action.
- **Dependencies:** Phase 2.
- **Effort:** L
- **Required Skills/Rules:** CQRS, tenant isolation, HAL.

#### Task 3.2: Migrate all notification preference resources
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** current-user, Organization, Group, and ActorSubscription notification preference DTOs/handlers/controllers/HAL/tests.
- **Description:** Migrate D-028, H-022-H-024/H-046, and A-020/A-050/A-058/A-079 to grouped PATCH while retaining the three exact mute actions.
- **Acceptance Criteria:** Individual matrix cells/groups and actor-subscription notification level update without replacing omitted state; required/locked cells fail atomically; mute rows A-021/A-051/A-080 remain actions.
- **Dependencies:** 3.1.
- **Effort:** L
- **Required Skills/Rules:** CQRS, HAL, authorization.

#### Task 3.3: Migrate every appearance entity/settings surface
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** current-user appearance preferences, AppearanceProfile, UiTheme DTOs/handlers/controllers/services/tests.
- **Description:** Migrate D-027/D-057/D-058, H-069/H-070, and A-045/A-046/A-055 to grouped PATCH.
- **Acceptance Criteria:** Palette, metadata, language/direction/theme preferences, and UI-theme groups preserve omitted values; active profile/mode/archive actions remain separate.
- **Dependencies:** 3.2.
- **Effort:** L
- **Required Skills/Rules:** Blazor, CQRS, concurrency.

#### Task 3.4: Migrate all remaining program/aspect/relationship entities
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** EventLocation disclosure, Event Islamic/Tech aspects, EventSessionAgendaItem, EventSessionGroup, EventSessionSpeaker, TagTypeTags, CategoryTypeCategories DTOs/handlers/controllers/tests.
- **Description:** Migrate D-009/D-035/D-051/D-053/D-056, H-001/H-010/H-011/H-028/H-051/H-068, and A-001/A-010/A-013/A-014/A-061/A-078. Split aspect create from partial update, remove the redundant parent session ID from the speaker update route, and enforce tenant-scoped uniqueness for the two Application-only lookup relationships.
- **Acceptance Criteria:** Every named surface has canonical route-ID grouped PATCH or Application-only grouped command semantics; cross-tenant relationships, duplicates, schedule/privacy, parent cache invariants, and database race safety hold. The migration must fail before index replacement when duplicate lookup relationships exist and must contain no unrelated schema changes.
- **Dependencies:** 3.3.
- **Effort:** XL
- **Required Skills/Rules:** CQRS, privacy, tenant isolation, UoW.

#### Task 3.5: Migrate all custom-property definition entities
- **Type:** modify
- **Layer:** Application / API
- **Files:** CustomPropertyDefinition, EventCustomPropertyDefinition, EventSessionCustomPropertyDefinition DTOs/handlers/controllers/tests.
- **Description:** Migrate D-052/D-054/D-055, H-065-H-067, and A-090/A-101/A-104; retain exact value replacement rows A-091/A-092/A-102/A-103.
- **Acceptance Criteria:** Metadata, validation, option, and relation groups are explicit; option writes are atomic; projection and concurrency behavior remain.
- **Dependencies:** 3.4.
- **Effort:** XL
- **Required Skills/Rules:** CQRS, UoW, EF Core.

#### Task 3.6: Migrate both template entities
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** EventTemplate and EventSessionTemplate DTOs/handlers/controllers/tests.
- **Description:** Migrate D-047-D-050, H-047/H-048, and A-084/A-088 to grouped PATCH while retaining sync/apply workflows as actions.
- **Acceptance Criteria:** Metadata and definition groups preserve omitted state; coupled options remain transactional; audit/concurrency and generated clients are updated.
- **Dependencies:** 3.5.
- **Effort:** XL
- **Required Skills/Rules:** CQRS, UoW, audit.

### Phase 4: Migrate Every Operational Editable Resource

- **Goal:** Convert all remaining operator-editable metadata/policy resources while isolating credentials and external side effects.
- **Depends on:** Phase 3.
- **Owned API rows:** A-007-A-009, A-015-A-017, A-057, A-059, A-100.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** External calls stay outside DB transactions; sensitive values never enter logs or generic audit payloads.

#### Task 4.1: Migrate StorageObject and both webhook resources
- **Type:** modify
- **Layer:** Application / API
- **Files:** StorageObject, WebhookEndpoint, and WebhookConsumerProviderMode DTOs/handlers/controllers/tests.
- **Description:** Migrate D-003/D-004/D-029, H-014/H-015/H-029, and A-015/A-016/A-100.
- **Acceptance Criteria:** Editable metadata/config groups patch atomically; upload/signing/rotation/delivery actions stay separate; secrets are excluded.
- **Dependencies:** Phase 3.
- **Effort:** L
- **Required Skills/Rules:** Security, CQRS, API.

#### Task 4.2: Migrate Listmonk, localization, and external API-key policy
- **Type:** modify
- **Layer:** Application / API / Blazor
- **Files:** Listmonk integration, LocalizationGovernance, ExternalApiKeyPolicy DTOs/handlers/controllers/tests.
- **Description:** Migrate D-005/D-042/D-046, H-018/H-020/H-030, and A-007/A-057/A-059.
- **Acceptance Criteria:** Omitted policy fields remain; provider tests/credential issuance/revoke/rotate remain actions; no key material is returned or logged.
- **Dependencies:** 4.1.
- **Effort:** L
- **Required Skills/Rules:** Security, localization, CQRS.

#### Task 4.3: Migrate every reporting policy/consent resource
- **Type:** modify
- **Layer:** Application / API
- **Files:** report communication consent, reporting provider locks, and reporting routing DTOs/handlers/controllers/tests.
- **Description:** Migrate D-001/D-006/D-007, H-005/H-026/H-027, and A-008/A-009/A-017 to grouped PATCH; separate provider secrets into explicit groups/actions.
- **Acceptance Criteria:** Independent consent/lock/routing groups do not overwrite siblings; privacy/audit rules and concurrency remain; secrets are never exposed.
- **Dependencies:** 4.2.
- **Effort:** L
- **Required Skills/Rules:** Privacy, auth, CQRS.

### Phase 5: Remove Unsafe Generic Writes And Complete UI Autosave

- **Goal:** Remove public generic writes for provider/credential/internal state and complete all tenant/instance autosave UI flows.
- **Depends on:** Phase 4.
- **Owned API rows:** A-003, A-044, A-071, A-077, A-098 plus all UI callers of Phase 1/3/4 settings contracts.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Safe dedicated workflows replace generic CRUD; explicit sensitive/coupled UI actions remain.

#### Task 5.1: Remove generic IndexedDid, SyncState, ActorKeyStore, and UserExternalLogin writes
- **Type:** delete / modify
- **Layer:** Application / API
- **Files:** D-031/D-037/D-044/D-059, H-049/H-052/H-058/H-071, A-044/A-071/A-077/A-098 and related generic create/update HAL/client/tests.
- **Description:** Remove public generic update exposure for provider-owned index/cursor/key/identity rows; retain or add only dedicated sync, rotation, link, or unlink workflows.
- **Acceptance Criteria:** No route accepts tenant IDs, provider keys, private encrypted keys, signing keys, cursors, or provider-owned timestamps through generic update DTOs.
- **Dependencies:** Phase 4.
- **Effort:** L
- **Required Skills/Rules:** Security, auth, Clean Architecture.

#### Task 5.2: Apply autosave to every remaining tenant and instance settings control
- **Type:** modify
- **Layer:** Blazor
- **Files:** all tenant/instance admin settings components/layouts/services/tests.
- **Description:** Classify every control as immediate, blur/debounce, atomic batch, or explicit action and wire it to the Phase 1/3/4 API contract.
- **Acceptance Criteria:** No independent control requires a page-level Save; HAL/locks gate writes; pending/error/saved status is accessible; sensitive/coupled actions remain explicit.
- **Dependencies:** 5.1 and all API migration tasks.
- **Effort:** XL
- **Required Skills/Rules:** Blazor, accessibility, HAL.

#### Task 5.3: Remove obsolete broad Save fan-out and wire DTOs
- **Type:** delete / modify
- **Layer:** Application / API / Blazor
- **Files:** H-019/A-003, tenant/instance layouts, local wire adapters, services, generated client callers/tests.
- **Description:** Delete broad bottom Save methods, the broad tenant onboarding write route/handler, and old generated-client calls after all sections have an explicit save boundary.
- **Acceptance Criteria:** H-019/A-003 are absent; no obsolete full-model wire DTO/client call remains; local form models are allowed only as non-wire state.
- **Dependencies:** 5.2.
- **Effort:** M
- **Required Skills/Rules:** Blazor, generated clients.

### Phase 6: Verify Every Semantic Exception And Enforce Exhaustiveness

- **Goal:** Verify every `A`/`S` row individually, regenerate contracts, and prevent any future unlisted update surface.
- **Depends on:** Phase 5.
- **Owned API rows:** A-004, A-012, A-018, A-021, A-023, A-047-A-049, A-051, A-053, A-062-A-065, A-069, A-080-A-083, A-087, A-091-A-099, A-102/A-103.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Exception assertions use exact route names and rationale; no wildcard allowlist.

#### Task 6.1: Verify all exact-replacement and action endpoints
- **Type:** investigate / modify
- **Layer:** Application / API / Tests
- **Files:** every owned A-row plus D/H `A`, `S`, and `N` rows in the inventory.
- **Description:** Confirm onboarding progress, exact settings, role/approval/mute/selection/archive, notification state transitions, email controls, reorder, custom-property values, upload content, and local/Application-only surfaces are semantically not entity partial updates.
- **Acceptance Criteria:** Every exception has route-specific tests and rationale; any endpoint found to mutate independent entity properties is reclassified to `M` and implemented before completion.
- **Dependencies:** Phase 5.
- **Effort:** L
- **Required Skills/Rules:** API semantics, auth, architecture tests.

#### Task 6.2: Add exhaustive contract guards and finalize artifacts
- **Type:** modify
- **Layer:** API / Blazor / Tests / Docs
- **Files:** architecture/API contract tests, HAL/routes, OpenAPI, generated client, API changelog/inventory, all four active workstream docs.
- **Description:** Enforce exact coverage of D-001-D-059, H-001-H-071, and A-001-A-104; fail when a new update surface is absent or a migrated broad operation returns.
- **Acceptance Criteria:** Every `M` row is implemented; every `C` row passes the canonical checklist; every `R` generic route is absent; every `A`/`S` exception is exact; generated client compiles; HAL and docs match.
- **Dependencies:** 6.1.
- **Effort:** L
- **Required Skills/Rules:** Architecture tests, API/OpenAPI, HAL, documentation.

## 7. Testing Strategy

- Handler tests cover empty/no-op groups, one group, multiple groups, validation/auth/concurrency failure before save, one save, and post-save cache invalidation.
- API tests cover authorization, route-ID authority, PATCH verb, removed old operations, `If-Match` where applicable, and ProblemDetails.
- Blazor tests cover immediate save, debounce/blur, per-control pending state, failure restore/reload, locked affordances, and explicit-action retention.
- Persistence tests are added and selected for a phase only if that phase changes EF concurrency or transaction behavior.
- Each phase runs one Release build and one selected non-browser test project once after all phase tasks.

## 8. Documentation, Configuration, And Operations Impact

- Update API changelog/inventory and generated OpenAPI/client with every public contract batch.
- Update `docs/API.md` only where update semantics or setting endpoint guidance changes.
- Update self-hosting/upgrade docs only if a migration adds or changes persisted concurrency state.
- No new environment variables, Compose services, Aspire resources, packages, or background jobs are planned.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Controllers remain `[Authorize]`; handlers retain resource and group-level authorization.
- Tenant/instance scope and route IDs come from trusted request context/route values.
- HAL and server lock metadata gate UI affordances.
- Autosave does not bypass rate limiting, validation, idempotency, audit, or concurrency.
- Secret values, provider tokens, signing material, PII, and raw policy payloads are not logged.
- Rapid user changes are serialized or superseded per control to avoid stale response ordering.

## 10. Cross-Cutting Product Considerations

- **Multi-tenancy:** Applicable. Scope resolution, lock inheritance, cross-tenant relationship checks, and tenant-aware cache invalidation remain mandatory.
- **Federation:** Applicable only to editable federation policy. Provider-owned records/keys remain excluded.
- **Localization:** Applicable. Localized text follows explicit clear semantics and saves on blur/debounce.
- **Accessibility:** Applicable. Pending, saved, failure, lock, and disabled states need text/status semantics; color alone is insufficient.
- **Product:** Applicable. Immediate controls should feel immediate, while secrets and consequential actions remain deliberate.

## 11. Observability And Operations

- Reuse structured command logging and configuration change logs.
- Log resource/group/key identifiers and outcomes, not values.
- Preserve stable ProblemDetails for validation, authorization, lock, and concurrency failures.
- No new health checks or metrics are required unless implementation reveals measurable autosave failure ambiguity.

## 12. Migration And Compatibility Plan

- Remove old public broad DTOs/operations and regenerate clients atomically per phase.
- Local Blazor form models may remain when they are not wire contracts.
- No API compatibility aliases, duplicate verbs, or generated-client overloads.
- Add EF migrations only when the selected aggregate requires a missing concurrency token; document reset/upgrade impact in that phase.
- Existing exact-key setting PUT routes remain by design, not as compatibility code.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Autosave overwrites sibling settings. | Medium | High | Exact-key writes and partial instance DTOs. | Request body includes unchanged siblings. | 1.1-1.3 |
| Out-of-order autosave responses restore stale values. | Medium | Medium | Per-control pending/sequence handling and server reload on failure. | Fast toggle test ends on old state. | 1.1, 1.3, 5.x |
| Blanket PATCH migration captures actions/secrets. | Medium | High | Inventory dispositions and explicit exclusions. | Secret/lifecycle field appears in generic DTO. | all, 6.1 |
| HAL/lock enforcement is replaced with local claims. | Low | High | Preserve `_links` and server lock metadata. | UI enables write without affordance. | 1.x, 5.x |
| Generated contracts drift. | Medium | High | Regenerate per public batch and final guard. | Client compile or contract inventory mismatch. | 2.x-6.2 |
| Scope becomes an unreviewable big bang. | Medium | High | Six independently verifiable phases. | One phase spans unrelated resource families. | plan/tasks |
| A new update surface bypasses the plan. | Medium | High | Exact count/identity assertions for all three registers. | DTO, handler, or endpoint scan differs from inventory. | 6.2 |

## 14. Success Metrics And Definition Of Done

- D-001-D-059, H-001-H-071, and A-001-A-104 all reach their assigned final state.
- Every public update-eligible entity uses the Event/EventSession grouped route-ID PATCH pattern; only individually named exact replacements and actions retain other semantics.
- Architecture/contract tests fail if a new update DTO, handler file, or PUT/PATCH endpoint is not registered.
- Tenant and ordinary instance settings save at the correct interaction boundary without stale sibling overwrite.
- Secrets, destructive actions, coupled invariants, and lifecycle transitions remain explicit commands; unsafe generic provider-owned writes are removed.
- HAL, authorization, tenant isolation, audit, concurrency, transaction, and cache behavior remain tested.
- Old development contracts are absent from OpenAPI and generated clients.
- All six phase gates pass and the active workstream matches repository reality.

## 15. Implementation Agent Contract - KEEP DEV DOCS CURRENT

1. On first start, read all four artifacts. On resume, read context/tasks first and only the relevant plan/inventory sections.
2. Start from the highest-priority unchecked task unless the user overrides it.
3. Treat `tasks.md` as the hot ledger; check substantial tasks immediately and reconcile small tasks by phase end.
4. Keep implementation completion separate from phase verification.
5. Update context for decisions, blockers, failed validation, discoveries, phase completion, or handoff.
6. Update this plan only when scope, architecture, sequencing, acceptance, risk, or validation changes.
7. Do not reread or rewrite unchanged artifacts after every task.
8. Run phase verification once after all phase tasks: one Release build and one selected test project.
9. Never touch unrelated dirty files and record them in handoffs when relevant.
10. Never claim completion while code, generated contracts, inventory, and task ledger disagree.

## 16. Progress Reporting Contract

- **Implemented:** Explain DTO groups, route/settings semantics, handler/control flow, authorization, concurrency/transaction, cache/audit, and UI behavior.
- **Verified:** List the exact phase build and selected test command.
- **Remaining:** State incomplete or deliberately excluded surfaces.
- **Next:** Name the next unchecked task.
- **Docs updated:** Confirm tasks reconciliation; state whether context/plan/inventory changed and why.

## 17. Potential Risks And Unknowns

The hardest boundary is not DTO shape but classification: settings keys, editable sub-resources, domain actions, secrets, and provider-owned state currently share “Update” naming. The implementation must keep the Event-style contract narrow, reuse settings infrastructure where it already fits, and reject the temptation to make every update-shaped command a generic PATCH.
