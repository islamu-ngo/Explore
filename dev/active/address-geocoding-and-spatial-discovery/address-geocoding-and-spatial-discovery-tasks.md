<!-- ABOUTME: Test-first execution ledger for governed local and optional Photon address acquisition. -->
<!-- ABOUTME: Tracks Red/Green tasks, dependencies, phase gates, blockers, and the spatial-discovery handoff. -->

# Address Geocoding And Spatial Discovery - Task Checklist

Last Updated: 2026-08-25 Europe/Brussels

## Status Summary

- **Overall status:** Phase 1 complete; Phase 2 Tasks 2.1-2.2 independently verified in the task-owned isolated worktree.
- **Completed:** 6/19 implementation tasks (phase verification tracked separately).
- **Current priority:** Task 2.3, effective governance policy and SQL-first local suggestion query.
- **Next recommended slice:** Complete Phase 1 only.
- **I-VSD:** [I-VSD Address Geocoding And Spatial Discovery](../../../islamic-value-sensitive-design/i-vsd-address-geocoding-and-spatial-discovery.md)
- **Spatial ownership:** Exact PostGIS work is deferred to `dev/active/home-discovery-experience/` Phase 6; no spatial runtime task exists here.
- **Context7:** Attempted on 2026-08-25 but unavailable. Use the official-document substitution recorded in the plan/context and retry before dependency adoption if available.

## Implementation Maintenance Rules

- Read context and the current task first; read only the referenced plan heading.
- Keep exactly one task in progress.
- A behavioral Green task cannot start until its named Red task failed for the expected missing behavior and the evidence is recorded.
- Use the same targeted TUnit command for Red and Green. Fixed sleeps, polling delays, and timing-luck assertions are forbidden.
- Check a substantial task immediately when its acceptance criteria pass; reconcile smaller tasks no later than phase end.
- Keep implementation completion separate from phase verification.
- Run one Release build and at most one full project test only after all phase tasks.
- Update context after a phase, decision, blocker, failed validation, material discovery, or handoff.
- Update the plan only when scope, architecture, sequence, acceptance, risk, or verification changes.
- Generate migrations, snapshots, OpenAPI, inventory, and NSwag client; never hand-edit them.
- Preserve unrelated shared-worktree changes.
- Never add compatibility aliases, dual coordinate shapes, client role checks, direct browser provider calls, or PostGIS/map work from this ledger.
- Never create a git commit unless the user separately authorizes it.

## Phase 1: Location Integrity And Complete Write-Contract Contraction - NOT STARTED

- [x] **1.1 Red Phase - Lock Coordinate Write And Aggregate Invariants**
  - **Files:**
    - `tests/Event.Application.UnitTests/Features/Locations/Commands/LocationAddressWriteContractTests.cs` (new)
    - `tests/Event.Application.UnitTests/Features/Events/Commands/CreateEventLocationWriteContractTests.cs` (new)
    - `tests/Event.Application.UnitTests/Features/AiAssistant/Actions/CreateEventDraftLocationWriteContractTests.cs` (new)
    - `tests/Event.Architecture.Tests/CoordinateWriteAuthorityArchitectureTests.cs` (new)
  - **Description:** Author failing public-contract tests before production edits. Cover direct Location create/PATCH, nested Event location creation, AI draft location mapping, complete finite coordinate pairs, manual stale-coordinate clearing, real create construction, private-home consent/erasure preservation, trusted tenant mismatch, and a structural ratchet against new untrusted coordinate write DTOs.
  - **Red commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*LocationAddressWriteContractTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*CreateEventLocationWriteContractTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*CreateEventDraftLocationWriteContractTests/*"`
    - `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj -- --treenode-filter "/*/*/*CoordinateWriteAuthorityArchitectureTests/*"`
  - **Acceptance:**
    - [x] Tests fail because raw coordinate write members/paths still exist or atomic transitions are missing.
    - [x] Failure is behavior/contract-specific, not a broken fixture or nondeterministic timeout.
    - [x] Authorized read/disclosure coordinate DTOs are explicitly excluded from the contraction assertion.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding` at `c2000922b`; 10 Location, 2 nested Event and 3 AI tests failed only on intended contract assertions; architecture controls passed 3/3 and the ratchet reported the expected 14 write-authority symbols; four files were LSP-clean and warning-free.
  - **Effort:** L
  - **Dependencies:** User approval of Phase 1.
  - **Guidance:** Plan Decisions 1-2; `criticality-guardrail`; Application/Domain/test rules.

- [x] **1.2 Green Phase - Implement Atomic Location Address Transitions**
  - **Files:**
    - `src/Explore.Domain/Location.cs` (existing)
    - `src/Explore.Domain/LocationPii.cs` (existing)
    - `src/Explore.Application/DTOs/Location/CreateLocationDto.cs` (existing)
    - `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs` (existing)
    - Location validators/requests/handlers and `LookupMappingProfile.cs` (existing)
    - Existing Location command/privacy tests plus Task 1.1 tests
  - **Description:** Replace flattened create mapping and independent coordinate mutation with explicit aggregate construction and manual/provider address transitions. Manual changes clear coordinates. Provider transition requires a complete finite pair. Remove direct Location raw coordinate write members. Preserve erasure, consent, concurrency and authorized coordinate reads.
  - **Green commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*LocationAddressWriteContractTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*CreateLocationCommandHandlerTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*UpdateLocationCommandHandlerTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*PrivateHomeOwnershipCommandHandlerTests/*"`
  - **Acceptance:**
    - [x] Real construction no longer depends on proxy-setter AutoMapper behavior.
    - [x] No partial/non-finite coordinate state is constructible through the public aggregate write API.
    - [x] Manual address mutation clears stale coordinates/provider selection.
    - [x] Request tenant/context mismatch fails closed; body tenancy remains absent.
    - [x] Private-home anti-resurrection and consent tests remain green.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding`; 158 focused/legacy/persistence/API tests passed with 0 failures, seven affected Release builds passed with 0 warnings/errors, all 12 changed production files were LSP-clean, and `git diff --check` plus generated/migration scope checks were clean.
  - **Effort:** L
  - **Dependencies:** 1.1 Red evidence.
  - **Guidance:** Plan Section 3.1; `clean-architecture-rules`; `cqrs-mediatr-guidelines`.

- [x] **1.3 Green Phase - Contract Nested Event And AI Location Writes**
  - **Files:**
    - `src/Explore.Application/DTOs/Event/CreateEventLocationDto.cs` (existing)
    - `src/Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs` (existing)
    - `src/Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` (existing)
    - `src/Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionPayload.cs` (existing)
    - `src/Explore.Application/Features/AiAssistant/Actions/CreateEventDraftAiActionMapper.cs` (existing)
    - Existing Event/AI tests plus Task 1.1 tests
  - **Description:** Remove model/browser coordinate authority from nested Event and AI draft paths. Route nested creation through governed manual address semantics now and the protected-selection abstraction when it exists. Do not change authorized disclosure/read contracts.
  - **Green commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*CreateEventLocationWriteContractTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*CreateEventDraftLocationWriteContractTests/*"`
  - **Acceptance:**
    - [x] No nested Event or AI draft write payload carries latitude/longitude.
    - [x] Manual nested creation stores no coordinates.
    - [x] AI/model output cannot manufacture trusted coordinates.
    - [x] Existing EventLocation disclosure paths remain unchanged.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding`; 74 focused/existing Event and AI tests passed, both affected Release builds passed with 0 warnings/errors, changed Task 1.3 files were LSP-clean, and the architecture ratchet reported only six generated-client write members owned by Task 1.4 while all authorized-read controls passed.
  - **Effort:** L
  - **Dependencies:** 1.2.
  - **Guidance:** Plan Decision 1; AI/privacy disclosure guards.

- [x] **1.4 Refactor Phase - Regenerate Contracts And Ratchet Boundaries**
  - **Files:** `LocationController.cs`, affected Event/AI API surfaces, `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `EventApiClient.g.cs`, affected services/dialogs/tests, `docs/API.md`, `docs/API_CHANGELOG.md` (existing/generated)
  - **Description:** Regenerate one breaking contract after every write path is current. Preserve operation IDs and private-home operations. Delete obsolete coordinate input UI/client code directly. Add the architecture ratchet without pinning prose.
  - **Target commands:**
    - `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj -- --treenode-filter "/*/*/*CoordinateWriteAuthorityArchitectureTests/*"`
    - `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj -- --treenode-filter "/*/*/*EventLocationDisclosureConvergenceTests/*"`
    - `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationControllerTests/*"`
  - **Acceptance:**
    - [x] OpenAPI/inventory/client are generated and contain no write-coordinate members or aliases.
    - [x] Authorized disclosure read fields remain governed and present where intended.
    - [x] Private-home routes, operation IDs, `If-Match`, and consent semantics are unchanged.
    - [x] `docs/API_CHANGELOG.md` records the direct breaking change and affected consumers.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding`; canonical API/inventory/NSwag regeneration was hash-stable, the generated write graph contains no coordinate member while authorized reads retain coordinates, architecture/disclosure/API/private-home gates passed, 11 focused deterministic dialog/validator tests passed, and affected API/Blazor/architecture projects compiled. Razor LSP is unavailable; compiled Razor and focused bUnit tests are the verification substitute.
  - **Effort:** L
  - **Dependencies:** 1.2, 1.3.
  - **Guidance:** `openapi-contract-change`; API controller and generated-artifact rules.

### Phase 1 Verification - RUN ONCE AFTER TASKS 1.1-1.4

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Governed Persistence And Local-Only Acquisition - IN PROGRESS

- [x] **2.1 Red Phase - Specify Conservative Address State And Isolation**
  - **Files:**
    - `tests/Event.Application.UnitTests/Features/Locations/AddressGovernancePolicyTests.cs` (new)
    - `tests/Event.Persistence.IntegrationTests/Repositories/LocalAddressSuggestionQueryTests.cs` (new)
    - `tests/Event.Persistence.IntegrationTests/Migrations/LocationAddressGovernanceMigrationTests.cs` (new)
  - **Description:** Author failing tests for source/visibility independence, `UnknownLegacy+Quarantined`, Private Home non-promotion, creator/organization/tenant-approved scope, user-scope non-escalation, missing organization/tenant failure, SQL-before-PII filtering, idempotent/concurrent promotion, and current provider migration heads.
  - **Red commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*AddressGovernancePolicyTests/*"`
    - `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocalAddressSuggestionQueryTests/*"`
    - `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressGovernanceMigrationTests/*"`
  - **Acceptance:**
    - [x] Tests fail for the missing governance state/query/migration behavior.
    - [x] PostgreSQL and SQLite cases are real relational executions.
    - [x] SQL Server/MariaDB/MySQL are labeled model/migration parity unless real lanes are added.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding`; independently reproduced `AddressGovernancePolicyTests` 1/33 passing, `LocalAddressSuggestionQueryTests` 2/6 passing, and `LocationAddressGovernanceMigrationTests` 1/13 passing. All 48 failures are intentional missing Task 2.2-2.4 contracts; PostgreSQL and SQLite relational controls passed, all three files were LSP-clean, and no production/generated artifact changed.
  - **Effort:** L
  - **Dependencies:** Phase 1 complete.
  - **Guidance:** Plan Decisions 3-4; `dotnet-efcore-guidelines`; migration/test rules.

- [x] **2.2 Green Phase - Persist Source Visibility And Organization Scope**
  - **Files:** `Location.cs`; new lookup enums/entities/configuration/seeding; Location configuration; all five generated provider migrations/snapshots; migration tests
  - **Description:** Add independent source and visibility state plus nullable organization scope. Reuse `CreatedBy`. Introduce `UnknownLegacy`, `Manual`, provider-selected source semantics and `Quarantined`, creator, organization, tenant-approved visibility. Generate migrations from the then-current immutable head.
  - **Green command:** `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressGovernanceMigrationTests/*"`
  - **Acceptance:**
    - [x] Existing rows are `UnknownLegacy+Quarantined`, never guessed or tenant-reusable.
    - [x] Source and visibility cannot overwrite each other.
    - [x] Every non-approved current row has one conservative private scope.
    - [x] Private Home cannot be `TenantApproved`.
    - [x] Five provider snapshots/migrations are generated and spatial/provider-transport free.
  - **Evidence:** `/home/amir/ISLAMU/Github/Event-address-geocoding`; independently confirmed domain 5/5, privacy lifecycle 10/10, lookup seeder 3/3, generator operation 4/4, monetary backfill 1/1, and migration/provider parity 13/13. PostgreSQL and SQLite executed the generated governance head against dynamic predecessor models; all five generated heads/snapshots are pending-model clean with exact seed ordering, constraints, FKs, and no unrelated operations.
  - **Effort:** XL
  - **Dependencies:** 2.1 Red evidence.
  - **Guidance:** EF migration generation invariant; lookup seeding parity.

- [ ] **2.3 Green Phase - Implement Effective Policy And Local Query**
  - **Files:**
    - existing governance keys/setting definitions and new typed address policy group
    - new Application policy contracts/resolver
    - new `ILocalAddressSuggestionQuery` and Application result models
    - new Persistence query implementation
    - Task 2.1 tests
  - **Description:** Reuse the hierarchical settings engine and named server authorization. Implement bounded no-tracking local search with tenant plus visibility predicates before exact PII projection. User scope cannot loosen policy. Missing tenant/organization/settings fail closed.
  - **Green commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*AddressGovernancePolicyTests/*"`
    - `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocalAddressSuggestionQueryTests/*"`
  - **Acceptance:**
    - [ ] `Disabled`, `AdminOnly`, `OrganizationGoverned`, and `OpenWithModeration` resolve exactly as the plan.
    - [ ] Cross-tenant/organization/creator and Private Home rows are absent before mapping.
    - [ ] Quarantined rows never autocomplete.
    - [ ] Query is bounded, deterministic, cancellable, no-tracking, and has no N+1/in-memory auth filter.
  - **Effort:** L
  - **Dependencies:** 2.2.
  - **Guidance:** `IHierarchicalSettingsResolver`; Application/Persistence rules.

- [ ] **2.4 Green Phase - Implement Safe Promotion And Migration Guidance**
  - **Files:** new promotion command/validator/handler, authorization action, persistence update path, Application/Persistence tests, `DOMAIN.md`, `AUTHORIZATION.md`, `MULTI_TENANCY.md`, migration/operator docs
  - **Description:** Add explicit moderation from quarantined/private scope to eligible scope. Promotion changes visibility only, requires concurrency plus named authorization, and never permits Private Home tenant-wide reuse. Document reset vs retained-data handling without compatibility code.
  - **Green commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*AddressGovernancePolicyTests/*"`
    - `dotnet run --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocalAddressSuggestionQueryTests/*"`
  - **Acceptance:**
    - [ ] Promotion is idempotent/concurrency-safe and tenant-bound.
    - [ ] Source/provenance does not change.
    - [ ] Private Home and erased/foreign rows fail closed.
    - [ ] Operator docs explain quarantined legacy review and generated migration flow.
  - **Effort:** M
  - **Dependencies:** 2.3.
  - **Guidance:** Plan Section 3.2; authorization/HAL authority remains server-side.

### Phase 2 Verification - RUN ONCE AFTER TASKS 2.1-2.4

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Local-Only API, HAL, BFF, Generated Client And Blazor - NOT STARTED

- [ ] **3.1 Red Phase - Specify Private Local Address API And BFF**
  - **Files:**
    - `tests/Event.Application.UnitTests/Features/Locations/Queries/SearchLocalAddressesRequestHandlerTests.cs` (new)
    - `tests/Event.Application.UnitTests/Telemetry/GeocodingMetricsTests.cs` (new)
    - `tests/Event.API.IntegrationTests/Features/LocationAddressSuggestionsControllerTests.cs` (new)
    - `tests/Event.API.IntegrationTests/Features/Hateoas/LocationAddressHateoasTests.cs` (new)
    - `tests/Explore.Blazor.IntegrationTests/Endpoints/BffAddressAcquisitionProxyTests.cs` (new)
  - **Description:** Author failing handler, telemetry, authenticated API/HAL and BFF tests for pre-Persistence validation, trusted tenant/organization rejection, authorized result shaping, bounded `provider=none` instruments, local-only suggestions/manual policy/promotion, 401/403, tenant spoofing, short/oversized input, rate limiting, private/no-store, RFC 7807, HAL omission, Private Home isolation, provider absence, antiforgery and trusted BFF transforms.
  - **Red commands:**
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*SearchLocalAddressesRequestHandlerTests/*|/*/*/*GeocodingMetricsTests/*"`
    - `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressSuggestionsControllerTests/*|/*/*/*LocationAddressHateoasTests/*"`
    - `dotnet run --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj -- --treenode-filter "/*/*/*BffAddressAcquisitionProxyTests/*"`
  - **Acceptance:**
    - [ ] Handler tests fail because pre-Persistence validation, trusted-scope rejection and authorized result shaping are missing.
    - [ ] Metrics tests fail because the local-only `Explore.Geocoding` meter and strict label allowlist are missing.
    - [ ] API/HAL/BFF tests fail because private local acquisition contracts are missing.
    - [ ] BFF test proves existing `/api/*` forwarding boundary rather than requesting a new endpoint.
    - [ ] Captured metrics/errors/logs contain no address/query/tenant/location payload.
  - **Effort:** L
  - **Dependencies:** Phase 2 complete.
  - **Guidance:** API controller, auth trust-boundary, BFF and HAL rules.

- [ ] **3.2 Green Phase - Implement Local Address Application Flow**
  - **Files:** new local suggestion DTO/validator/query/handler; effective policy result/query; promotion request integration; new `src/Explore.Application/Telemetry/GeocodingMetrics.cs`; Application tests
  - **Description:** Manually validate, resolve trusted context/policy, query only eligible local rows, map bounded results and explicit outcomes, expose manual/promotion capability semantics, and emit bounded local-only `provider=none` metrics. Do not add concrete provider configuration to Application.
  - **Green command:** `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*AddressGovernancePolicyTests/*|/*/*/*SearchLocalAddressesRequestHandlerTests/*|/*/*/*GeocodingMetricsTests/*"`
  - **Acceptance:**
    - [ ] Short/oversized input fails before Persistence.
    - [ ] Missing/mismatched tenant/organization fails closed.
    - [ ] Results contain only source/visibility/display data required by the authorized editor.
    - [ ] Provider absence is a normal local-only outcome.
    - [ ] Local-only request/duration/rate-limit/rejection metrics use only the plan's bounded labels and never sensitive identifiers or payloads.
  - **Effort:** M
  - **Dependencies:** 3.1 Red evidence.
  - **Guidance:** CQRS/manual validation; no provider types in Application.

- [ ] **3.3 Green Phase - Publish API HAL Generated Client And BFF Proof**
  - **Files:** new capability-partitioned address controllers; RouteNames; rate limiting; HAL policies/assemblers; generated OpenAPI/inventory/client; API/BFF tests; API/security/testing docs
  - **Description:** Add private POST suggestions and authorized promotion/status operations. Controllers only dispatch/map/assemble. Publish local/manual/moderation HAL based on executable server state. Reuse existing BFF; regenerate current contracts.
  - **Green commands:**
    - `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressSuggestionsControllerTests/*|/*/*/*LocationAddressHateoasTests/*"`
    - `dotnet run --project tests/Explore.Blazor.IntegrationTests/Explore.Blazor.IntegrationTests.csproj -- --treenode-filter "/*/*/*BffAddressAcquisitionProxyTests/*"`
  - **Acceptance:**
    - [ ] Stable named operations, classifications, ProblemDetails and private/no-store are generated.
    - [ ] HAL omits denied/unready actions and advertises exact URL/method when present.
    - [ ] BFF antiforgery, header stripping and trusted enrichment pass with no bespoke route.
    - [ ] Generated client contains only current provider-free shapes.
  - **Effort:** L
  - **Dependencies:** 3.2.
  - **Guidance:** API/HAL/BFF rules; OpenAPI regeneration order.

- [ ] **3.4 Red Phase - Specify Accessible HAL-Governed Address UI**
  - **Files:**
    - `tests/Explore.Blazor.Client.Tests/Components/Locations/AddressAutocompleteTests.cs` (new)
    - `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs` (new)
    - existing `LocationsTests.cs`, `LocationServiceTests.cs`, `EventLocationPrivacyAccessibilityTests.cs`
  - **Description:** Author failing component/service tests for local combobox semantics, keyboard/focus/live status, cancellation/latest-request-wins, bounded results, source/visibility labels, provider-absent state, HAL-only manual/promotion actions, RTL/localization/reduced motion and private-home consent preservation.
  - **Red command:** `dotnet run --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj -- --treenode-filter "/*/*/*AddressAutocompleteTests/*|/*/*/*LocationDialogTests/*|/*/*/*LocationsTests/*"`
  - **Acceptance:**
    - [ ] Tests fail for missing accessible local-only UI/HAL behavior.
    - [ ] Async tests subscribe before trigger and await exact state with bounded timeout.
    - [ ] No test pins prose wording.
  - **Effort:** L
  - **Dependencies:** 3.3.
  - **Guidance:** Accessibility, Blazor UI and CSS-isolation skills.

- [ ] **3.5 Green Phase - Implement Local Address Administration UI**
  - **Files:** new `AddressAutocomplete.razor`/CSS; existing create/edit dialogs; LocationService; TenantLookupTablesSection; Blazor docs/tests
  - **Description:** Build local-only accessible autocomplete, integrate manual create/edit without coordinates, preserve private-home consent, and gate create/edit/delete/search/promotion only through HAL.
  - **Green command:** `dotnet run --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj -- --treenode-filter "/*/*/*AddressAutocompleteTests/*|/*/*/*LocationDialogTests/*|/*/*/*LocationsTests/*|/*/*/*EventLocationPrivacyAccessibilityTests/*"`
  - **Acceptance:**
    - [ ] Keyboard/screen-reader/RTL users can search/select/clear/manual-enter.
    - [ ] Latest request wins; disposal cancels pending work without sleeps.
    - [ ] Missing HAL removes the exact control; no role/claim/provider policy check exists.
    - [ ] Edit retains the separate private-home consent action.
    - [ ] Complete no-provider/no-map behavior is usable.
  - **Effort:** L
  - **Dependencies:** 3.4 Red evidence.
  - **Guidance:** Plan Phase 3; MudBlazor v9; HAL invariant.

### Phase 3 Verification - RUN ONCE AFTER TASKS 3.1-3.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Optional Photon Adapter, Protected Selections And Release Closure - DECISION GATED

- [ ] **4.1 Gate - Approve Photon Topology Terms And Provenance**
  - **Files:** clean-room source/dependency record; `.env.example`; `CONFIGURATION.md`, `SECRETS.md`, `SELF_HOSTING.md`, `OPERATIONS.md`; AppHost/Compose only if operator-pulled local profile is approved
  - **Description:** Record endpoint ownership, regional/planet data footprint, capacity, update/swap, support, backup/rebuild, TLS, terms, attribution and recovery. Public `photon.komoot.io` cannot be an implicit production/default endpoint.
  - **Acceptance:**
    - [ ] Official source register and source-free functional handoff are complete.
    - [ ] Provider/service/data licenses and outbound-distribution boundary are documented.
    - [ ] Production endpoint, capacity and recovery owner are named.
    - [ ] `Provider=None` remains the default and complete local-only behavior is unaffected.
  - **Effort:** L
  - **Dependencies:** Phase 3; operator/legal-distribution approval where required.
  - **Guidance:** `ip-clean-room`; dependency/service license gate; no source ingestion.

- [ ] **4.2 Red Phase - Specify Photon Resilience Token And Telemetry**
  - **Files:**
    - `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/PhotonAddressGeocoderTests.cs` (new)
    - `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/AddressSelectionProtectorTests.cs` (new)
    - `tests/Explore.Infrastructure.Tests/Telemetry/PhotonGeocodingTelemetryTests.cs` (new)
    - `tests/Event.Application.UnitTests/Telemetry/GeocodingMetricsTests.cs` (existing/extend)
    - `tests/Event.Architecture.Tests/GeocodingBoundaryArchitectureTests.cs` (new)
  - **Description:** Author failing deterministic tests for absence/success/malformed/cancel/timeout/transient/permanent status/circuit, one resilience handler, token tenant/actor/organization/purpose/target/concurrency/provider/config/time binding, coordinate opacity, zero-PII instruments and no local-data upstream request.
  - **Red commands:**
    - `dotnet run --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj -- --treenode-filter "/*/*/*PhotonAddressGeocoderTests/*|/*/*/*AddressSelectionProtectorTests/*|/*/*/*PhotonGeocodingTelemetryTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*GeocodingMetricsTests/*"`
    - `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj -- --treenode-filter "/*/*/*GeocodingBoundaryArchitectureTests/*"`
  - **Acceptance:**
    - [ ] Tests fail for missing adapter/protector/instruments/boundaries.
    - [ ] No fixed sleeps; exact handler/time/event signals drive retry/cancel/expiry tests.
    - [ ] Label allowlist rejects tenant/actor/location/address/query/URI/coordinate/token/payload.
  - **Effort:** L
  - **Dependencies:** 4.1 approval.
  - **Guidance:** Plan Decisions 5-8; official Data Protection/HTTP resilience docs.

- [ ] **4.3 Green Phase - Implement Photon Adapter And Selection Protection**
  - **Files:** new semantic Application provider port/models; existing Application `GeocodingMetrics` extended only for Photon outcomes; new Infrastructure Photon transport/options/adapter, Data Protection adapter, telemetry and registration; package/lock files only if approved; Task 4.2 tests
  - **Description:** Implement concrete provider composition outside Application, one bounded resilience handler, minimal request/normalization, and least-privilege time-limited protected selection. `None` registers no outbound client.
  - **Green commands:**
    - `dotnet run --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj -- --treenode-filter "/*/*/*PhotonAddressGeocoderTests/*|/*/*/*AddressSelectionProtectorTests/*|/*/*/*PhotonGeocodingTelemetryTests/*"`
    - `dotnet run --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj -- --treenode-filter "/*/*/*GeocodingMetricsTests/*"`
    - `dotnet run --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj -- --treenode-filter "/*/*/*GeocodingBoundaryArchitectureTests/*"`
  - **Acceptance:**
    - [ ] Five-second total budget and at most two transient retries fit one pipeline.
    - [ ] Permanent 4xx is not retried; cancellation stops immediately.
    - [ ] Token rejects every scope/target/concurrency/replay mismatch before persistence.
    - [ ] Browser DTO never receives raw coordinates.
    - [ ] No local/manual address is sent upstream.
  - **Effort:** L
  - **Dependencies:** 4.2 Red evidence.
  - **Guidance:** Infrastructure owns provider policy; Application owns semantics only.

- [ ] **4.4 Red Phase - Specify Provider Integration Across API And UI**
  - **Files:** extend Phase 3 API/HAL/BFF/Blazor tests with Photon-ready/unavailable/token scenarios
  - **Description:** Add failing contract tests before wiring provider results: `None`, ready, unavailable, rate-limited, token success/rejection, typed-input retention, HAL provider relation, no coordinate response, no provider requirement for local/manual.
  - **Red commands:**
    - `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressSuggestionsControllerTests/*|/*/*/*LocationAddressHateoasTests/*"`
    - `dotnet run --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj -- --treenode-filter "/*/*/*AddressAutocompleteTests/*|/*/*/*LocationDialogTests/*"`
  - **Acceptance:**
    - [ ] Tests fail only for missing provider integration.
    - [ ] Local-only expectations remain green.
    - [ ] No coordinate appears in API/client/UI result objects.
  - **Effort:** M
  - **Dependencies:** 4.3.
  - **Guidance:** Provider failure cannot disable local/manual paths.

- [ ] **4.5 Green Phase - Integrate Optional Provider Into Existing Vertical Slice**
  - **Files:** Application merge/orchestration; existing Phase 3 API/HAL/generated client/UI; configuration/operations docs; Task 4.4 tests
  - **Description:** Merge eligible local results with optional provider suggestions at the Application response boundary without mixing datasets. Consume protected selections in Location/Event/AI writes. Expose provider availability through server HAL/status and preserve typed input/fallback behavior.
  - **Green commands:**
    - `dotnet run --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj -- --treenode-filter "/*/*/*LocationAddressSuggestionsControllerTests/*|/*/*/*LocationAddressHateoasTests/*"`
    - `dotnet run --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj -- --treenode-filter "/*/*/*AddressAutocompleteTests/*|/*/*/*LocationDialogTests/*"`
  - **Acceptance:**
    - [ ] Provider `None`, unavailable and ready states are honest and accessible.
    - [ ] API response contains display/attribution/source/token only.
    - [ ] Valid token atomically writes address/coordinate; invalid token writes nothing.
    - [ ] Local data never enters provider request.
    - [ ] No-provider/no-map UI remains complete.
  - **Effort:** L
  - **Dependencies:** 4.4 Red evidence.
  - **Guidance:** Plan Section 3.3; no concrete provider behavior in Application.

### Phase 4 Verification - RUN ONCE AFTER TASKS 4.1-4.5

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

- [ ] **4.6 Final Task - Changelog Contribution And Commit Composition**
  - **Files:** next available `docs/releases/changes/CHG-2026-NNNN.yaml` (new), final workstream docs, clean-room/provenance evidence
  - **Description:** Only after all functional work, docs and phase gates are green, create the Tier 2 change fragment and compose the terminal Conventional Commit message/trailers. Do not execute `git commit` without explicit user authorization.
  - **Acceptance:**
    - [ ] All implementation and phase-verification boxes are complete.
    - [ ] Change fragment validates and documents breaking write contracts, migration/quarantine behavior, privacy and operator impact.
    - [ ] Proposed terminal message includes public scope, `Change-Id: CHG-2026-NNNN`, and `BREAKING CHANGE:`.
    - [ ] Clean-room source register, AFC/SSO decision and dependency/service/data license evidence are linked.
    - [ ] Context/tasks/plan/I-VSD and canonical docs agree.
  - **Effort:** S
  - **Dependencies:** Phase 4 verification.
  - **Guidance:** No commit without separate user approval.

## Remaining / Deferred Work

| Item | Why deferred | Trigger / authoritative owner |
|---|---|---|
| Exact PostGIS discovery | Separate product, disclosure authority, lifecycle and operational risk | `dev/active/home-discovery-experience/` Phase 6 after ADR-013 acceptance |
| Map experience | Independent tile/data licensing, privacy, accessibility, bundle and cost | New map workstream |
| Google Places/Maps | Terms, EEA, billing/session, field retention and branding | Separate legal/product/budget workstream |
| Pelias | High data operations and no measured Photon coverage failure | Benchmark plus operator approval |
| GeoNames offline search | Separate importer/update/quality/attribution lifecycle | Approved air-gapped requirement |
| Martin/vector tiles | No safe coarse tile contract; auto-publication risk | Separate aggregate-tile workstream |
| Generic provider abstraction | One adapter provides no proven reusable behavior | Second approved provider |

## Current Blockers / Decisions

1. User approval is required before Phase 1.
2. Phase 4 is blocked on Photon topology, terms, capacity, ownership and recovery evidence.
3. Context7 is unavailable; official docs are the current evidence substitute.
4. ADR-013 remains Proposed and cannot be activated from this ledger.
