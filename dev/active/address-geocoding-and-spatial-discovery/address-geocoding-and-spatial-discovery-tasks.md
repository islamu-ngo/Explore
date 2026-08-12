<!-- ABOUTME: Execution ledger for the address geocoding and governed spatial discovery workstream. -->
<!-- ABOUTME: Tracks implementation tasks, dependencies, acceptance evidence, phase gates, and deferred provider work. -->

# Address Geocoding And Spatial Discovery — Task Checklist

Last Updated: 2026-08-12 Europe/Brussels

## Status Summary

- **Overall status:** Senior CTO review plus provider-licensing/optionality/local-address-governance feedback incorporated; runtime implementation not started.
- **Completed:** 0/28 implementation tasks (phase verification tracked separately).
- **Current priority:** Task 1.1, codify the approved optional capability contract.
- **Next recommended slice:** Complete Phase 1 only; do not start external provider or spatial work until its decision gates are satisfied.
- **Planning evidence:** Re-baseline Release build passed on 2026-08-12 with 37 projects, 0 errors, and 0 warnings. Tavily verified EF cross-context transactions, Google Places map/no-map and storage rules, OSM ODbL separation/attribution, tile-source constraints, and GeoNames CC BY 4.0 from primary sources. Final QA proved 28/28 task and 9/9 phase parity, one build/test gate per phase, and clean scoped diffs. Context7 is unavailable and must not be claimed as verified.

## Implementation Maintenance Rules

- Read the full workstream once at initial implementation start; on resume, read context/tasks first and only relevant plan sections.
- Do not reread unchanged artifacts after every task.
- Mark substantial work `🟡 IN PROGRESS` when it spans meaningful work or a handoff; skip churn for a tiny task completed immediately.
- Check a substantial completed task immediately; reconcile small completed tasks no later than phase end.
- Add discovered work under the owning phase and keep the count, priority, next slice, deferred work, and date accurate.
- Check a phase complete only after every implementation task and both phase-verification boxes pass.
- Update context after a phase, decision, blocker, failed validation, material discovery, or handoff.
- Update the plan only when scope, architecture, sequence, acceptance, risk, or validation strategy changes.
- Do not run build/tests after individual tasks; verify once at phase end.
- Do not start the app, browser, Docker, Aspire, Playwright, Chrome DevTools, or live services for the automated phase gate.
- Never hand-edit migrations/snapshots/generated clients or add backward-compatibility shims.

## Phase 1: Governance And Location Integrity ⏳ NOT STARTED

- [ ] **1.1 Codify The Approved Optional Capability Contract**
  - **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md`, `.claude/contract/intents.yaml` only if justified, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and this workstream (existing).
  - **Acceptance:** ADR/docs define healthy `Geocoding:Provider=None`, `Maps:Provider=None`, and `Database__Capabilities__Postgis=false`; current Photon opt-in; future GoogleMaps-or-None rule; provider retention/attribution profiles; hierarchical custom-address policy/scoping/promotion; never-upstream isolation; optional PostGIS context/history; and Martin/exact-tile deferral. Cross-link Home Discovery Phase 6 and `ELP-730`; record Context7 limitation.
  - **Effort:** M
  - **Dependencies:** User architecture decision recorded on 2026-08-12; runtime implementation and ADR activation remain separately approval-gated.

- [ ] **1.2 Make Location Address And Coordinate State Atomic**
  - **Files:** `src/Explore.Domain/Location.cs`, `src/Explore.Domain/LocationPii.cs`, `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/Handlers/Commands/`, `src/Explore.Application/Profiles/LookupMappingProfile.cs`, and `tests/Event.Application.UnitTests/Features/Locations/` (existing/new files named in plan Task 1.2).
  - **Acceptance:** Real create construction is characterized; manual/geocoded aggregate methods enforce finite both-or-none coordinates; manual address changes clear coordinates; erasure stays authoritative.
  - **Effort:** L
  - **Dependencies:** 1.1 may remain Proposed for autocomplete-only work.

- [ ] **1.3 Remove Body-Controlled Tenancy And Regenerate The Contract**
  - **Files:** `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/`, `src/Explore.API/Controllers/LocationController.cs`, `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, location service/dialogs/tests, `docs/API.md`, `docs/API_CHANGELOG.md` (existing; exact files in plan Task 1.3).
  - **Acceptance:** No tenant or raw coordinates are accepted in location writes; trusted context owns tenancy; manual command shape awaits effective policy/HAL instead of bypassing it; artifacts have only current shapes.
  - **Effort:** L
  - **Dependencies:** 1.2.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Application Address Acquisition And Governance Contract ⏳ NOT STARTED

- [ ] **2.1 Define Minimal Address Acquisition And Policy Contracts**
  - **Files:** new geocoder/selection/local-suggestion/policy contracts and models; governance keys/definitions/typed group; `GeocodingOptions`; authorization action; Application contract tests (exact paths in plan Task 2.1).
  - **Acceptance:** Contracts cover `None`/Photon, scoped local search, source/visibility, protected selection, effective creation policy, and promotion without HTTP/EF/NTS/provider transport types. Security settings stop above user scope; deferred Google is not accepted as implemented configuration.
  - **Effort:** M
  - **Dependencies:** Phase 1.

- [ ] **2.2 Merge Optional Provider And Scoped Local Suggestions**
  - **Files:** new address DTO/validator/query/handler, effective-policy support, promotion command/handler, authorization contracts/actions, and matching Application tests.
  - **Acceptance:** `None` performs local-only search; local rows are tenant-approved/current-org/current-creator scoped before mapping; provider results remain attributed/tokenized; promotion changes visibility only; no local address is sent upstream.
  - **Effort:** M
  - **Dependencies:** 2.1.

- [ ] **2.3 Enforce Protected Or Governed Manual Location Writes**
  - **Files:** existing `src/Explore.Domain/Location.cs`, `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/`, and matching command tests named in plan Task 2.3.
  - **Acceptance:** Tampered/expired/wrong-purpose/profile-invalid tokens fail closed; valid selections atomically set the bundle; manual writes require effective policy plus actor authorization, set creator/org visibility, store no coordinates, and make no provider call; reuse `Location.CreatedBy`.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.2.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Provider-Neutral Local Address Persistence ⏳ NOT STARTED

- [ ] **3.1 Add Source, Visibility, And Organization Ownership State**
  - **Files:** `Location`; lookup enums/entities/configuration/seeding; location EF configuration; generated migrations/snapshots for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL; matching tests (exact paths in plan Task 3.1).
  - **Acceptance:** Source (`ProviderSelection|LocalManual`) and visibility (`CreatorPrivate|OrganizationScoped|TenantApproved`) are independent; creator reuses `CreatedBy`; organization ownership is constrained; all five provider models have parity and no spatial/provider-specific type; development baselines reset/regenerate without heuristic legacy backfill or shim.
  - **Effort:** XL
  - **Dependencies:** Phase 2.

- [ ] **3.2 Implement Scoped Local Search And Promotion Persistence**
  - **Files:** location query/repository/specification/configuration, promotion persistence path, and new Persistence integration tests named in plan Task 3.2.
  - **Acceptance:** SQL applies tenant plus tenant-approved/current-org/current-creator predicates before exact PII projection across all providers; query is bounded/no-tracking; promotion is idempotent/concurrency-safe; provider `None` works; no upstream dataset path exists.
  - **Effort:** L
  - **Dependencies:** 3.1.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: Photon Infrastructure Adapter ⏳ NOT STARTED

- [ ] **4.1 Fix The Photon Deployment Contract**
  - **Files:** `src/Explore.AppHost/AppHost.cs` and `docker-compose.yml` only if self-hosted, `.env.example`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`.
  - **Acceptance:** `Geocoding:Provider=None` is the documented default; endpoint ownership, capacity, update/swap, health, TLS, rebuild/rollback and profiles are explicit; regional procedure is documented; public Photon is never implicit; `.env.example` documents current values and the future Google rule without advertising Google as implemented.
  - **Effort:** L
  - **Dependencies:** 1.1 provider decision.

- [ ] **4.2 Implement Photon And Selection Protection Adapters**
  - **Files:** new `src/Explore.Infrastructure/Geocoding/PhotonAddressGeocoder.cs`, `PhotonApiModels.cs`, `PhotonOptionsValidator.cs`, `DataProtectionAddressSelectionProtector.cs`, existing `InfrastructureServicesRegistration.cs`, and tests named in plan Task 4.2.
  - **Acceptance:** HTTP success/malformed/timeout/cancel/429/5xx/retry/redaction and token tamper/expiry/purpose/config behavior are covered; defaults enforce a 5-second total timeout, two retries delayed 200/500 ms, bounded `Retry-After`, and immediate cancellation; telemetry is PII-free.
  - **Effort:** L
  - **Dependencies:** 2.1, 4.1.

- [ ] **4.3 Add Geocoding Readiness And Safe Configuration**
  - **Files:** new `src/Explore.Infrastructure/Geocoding/GeocodingReadinessProbe.cs`, `src/Explore.API/HealthChecks/GeocodingReadinessHealthCheck.cs`, existing `src/Explore.API/Program.cs`, config/secrets/self-hosting docs, and the readiness probe test named in plan Task 4.3.
  - **Acceptance:** `None` registers no outbound adapter and is healthy; invalid implemented-provider details/ranges fail startup; defaults are documented; readiness is bounded/query-free. Future invalid Google/non-Google-map env config disables Google with degraded/admin warning while settings API rejects it; no Google call or host-wide outage; no secrets/endpoints leak.
  - **Effort:** M
  - **Dependencies:** 4.2.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Private Geocoding API Contract ⏳ NOT STARTED

- [ ] **5.1 Add Authenticated Autocomplete POST**
  - **Files:** new `src/Explore.API/Controllers/GeocodingController.cs`, existing `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs`, and new `tests/Event.API.IntegrationTests/Features/GeocodingControllerTests.cs`.
  - **Acceptance:** Named authenticated POST is bounded, rate-limited, private/no-store, cancellable, tenant-safe and PII-free; provider `None` returns eligible local rows without external adapter resolution; abuse/error cases are covered.
  - **Effort:** M
  - **Dependencies:** Phase 4.

- [ ] **5.2 Publish Address Acquisition And Moderation Through HAL**
  - **Files:** location/admin HAL policies/assemblers, governance status/query, promotion endpoint/route, settings endpoint only if generic settings is insufficient, and API/HAL tests (exact paths in plan Task 5.2).
  - **Acceptance:** Local/provider autocomplete, `create_custom_address`, and `approve_tenant_address` reflect executable server state; `None` preserves local search; invalid Google/non-Google state exposes no Google action and a clear admin warning; Blazor owns no auth/provider/policy decision.
  - **Effort:** M
  - **Dependencies:** 3.2, 5.1.

- [ ] **5.3 Regenerate And Document The API Contract**
  - **Files:** `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md`.
  - **Acceptance:** OpenAPI parity passes; current client methods/shapes are generated; POST/no-store/rate-limit/token, policy/visibility/promotion, optional-provider status, and breaking-write changes are documented without sample PII.
  - **Effort:** M
  - **Dependencies:** 5.1, 5.2.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: Accessible Location Editing Experience ⏳ NOT STARTED

- [ ] **6.1 Build The Address Autocomplete Component**
  - **Files:** new `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor`, optional code-behind, isolated CSS, and `tests/Explore.Blazor.Client.Tests/Components/Locations/AddressAutocompleteTests.cs`.
  - **Acceptance:** Accessible combobox/listbox supports keyboard, focus, live status, debounce/cancellation, bounded results, provider attribution, source/visibility badges, localization, RTL, mobile and reduced motion; latest request wins.
  - **Effort:** L
  - **Dependencies:** 5.3.

- [ ] **6.2 Integrate Create And Edit Dialogs**
  - **Files:** existing `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, code-behind, `EditLocationDialog.razor`, code-behind, `src/Explore.Blazor.Client/Services/LocationService.cs`, existing service tests, and new `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs`.
  - **Acceptance:** Raw coordinates are gone; token matches visible fields; manual controls require HAL and clear stale selection; provider errors retain input; `None/None` is healthy; admin policy/provider status is clear. Current UI does not offer unimplemented Google; its activation must add GoogleMaps-or-None form validation and server warning atomically.
  - **Effort:** L
  - **Dependencies:** 6.1.

- [ ] **6.3 Enforce HAL-Gated Location Affordances**
  - **Files:** `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor`, `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationsTests.cs`, `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs`.
  - **Acceptance:** Create/edit/delete/autocomplete/custom-create/tenant-approve controls depend only on HAL links and invoke advertised URL/method; no local claim/role/provider/policy checks exist.
  - **Effort:** M
  - **Dependencies:** 5.2, 6.2.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Optional PostGIS Capability Package ⛔ DECISION-GATED

- **Gate entry checklist:** Explicit repository owner/product architecture approval; ADR-013 accepted by Task 7.1 with actual date and named decider/role; `ELP-230C/515/520/530` reconciled; target PostgreSQL activation path documented; capability-off rollback confirmed.

- [ ] **7.1 Accept ADR-013 And Record The Activation Decision**
  - **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md`, this workstream, and `docs/ARCHITECTURE.md` only if its decision index/status requires synchronization.
  - **Acceptance:** The Phase 7 implementation owner records, but does not self-grant, explicit activation approval; ADR-013 changes from `Proposed` to `Accepted` with actual date and named decider/role and preserves optional capability, isolated migration, origin privacy, and exact discovery boundaries. Without approval, Tasks 7.2–8.3 remain blocked.
  - **Effort:** S
  - **Dependencies:** Task 1.1 codification, explicit Phase 7 activation approval, and reconciled privacy prerequisites.

- [ ] **7.2 Add Explicit Optional PostGIS Capability Composition**
  - **Files:** new `PrimaryDatabaseCapabilityOptions.cs`; existing provider/persistence/migration composition, `.env.example`, AppHost/Compose, provider/AppHost tests, `CONFIGURATION.md`, `SELF_HOSTING.md`, `OPERATIONS.md` (exact paths in plan Task 7.2).
  - **Acceptance:** The flag defaults false and runs no spatial service/migration/probe; only PostgreSQL plus explicit true selects the optional context; invalid provider/mode combinations fail clearly; plain PostgreSQL remains the local default; managed/local activation is documented; the Aspire image override API is revalidated from current official documentation at implementation time.
  - **Effort:** L
  - **Dependencies:** 7.1 and `ELP-230C` migration integrity.

- [ ] **7.3 Generate The Isolated PostGIS Projection Migration**
  - **Files:** central package/locks; new `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryDbContext.cs`, design-time factory, row/config/store; Application scalar ports; generated dedicated migration/snapshot; spatial tests named in plan Task 7.3.
  - **Acceptance:** Primary snapshots for all providers have no spatial diff; the dedicated `__EFPostgisDiscoveryMigrationsHistory` creates `geography(Point,4326)`, GiST and relational constraints only when enabled; capability-off plain PostgreSQL creates no optional schema; Domain/Application remain NTS-free and public contracts coordinate-free.
  - **Effort:** XL
  - **Dependencies:** 7.2.

- [ ] **7.4 Implement Explicit Approval And Revocation**
  - **Files:** new approve/revoke commands and handlers under `src/Explore.Application/Features/Locations/`, Application store contract, `src/Explore.Persistence/Spatial/Postgis/PostgisLocationDiscoveryPointStore.cs`, `LocationPrivacyGovernanceMutationService.cs`, and tests named in plan Task 7.4.
  - **Acceptance:** Scalar Application contracts remain database-neutral; capability-off/non-PostgreSQL deployments expose no approval action or store call; only authorized, tenant-owned, active, coordinate-valid locations can be approved; no automatic backfill; concurrency-safe reapproval and revocation preserve evidence.
  - **Effort:** L
  - **Dependencies:** 7.3 and `ELP-530` semantics.

- [ ] **7.5 Integrate Erasure, Correction, And Readiness**
  - **Files:** primary erasure/mutation/outbox paths, PostGIS store and new transaction coordinator/readiness check, privacy/architecture tests, ELP tasks, `SELF_HOSTING.md`, `OPERATIONS.md`, `SECURITY-MODEL.md`, `TESTING.md` (exact paths in plan Task 7.5).
  - **Acceptance:** Existing `EfCoreUnitOfWork` solely owns begin/commit/rollback/retry; a fresh spatial context shares the primary `DbConnection` and enlists with `UseTransactionAsync` on its current `DbTransaction`; success commits both, failure/cancellation rolls both back, retries are fresh/idempotent, and execution outside a primary transaction fails fast. No `TransactionScope`, nested/distributed transaction, or second connection is allowed. Disabled paths construct no spatial context and reference no optional table; correction invalidates approval; bounded readiness and `ELP-730` close only with evidence.
  - **Effort:** XL
  - **Dependencies:** 7.4 and `ELP-515`, `ELP-520`, `ELP-530`.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Exact Nearby Occurrence Discovery ⛔ DEPENDS ON PHASE 7

- [ ] **8.1 Implement The PostGIS Occurrence Query**
  - **Files:** new database-neutral `src/Explore.Application/Contracts/Persistence/INearbyOccurrenceQuery.cs`, nearby DTO/request/handler, optional `src/Explore.Persistence/Spatial/Postgis/PostgisNearbyOccurrenceQuery.cs`, and `tests/Event.Persistence.IntegrationTests/Repositories/PostgisNearbyOccurrenceQueryTests.cs`.
  - **Acceptance:** Capability-off/non-PostgreSQL paths retain area-only behavior and never resolve the adapter; the enabled adapter proves tenant/public/published/future/in-person/active predicates, `ST_DWithin`, nearest occurrence, stable ordering/pagination, boundary/tie/multi-location/exclusion/cancellation cases, index use, and no point response.
  - **Effort:** XL
  - **Dependencies:** Phase 7.

- [ ] **8.2 Add Private Nearby And Approval API Operations**
  - **Files:** `src/Explore.API/Controllers/PublicExperienceController.cs`, new `LocationDiscoveryController.cs`, route names, location HAL assembler/policy, rate-limit extension, two API tests, and generated sources named in plan Task 8.2.
  - **Acceptance:** Authenticated named POSTs are tenant-safe, bounded, private/no-store and HAL-gated; unsupported modes fail honestly; origin never reaches routes/logs/errors/metrics/settings.
  - **Effort:** L
  - **Dependencies:** 7.4, 8.1.

- [ ] **8.3 Integrate Home Discovery And Canonical Contracts**
  - **Files:** `GetHomeDiscoveryQueryHandler.cs`, `HomeDiscoveryDto.cs`, `HomeDiscoveryService.cs`, generated OpenAPI/client/inventory, Home API/client tests, and canonical docs named in plan Task 8.3.
  - **Acceptance:** Area-only cache behavior is unchanged; exact mode activates rounded distance/nearest occurrence only after explicit action; origin is not persisted; workstream evidence is reconciled.
  - **Effort:** L
  - **Dependencies:** 8.2.

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 9: One Accessible Map Experience ⛔ DECISION-GATED

- **Gate entry checklist:** Compare an operator-controlled PMTiles/Protomaps-style option with hosted candidates such as MapTiler and Stadia Maps using current official terms; record all software/data/style/glyph/sprite licenses and attribution; confirm production/commercial, caching/CDN, redistribution/offline, and self-hosting rights; document hosted privacy/DPA/data region; prove p95 tiles ≤750 ms and first useful render ≤3 seconds on the supported median device/representative 4G profile; approve self-hosted TCO or a hosted hard monthly ceiling/overage policy; retain complete no-map behavior.

- [ ] **9.1 Select The Production Map Integration And Tile Source**
  - **Files:** `Directory.Packages.props`, `src/Explore.Blazor.Client/Explore.Blazor.Client.csproj`, its lock, `PublicExperienceHomeBlocksConfig.cs`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, ADR-013 or a focused ADR only if durable.
  - **Acceptance:** One current Blazor/MapLibre integration and one source are approved/pinned only after the complete gate matrix passes. PMTiles' runtime license is not treated as a data license; public OSM/demo/internal URLs, missing attribution/redistribution rights, failed performance budgets, and unbounded hosted spend are rejected.
  - **Effort:** M
  - **Dependencies:** Explicit product/operations decision; Context7 revalidation if available.

- [ ] **9.2 Implement The Supplementary Map Component**
  - **Files:** new `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor`, optional code-behind/isolated CSS/JS module, and `tests/Explore.Blazor.Client.Tests/Components/Maps/EventDiscoveryMapTests.cs`.
  - **Acceptance:** Admin preview and public coarse/user-local context have complete non-map equivalents; no exact public event point/PII/secret leaks; prerender/navigation/JS/provider failure preserve the form/list; `<noscript>`, unavailable WebGL, and unsupported/low-end devices receive an accessible fallback message.
  - **Effort:** L
  - **Dependencies:** 9.1.

- [ ] **9.3 Integrate Map Preview And Nearby Context**
  - **Files:** existing location dialogs, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor` and CSS, `HomeDiscoveryDto.cs`, new/existing tests named in plan Task 9.3, `docs/BLAZOR.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`.
  - **Acceptance:** HAL/config-gated map is optional; saving and discovery list work without it; responsive/RTL/keyboard/focus/announcement/reduced-motion behavior is covered.
  - **Effort:** L
  - **Dependencies:** 9.2, 6.2, 8.3.

### Phase 9 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

| Item | Reason | Trigger |
|---|---|---|
| Google Places with optional Google Maps | Pairing is resolved (`GoogleMaps` or no map only), but EEA/storage/attribution/session/billing and field-level retention remain unapproved. | Approved legal/budget/retention contract; scoped phase atomically adds Places, optional Google Maps, server/API + admin-form validation/warnings, `.env.example`, and tests. |
| Pelias | No measured Photon coverage failure; significant data operations. | Regional benchmark plus accepted operations capacity. |
| Native GeoNames/SQLite FTS5 | Separate importer/refresh/search-quality workstream. CC BY 4.0 allows commercial adaptation without source-code copyleft but requires credit, license link, and change indication for public/redistributed use. | Approved air-gapped/minimal requirement plus result/credits attribution, change notice, redistribution metadata, and compliance tests. |
| LeafletForBlazor | No second renderer requirement; package maturity is volatile. | Measured MapLibre/WebGL incompatibility on supported clients. |
| Martin | No approved safe tile dataset; exact points cannot be public MVT geometry. | Approved coarse/aggregate tile contract; allowlisted read-only source, `auto_publish: false`, pinned image, same-origin route. |
| Generic map abstraction | One renderer does not justify it. | Second approved renderer exposes concrete reusable behavior. |

## Current Blockers / Decisions

1. Runtime activation of ADR-013/Phase 7 remains approval-gated. Once approved, Task 7.1 must record `Accepted`, the actual date, and named decider/role before spatial implementation begins.
2. Ownership, regional/planet dataset sizing, and update procedure for a self-hosted/contracted Photon service; public Photon is not an implicit development or production fallback.
3. Production map tile/style decision against the Phase 9 license, self-hosting, privacy, performance, and cost gate; public OSM/demo tiles are not acceptable defaults.
4. Context7 is not installed in this session. Dependency choices require Context7 revalidation if it becomes available, otherwise primary official documentation with recorded substitution.
