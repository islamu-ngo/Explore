<!-- ABOUTME: Execution ledger for the address geocoding and governed spatial discovery workstream. -->
<!-- ABOUTME: Tracks implementation tasks, dependencies, acceptance evidence, phase gates, and deferred provider work. -->

# Address Geocoding And Spatial Discovery — Task Checklist

Last Updated: 2026-08-11 Europe/Brussels

## Status Summary

- **Overall status:** Draft, awaiting user review and Task 1.1 architecture decisions.
- **Completed:** 0/25 implementation tasks (phase verification tracked separately).
- **Current priority:** Task 1.1, approve and reconcile the architecture contract.
- **Next recommended slice:** Complete Phase 1 only; do not start external provider or spatial work until its decision gates are satisfied.
- **Planning evidence:** Baseline Release build passed with 37 projects, 0 errors, and 0 warnings. Tavily primary-source extraction completed. Context7 is unavailable in this session and must not be claimed as verified.

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

- [ ] **1.1 Approve And Reconcile The Architecture Contract**
  - **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md`, `.claude/contract/intents.yaml` only if justified, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and this workstream (existing).
  - **Acceptance:** Explicitly decide PostgreSQL/PostGIS policy, Photon-first provider, protected token flow, and Martin/exact-tile deferral; cross-link Home Discovery Phase 6 and `ELP-730`; record Context7 limitation.
  - **Effort:** M
  - **Dependencies:** None; requires user/product/privacy/operations approval for ADR activation.

- [ ] **1.2 Make Location Address And Coordinate State Atomic**
  - **Files:** `src/Explore.Domain/Location.cs`, `src/Explore.Domain/LocationPii.cs`, `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/Handlers/Commands/`, `src/Explore.Application/Profiles/LookupMappingProfile.cs`, and `tests/Event.Application.UnitTests/Features/Locations/` (existing/new files named in plan Task 1.2).
  - **Acceptance:** Real create construction is characterized; manual/geocoded aggregate methods enforce finite both-or-none coordinates; manual address changes clear coordinates; erasure stays authoritative.
  - **Effort:** L
  - **Dependencies:** 1.1 may remain Proposed for autocomplete-only work.

- [ ] **1.3 Remove Body-Controlled Tenancy And Regenerate The Contract**
  - **Files:** `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/`, `src/Explore.API/Controllers/LocationController.cs`, `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, location service/dialogs/tests, `docs/API.md`, `docs/API_CHANGELOG.md` (existing; exact files in plan Task 1.3).
  - **Acceptance:** No tenant or raw coordinates are accepted in location writes; trusted context owns tenancy; manual entry works; artifacts have only current shapes; UI affordances are HAL-gated.
  - **Effort:** L
  - **Dependencies:** 1.2.

### Phase 1 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 2: Application Geocoding Contract ⏳ NOT STARTED

- [ ] **2.1 Define Minimal Geocoding Ports And Models**
  - **Files:** new `src/Explore.Application/Contracts/Geocoding/IAddressGeocoder.cs`, `AddressGeocodingModels.cs`, `IAddressSelectionProtector.cs`, `src/Explore.Application/Configuration/GeocodingOptions.cs`, and `tests/Event.Application.UnitTests/Features/Geocoding/AddressGeocodingContractTests.cs`.
  - **Acceptance:** Provider-neutral search/selection contracts bound query, results, culture/country, coordinates, cancellation, provider configuration, and token expiry without HTTP/NTS/provider types.
  - **Effort:** M
  - **Dependencies:** Phase 1.

- [ ] **2.2 Implement Address Autocomplete CQRS Orchestration**
  - **Files:** new `src/Explore.Application/DTOs/Geocoding/AddressAutocompleteDtos.cs`, validator, `Features/Geocoding/Requests/Queries/SearchAddressesRequest.cs`, matching handler, and `tests/Event.Application.UnitTests/Features/Geocoding/SearchAddressesRequestHandlerTests.cs`.
  - **Acceptance:** Manual validation prevents invalid provider calls; bounded ordered results carry protected tokens; failures are categorized without logging PII.
  - **Effort:** M
  - **Dependencies:** 2.1.

- [ ] **2.3 Consume Protected Selections In Location Commands**
  - **Files:** existing `src/Explore.Domain/Location.cs`, `src/Explore.Application/DTOs/Location/`, `src/Explore.Application/Features/Locations/`, and matching command tests named in plan Task 2.3.
  - **Acceptance:** Tampered/expired/wrong-purpose tokens fail closed; valid selections atomically set the address/coordinate bundle; manual fallback stores no coordinates; external I/O stays outside transactions.
  - **Effort:** L
  - **Dependencies:** 2.1, 2.2.

### Phase 2 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Photon Infrastructure Adapter ⏳ NOT STARTED

- [ ] **3.1 Fix The Photon Deployment Contract**
  - **Files:** `src/Explore.AppHost/AppHost.cs` and `docker-compose.yml` only if self-hosted, `.env.example`, `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`.
  - **Acceptance:** Endpoint ownership, capacity, data update/swap, health, TLS, rebuild/rollback and profiles are explicit; public Photon is rejected in production.
  - **Effort:** L
  - **Dependencies:** 1.1 provider decision.

- [ ] **3.2 Implement Photon And Selection Protection Adapters**
  - **Files:** new `src/Explore.Infrastructure/Geocoding/PhotonAddressGeocoder.cs`, `PhotonApiModels.cs`, `PhotonOptionsValidator.cs`, `DataProtectionAddressSelectionProtector.cs`, existing `InfrastructureServicesRegistration.cs`, and tests named in plan Task 3.2.
  - **Acceptance:** HTTP success/malformed/timeout/cancel/429/5xx/retry/redaction and token tamper/expiry/purpose/config behavior are covered; telemetry is PII-free.
  - **Effort:** L
  - **Dependencies:** 2.1, 3.1.

- [ ] **3.3 Add Geocoding Readiness And Safe Configuration**
  - **Files:** new `src/Explore.Infrastructure/Geocoding/GeocodingReadinessProbe.cs`, `src/Explore.API/HealthChecks/GeocodingReadinessHealthCheck.cs`, existing `src/Explore.API/Program.cs`, config/secrets/self-hosting docs, and the readiness probe test named in plan Task 3.3.
  - **Acceptance:** Invalid production configuration fails startup; readiness is bounded and query-free; no credentials or endpoint details leak.
  - **Effort:** M
  - **Dependencies:** 3.2.

### Phase 3 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 4: Private Geocoding API Contract ⏳ NOT STARTED

- [ ] **4.1 Add Authenticated Autocomplete POST**
  - **Files:** new `src/Explore.API/Controllers/GeocodingController.cs`, existing `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs`, and new `tests/Event.API.IntegrationTests/Features/GeocodingControllerTests.cs`.
  - **Acceptance:** Named authenticated POST is bounded, rate-limited, private/no-store, cancellable, tenant-safe and PII-free in logs; anonymous/malformed/oversized/limited cases are covered.
  - **Effort:** M
  - **Dependencies:** Phase 3.

- [ ] **4.2 Publish Geocoding Through HAL Where Executable**
  - **Files:** `src/Explore.API/Hateoas/Policies/LocationLinkPolicy.cs`, `src/Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs`, `src/Explore.Application/DTOs/Location/LocationDto.cs`, `tests/Event.API.IntegrationTests/Features/Hateoas/LocationHateoasTests.cs` (existing).
  - **Acceptance:** Only authorized and ready resources contain the relation; malformed/failed checks omit it; Blazor needs no local authorization/provider logic.
  - **Effort:** M
  - **Dependencies:** 4.1.

- [ ] **4.3 Regenerate And Document The API Contract**
  - **Files:** `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md`.
  - **Acceptance:** OpenAPI parity passes; current client method/shapes are generated; POST/no-store/rate-limit/token and breaking-write changes are documented without sample PII.
  - **Effort:** M
  - **Dependencies:** 4.1, 4.2.

### Phase 4 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 5: Accessible Location Editing Experience ⏳ NOT STARTED

- [ ] **5.1 Build The Address Autocomplete Component**
  - **Files:** new `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor`, optional code-behind, isolated CSS, and `tests/Explore.Blazor.Client.Tests/Components/Locations/AddressAutocompleteTests.cs`.
  - **Acceptance:** Accessible combobox/listbox supports keyboard, focus, live status, debounce/cancellation, bounded results, errors, attribution, localization, RTL, mobile and reduced motion; latest request wins.
  - **Effort:** L
  - **Dependencies:** 4.3.

- [ ] **5.2 Integrate Create And Edit Dialogs**
  - **Files:** existing `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, code-behind, `EditLocationDialog.razor`, code-behind, `src/Explore.Blazor.Client/Services/LocationService.cs`, existing service tests, and new `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs`.
  - **Acceptance:** Raw coordinate inputs are gone; current selection/token matches visible fields; manual changes clear it; provider errors retain typed input; PATCH concurrency remains correct.
  - **Effort:** L
  - **Dependencies:** 5.1.

- [ ] **5.3 Enforce HAL-Gated Location Affordances**
  - **Files:** `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor`, `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationsTests.cs`, `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs`.
  - **Acceptance:** Create/edit/delete/autocomplete controls depend only on HAL link presence and invoke advertised URL/method; no local claim/role/provider checks exist.
  - **Effort:** M
  - **Dependencies:** 4.2, 5.2.

### Phase 5 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Phase 6: Approved PostgreSQL/PostGIS Foundation ⛔ DECISION-GATED

- [ ] **6.1 Add PostgreSQL Spatial Capability And Image**
  - **Files:** `Directory.Packages.props`, affected Persistence/test project locks, `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`, `ExploreDbContext.cs`, `src/Explore.AppHost/AppHost.cs`, `docker-compose.yml`, provider/AppHost tests, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md` (exact files in plan Task 6.1).
  - **Acceptance:** NTS is Npgsql-only; non-PG `postgis` fails clearly; primary image/tag/digest and PG18 volume migration are correct; other PostgreSQL services are untouched.
  - **Effort:** L
  - **Dependencies:** 1.1 ADR activation and `ELP-230C` migration integrity.

- [ ] **6.2 Generate The Governed Discovery Projection Migration**
  - **Files:** new `src/Explore.Persistence/Spatial/LocationDiscoveryPoint.cs`, configuration/store, new Application store/scalar contracts, existing DbContext files, generated PostgreSQL migration/snapshot, and spatial tests named in plan Task 6.2.
  - **Acceptance:** `geography(Point,4326)`, GiST and tenant/location constraints exist; Domain/Application have no NTS; public contracts remain coordinate-free; non-PG migration models have no unintended diff.
  - **Effort:** XL
  - **Dependencies:** 6.1.

- [ ] **6.3 Implement Explicit Approval And Revocation**
  - **Files:** new approve/revoke commands and handlers under `src/Explore.Application/Features/Locations/`, Application store contract, `src/Explore.Persistence/Spatial/LocationDiscoveryPointStore.cs`, `LocationPrivacyGovernanceMutationService.cs`, and tests named in plan Task 6.3.
  - **Acceptance:** Only authorized, tenant-owned, active, coordinate-valid locations can be approved; no automatic backfill; concurrency-safe reapproval and immediate revocation preserve audit evidence.
  - **Effort:** L
  - **Dependencies:** 6.2 and `ELP-530` semantics.

- [ ] **6.4 Integrate Erasure, Correction, And Readiness**
  - **Files:** `UserLocationPrivacyErasureRepository.cs`, `LocationPrivacyGovernanceMutationService.cs`, `LocationPrivacyOutboxMessageFactory.cs`, spatial store, new PostGIS readiness check, privacy/architecture tests, ELP tasks, `SELF_HOSTING.md`, `OPERATIONS.md`, `SECURITY-MODEL.md`, `TESTING.md` (exact paths in plan Task 6.4).
  - **Acceptance:** Erasure transaction cannot leave an active point; replay is idempotent; correction invalidates approval; readiness proves extension/table/index/query/mode without sensitive output; `ELP-730` closes only with evidence.
  - **Effort:** XL
  - **Dependencies:** 6.3 and `ELP-515`, `ELP-520`, `ELP-530`.

### Phase 6 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 7: Exact Nearby Occurrence Discovery ⛔ DEPENDS ON PHASE 6

- [ ] **7.1 Implement The PostGIS Occurrence Query**
  - **Files:** new `src/Explore.Application/Contracts/Persistence/IPostgisNearbyOccurrenceQuery.cs`, `DTOs/PublicExperience/NearbyEventDiscoveryDtos.cs`, nearby request/handler, `src/Explore.Persistence/Spatial/PostgisNearbyOccurrenceQuery.cs`, and `tests/Event.Persistence.IntegrationTests/Repositories/PostgisNearbyOccurrenceQueryTests.cs`.
  - **Acceptance:** Tenant/public/published/future/in-person/active predicates, `ST_DWithin`, nearest occurrence, stable ordering and pagination are proven for boundaries/ties/multiple locations/exclusions/cancellation; EXPLAIN uses indexes; no point is returned.
  - **Effort:** XL
  - **Dependencies:** Phase 6.

- [ ] **7.2 Add Private Nearby And Approval API Operations**
  - **Files:** `src/Explore.API/Controllers/PublicExperienceController.cs`, new `LocationDiscoveryController.cs`, route names, location HAL assembler/policy, rate-limit extension, two API tests, and generated sources named in plan Task 7.2.
  - **Acceptance:** Authenticated named POSTs are tenant-safe, bounded, private/no-store and HAL-gated; unsupported modes fail honestly; origin never reaches routes/logs/errors/metrics/settings.
  - **Effort:** L
  - **Dependencies:** 6.3, 7.1.

- [ ] **7.3 Integrate Home Discovery And Canonical Contracts**
  - **Files:** `GetHomeDiscoveryQueryHandler.cs`, `HomeDiscoveryDto.cs`, `HomeDiscoveryService.cs`, generated OpenAPI/client/inventory, Home API/client tests, and canonical docs named in plan Task 7.3.
  - **Acceptance:** Area-only cache behavior is unchanged; exact mode activates rounded distance/nearest occurrence only after explicit action; origin is not persisted; workstream evidence is reconciled.
  - **Effort:** L
  - **Dependencies:** 7.2.

### Phase 7 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: One Accessible Map Experience ⛔ DECISION-GATED

- [ ] **8.1 Select The Production Map Integration And Tile Source**
  - **Files:** `Directory.Packages.props`, `src/Explore.Blazor.Client/Explore.Blazor.Client.csproj`, its lock, `PublicExperienceHomeBlocksConfig.cs`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, ADR-013 or a focused ADR only if durable.
  - **Acceptance:** One current Blazor/MapLibre integration and one licensed production basemap are approved/pinned after compatibility/SSR/disposal/CSP/accessibility/RTL/bundle review; public OSM/demo/internal URLs are rejected.
  - **Effort:** M
  - **Dependencies:** Explicit product/operations decision; Context7 revalidation if available.

- [ ] **8.2 Implement The Supplementary Map Component**
  - **Files:** new `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor`, optional code-behind/isolated CSS/JS module, and `tests/Explore.Blazor.Client.Tests/Components/Maps/EventDiscoveryMapTests.cs`.
  - **Acceptance:** Admin preview and public coarse/user-local context have complete non-map equivalents; no exact public event point/PII/secret leaks; prerender/navigation/JS/provider failure preserve the form/list.
  - **Effort:** L
  - **Dependencies:** 8.1.

- [ ] **8.3 Integrate Map Preview And Nearby Context**
  - **Files:** existing location dialogs, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor` and CSS, `HomeDiscoveryDto.cs`, new/existing tests named in plan Task 8.3, `docs/BLAZOR.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`.
  - **Acceptance:** HAL/config-gated map is optional; saving and discovery list work without it; responsive/RTL/keyboard/focus/announcement/reduced-motion behavior is covered.
  - **Effort:** L
  - **Dependencies:** 8.2, 5.2, 7.3.

### Phase 8 Verification — RUN ONCE AFTER ALL PHASE TASKS

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`

## Remaining / Deferred Work

| Item | Reason | Trigger |
|---|---|---|
| Google Places + Google Maps | EEA legal/storage/attribution/session/billing decision absent. | Approved legal memo, budget/quotas, retention contract, premium coverage requirement. |
| Pelias | No measured Photon coverage failure; significant data operations. | Regional benchmark plus accepted operations capacity. |
| Native GeoNames/SQLite FTS5 | Separate importer/dataset/licensing/search-quality workstream. | Approved air-gapped/minimal deployment requirement. |
| LeafletForBlazor | No second renderer requirement; package maturity is volatile. | Measured MapLibre/WebGL incompatibility on supported clients. |
| Martin | No approved safe tile dataset; exact points cannot be public MVT geometry. | Approved coarse/aggregate tile contract; allowlisted read-only source, `auto_publish: false`, pinned image, same-origin route. |
| Generic map abstraction | One renderer does not justify it. | Second approved renderer exposes concrete reusable behavior. |

## Current Blockers / Decisions

1. ADR-013 activation and the recommendation that every PostgreSQL deployment use the PostGIS-capable image/extension.
2. Ownership and sizing of a self-hosted/contracted Photon service; public Photon is not a production fallback.
3. Production map tile/style provider and MapLibre wrapper compatibility; public OSM/demo tiles are not acceptable defaults.
4. Context7 is not installed in this session. Dependency choices require Context7 revalidation if it becomes available, otherwise primary official documentation with recorded substitution.
