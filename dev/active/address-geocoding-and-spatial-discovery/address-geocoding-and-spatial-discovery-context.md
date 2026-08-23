<!-- ABOUTME: Resume context for the address geocoding and governed spatial discovery workstream. -->
<!-- ABOUTME: Captures current evidence, decisions, blockers, validation, and the next executable implementation slice. -->

# Address Geocoding And Spatial Discovery — Context

Last Updated: 2026-08-23 Europe/Brussels

## SESSION PROGRESS (2026-08-23 Europe/Brussels) — Upstream Rebaseline

The Event Location Privacy workstream shipped completely and moved to
`dev/zarchive/event-location-privacy/`. This workstream was re-checked against that change. **No task
was invalidated and no phase was resequenced**; what changed is a set of file-level assumptions and one
dependency that is now satisfied. Plan Sections 2.1/2.2/2.4, Tasks 1.3, 2.2, 3.1, 6.2, Phase 7's gate,
and Section 12 were amended, and `tasks.md` gained an "Upstream Changes To Absorb" table.

### What changed underneath this workstream

- **Deleted surface.** `GET /api/location/by-city|by-country`, `ILocationRepository.GetLocationsByCity`/
  `GetLocationsByCountry`, the matching `LocationService`/generated-client methods, and their tests are
  gone. They enumerated exact venue addresses — including private homes — for any caller with
  tenant-wide location view, with no disclosure evaluation. `ILocalAddressSuggestionQuery` must not
  reintroduce that shape.
- **Location contract grew.** `LocationDto` carries descriptive `LocationKindId`, and `LocationController`
  gained consent-backed `POST /{id}/private-home` and `POST /{id}/private-home/ownership`
  (`PrivateHomeOwnershipConsentDto`, `If-Match` required, ownership claimed only by the incoming owner).
- **`EditLocationDialog.razor` changed.** It now injects `IDialogService` and hosts the private-home
  consent action. Task 6.2 removes coordinate inputs from this file and must preserve that action.
- **Migration head moved.** All five providers now end at `ContractEventLocationPhysicalReferences`,
  which adds `location_id IS NULL OR event_location_id IS NOT NULL` to the four carrier tables. Phase 3
  branches from there.
- **New build-enforcing guards.** `EventLocationDisclosureConvergenceTests` closes
  `EventLocationDisclosureEvaluator` to a documented authority set;
  `OutboundProducerPrivacyTests` fails any listed producer directory that reads raw venue address or
  coordinates; `EventLocationSchemaContractionTests` pins the contraction migration.
- **OpenAPI enum generation changed.** Enum schemas now derive from each type's own `JsonConverter`, and
  HAL-wrapped enum properties emit `$ref` instead of `integer`/`object`. New address-source/visibility
  enums must be registered in `OpenApiStringEnumSchemaCatalog`, and the API must be rebuilt before the
  client because the client reads the emitted schema file.
- **Phase 7 dependency satisfied.** `ELP-230C`, `ELP-515`, `ELP-520`, `ELP-530`, and `ELP-730` are done.
  That gate item is now an evidence check, not a wait.

### Still true

Planning remains complete and runtime implementation still has not started. Task 1.1 is still the
current priority, and the Photon/map/Google/ADR blockers below are unchanged.

## SESSION PROGRESS (2026-08-12 Europe/Brussels)

### ✅ COMPLETED

- Planning created from `dev/report/address_geocoding_analysis.md` and current repository reality.
- Contribution Contract, matched rules, required skills, canonical docs, current location/privacy/discovery flows, database provider composition, AppHost/Compose, existing tests, and overlapping workstreams were investigated.
- Code-review graph was used before filesystem fallback; current graph matched repository HEAD.
- Baseline Release build passed: 37 projects, 0 errors, 0 warnings.
- Tavily primary-source extraction verified Npgsql spatial mapping, Google Places policy/session constraints, Martin publication/configuration behavior, and OSM tile policy.
- Context7 availability was checked; no Context7 connector/tool/resource/template exists in this session, so no Context7 evidence is claimed.
- The user approved a fully optional PostGIS policy: every primary provider, including plain PostgreSQL, keeps database-neutral behavior by default; exact spatial discovery is an explicit PostgreSQL capability only.
- The plan was re-baselined around `Database__Capabilities__Postgis=false`, an isolated optional PostGIS DbContext/migration history, semantic Application ports, and an honest area-only fallback.
- Senior CTO review returned **Approve with Required Changes**; RC-1 through RC-5 are incorporated, including an explicit shared-connection/shared-transaction mechanism and a dedicated ADR-013 acceptance task.
- Current Google Places policies were rechecked through Tavily: Places content may be used without a map with adjacent Google branding/attribution; if displayed on a map it must use Google Maps. Place IDs have a storage exception, so the report's blanket 30-day storage statement is not a safe invariant.
- OSM ODbL and GeoNames CC BY 4.0 implications were rechecked from primary sources. Local application data remains independent and never upstreams into provider datasets.
- The repository's existing lockable Instance → Tenant → Organization → Group → User settings cascade and `Location.CreatedBy` were selected for custom-address governance/scoping; no parallel policy engine or duplicate creator field is planned.
- Geocoding and map providers now default independently to `None`. The future Google adapter may pair only with Google Maps or no map; current runtime configuration does not accept an unimplemented Google value.
- Plan and 28-task, nine-phase execution ledger were synchronized.
- The 2026-08-12 baseline Release build passed: 37 projects, 0 errors, 0 warnings.
- Licensing/governance re-baseline QA passed: plan Sections 0–17 remain present; all 28 task IDs/names and all nine phase names match the ledger; each phase has one Release build plus one selected test; stale 26-task/eight-phase references are absent; and scoped `git diff --check` is clean.

### 🟡 IN PROGRESS

- Awaiting approval to begin runtime implementation; planning and validation are complete.

### ⏭️ NEXT

1. User confirms the re-baselined plan or explicitly approves implementation.
2. Start Phase 1 at Task 1.1 to codify the approved optional capability contract, then complete the location integrity/breaking contract slice before external provider work.
3. Confirm operator ownership only if enabling the first optional provider, Photon, before Phase 4; `Geocoding:Provider=None` requires no provider decision.
4. Do not start the Phase 7 PostGIS package or Phase 9 map work until their explicit activation gates are approved. Google remains a separate deferred adapter phase.

### ⚠️ BLOCKERS

- **ADR activation:** ADR-013 remains `Proposed`. After explicit repository owner/product architecture approval, the Phase 7 implementation owner must record `Accepted`, the actual date, and named decider/role before spatial code begins.
- **Photon production topology:** Public Photon is unsuitable as an implicit development or production dependency; endpoint ownership, regional/planet data footprint, update cadence, and capacity are not approved.
- **Map source:** No basemap/style source has passed the Phase 9 license, self-hosting, privacy, performance, and cost gate; public OSM/demo tiles remain forbidden defaults.
- **Google activation:** Pairing semantics are resolved, but storage/retention, EEA terms, session/billing, field masks, and implementation scope are not approved. Do not expose `GooglePlaces` as a selectable value yet.
- **Resolved 2026-08-23 — Event Location Privacy dependency:** previously an in-flight coordination risk, now shipped and archived. Phase 7 verifies its evidence instead of waiting on it.
- **Context7:** Requested documentation connector is unavailable. Revalidate dependencies there if it becomes available, otherwise continue with primary official documentation and record the substitution.

## Quick Resume

1. Read this context and `address-geocoding-and-spatial-discovery-tasks.md`, including its "Upstream Changes To Absorb" table.
2. Read only the current phase, constraints, or changed decisions from `address-geocoding-and-spatial-discovery-plan.md`; do not reread the full unchanged plan every resume.
3. Start from Task 1.1 unless the user overrides it.
4. Keep `tasks.md` current. Update context/plan only at their defined triggers.
5. Preserve unrelated shared-worktree changes and never edit generated migrations/client files by hand.

## Plan In One Paragraph

First repair the `Location` write boundary: trusted tenancy only, atomic address/coordinate state, and no raw coordinate writes. Reuse the five-tier settings engine plus server authorization/HAL to govern local creation; keep source separate from creator/org/tenant-approved visibility; filter exact local suggestions in SQL; and never send local data upstream. `Geocoding:Provider=None` and `Maps:Provider=None` are healthy. Add one optional Photon adapter, protected selections, private POST API, and accessible source-labelled combobox. Google stays deferred, with a binding rule that it may use Google Maps or no map only. Phase 3 generates provider-neutral provenance/visibility migrations for all five databases. Only after ADR-013 acceptance may explicit PostgreSQL/PostGIS opt-in activate the isolated spatial context/history and shared-UoW transaction flow. MapLibre remains a separate optional gated phase.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/report/address_geocoding_analysis.md` | Existing | Research | Original provider/map analysis. | Input, not authority over current ADRs/rules. |
| `docs/adr/ADR-013-postgis-proximity-discovery.md` | Existing | Architecture | Governs exact proximity, public-point privacy, query shape, readiness, and activation. | Proposed; Task 1.1 owns decision. |
| `src/Explore.Domain/Location.cs` | Existing | Domain | Tenant aggregate, audit creator, PII proxies, privacy lifecycle, concurrency. | Task 1.2 adds atomic methods; Phase 3 adds separate address source/visibility and nullable organization ownership while reusing `CreatedBy`. |
| `src/Explore.Domain/LocationPii.cs` | Existing | Domain | Hard-deleteable exact address/postcode/coordinates. | Remains scalar/private; no NTS `Point`. |
| `src/Explore.Application/DTOs/Location/CreateLocationDto.cs` | Existing | Application | Current location create contract. | Remove body tenant and raw coordinate inputs. |
| `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs` | Existing | Application | Current grouped PATCH contract. | Remove raw coordinate groups; add protected selection group. |
| `src/Explore.Application/Features/Locations/Handlers/Commands/*LocationCommandHandler.cs` | Existing | Application | Validate and mutate location aggregate. | Explicit construction replaces fragile flattened mapping. |
| `src/Explore.Application/Profiles/LookupMappingProfile.cs` | Existing | Application | Current flattened Location maps. | Characterize then remove unsafe create mapping if no longer needed. |
| `src/Explore.Domain/Settings/Definitions/` and `IHierarchicalSettingsResolver` | Existing/modify | Domain/Application | Lockable five-tier address-creation governance. | Instance/tenant mode plus organization grant; no user self-grant. |
| `src/Explore.Application/Contracts/Geocoding/` | New | Application | Provider-neutral geocoder, models, and selection protection. | `None` is healthy; no HTTP/NTS/provider DTOs. |
| `src/Explore.Application/Contracts/Persistence/ILocalAddressSuggestionQuery.cs` | New | Application | Bounded semantic port for scoped local suggestions. | Tenant-approved/current-org/current-creator semantics only. Must not revive the deleted `GetLocationsByCity/Country` enumeration shape. |
| `src/Explore.Application/Features/Geocoding/` | New | Application | CQRS provider/local merge, effective policy, validation, and promotion. | Manual validators; cancellation; local data never upstreamed. |
| `src/Explore.Persistence/Repositories/` address query | New/modify | Persistence | SQL tenant/visibility filtering before exact PII projection. | Provider-neutral across all five primary databases. |
| `src/Explore.Infrastructure/Geocoding/` | New | Infrastructure | Photon HTTP and Data Protection adapters. | `HttpClientFactory`, resilience, PII-safe telemetry. |
| `src/Explore.API/Controllers/GeocodingController.cs` | New | API | Authenticated private autocomplete POST. | Named route, rate limit, RFC 7807, no-store. |
| `src/Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs` | Existing | BFF | Existing API/YARP proxy, token and tenant boundary. | Reuse; no bespoke geocoding BFF endpoint. |
| `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.*` | New | Blazor | Accessible address selection. | Combobox/listbox, debounce/cancel, attribution, RTL. |
| `src/Explore.Blazor.Client/Pages/Admin/Dialogs/*LocationDialog*` | Existing | Blazor | Manual location create/edit experience. | Remove coordinate inputs; integrate protected selection. `EditLocationDialog` also hosts the private-home consent action and injects `IDialogService` (2026-08-23) — preserve both. |
| `src/Explore.Secrets/Database/PrimaryDatabaseCapabilityOptions.cs` | New, Phase 7 | Composition | Binds and validates optional primary-database capabilities. | `Database__Capabilities__Postgis` defaults false and is valid only for PostgreSQL. |
| `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | Existing | Persistence | Closed five-provider EF configuration. | Primary `ExploreDbContext` remains NTS/PostGIS-free for every provider. |
| `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryDbContext.cs` | New, Phase 7 | Persistence | Isolated optional spatial model. | Registered/migrated only for PostgreSQL plus the explicit capability. |
| `src/Explore.Persistence/Migrations/PostgisDiscovery/` | New, generated in Phase 7 | Persistence | Optional PostGIS schema and snapshot. | Uses `__EFPostgisDiscoveryMigrationsHistory`; never hand-edit generated files. |
| `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryTransactionCoordinator.cs` | New, Phase 7 | Persistence | Enlists a short-lived spatial context in the primary UoW transaction. | Same `DbConnection` + `DbTransaction` via `UseTransactionAsync`; never owns commit/rollback/retry. |
| `src/Explore.Persistence/Spatial/` | New, Phase 7 | Persistence | Semantic point store, transactional coordinator, and optional PostGIS occurrence-query adapter. | Persistence owns NTS; never Domain/Application. |
| `src/Explore.AppHost/AppHost.cs` | Existing | DevOps | Local topology and PostgreSQL 18 resource. | Plain PostgreSQL remains default; provide a pinned, explicit PostGIS opt-in only after approval. |
| `docker-compose.yml` | Existing | DevOps | Self-hosted topology. | Map `DATABASE_CAPABILITIES_POSTGIS` explicitly; do not imply extension availability from provider choice. |
| `schemas/openapi_islamu-event.json` | Generated | Contract | Canonical API schema. | Regenerate, never hand-edit. |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Generated | Blazor | Generated API client. | Regenerate, never hand-edit. |
| `tests/Event.Architecture.Tests/DiscoveryPostgisSeparationArchitectureTests.cs` | Existing | Tests | Current negative proof that PostGIS runtime is absent. | Rebaseline only after activation to positive privacy/layer guards. |
| `tests/Event.Architecture.Tests/EventLocationDisclosureConvergenceTests.cs` | Existing | Tests | Closes the disclosure evaluator to a documented authority set. | Geocoding/discovery must route through `IEventLocationDisclosureService`, never the evaluator. |
| `tests/Event.API.IntegrationTests/Privacy/OutboundProducerPrivacyTests.cs` | Existing | Tests | Negative audit of outbound producer families. | Listed directories must not reference `Location.Address/Postcode/Latitude/Longitude`. |
| `tests/Event.Architecture.Tests/EventLocationSchemaContractionTests.cs` | Existing | Tests | Pins the five-provider contraction migration. | Phase 3/7 migrations extend that head; four add/drop pairs per provider. |
| `dev/zarchive/event-location-privacy/` | Archived 2026-08-23 | Dev docs | Authoritative PII disclosure/erasure/remediation workstream, now shipped. | `ELP-230C/515/520/530/730` are complete; treat as evidence, not pending dependencies. |
| `dev/active/home-discovery-experience/` | Existing | Dev docs | Authoritative area-only Home and deferred PostGIS phase. | Preserve area-only behavior; cross-link instead of duplicating. |

## Key Decisions

1. **Autocomplete is independent of PostGIS/maps.** It is the first useful vertical slice.
2. **Providers are optional.** `Geocoding:Provider=None` and `Maps:Provider=None` are healthy. Application owns one geocoder port; first opt-in adapter is Photon with a 5-second total timeout, two retries, and 200/500 ms delays.
3. **Protected stateless selections.** Data Protection prevents browser coordinate/result tampering without server session storage.
4. **Atomic governed address bundle.** Manual edits clear coordinates and require effective policy plus actor authorization; geocoded selection sets all fields together.
5. **Trusted tenancy only.** Remove request-body tenant ID and raw coordinate writes; no compatibility shims.
6. **Existing API/YARP path.** No custom BFF endpoint or browser provider credentials.
7. **PostGIS is an opt-in capability, not a provider default.** `Database__Capabilities__Postgis=false` is the safe default; PostgreSQL without the extension behaves like every other database-neutral provider.
8. **Isolated optional migration chain.** `PostgisDiscoveryDbContext` and its dedicated history own the governed projection; no spatial property/index enters `ExploreDbContext` or `LocationPii`, and no automatic publication/backfill occurs.
9. **Semantic port, one exact adapter.** Application requests nearby occurrences without naming PostGIS; the optional PostGIS adapter is the only planned exact engine and uses eligible future occurrences with stable ordering.
10. **One transaction owner.** `EfCoreUnitOfWork` owns begin/commit/rollback/retry; a fresh spatial context shares the primary connection and transaction via `UseTransactionAsync`. No `TransactionScope`, nested/distributed transaction, second connection, or async cleanup.
11. **No exact public points.** API returns rounded distance/safe occurrence metadata only; origin is transient and private.
12. **Scoped local lifecycle.** Source (`provider`/`manual`) is separate from visibility (`creator`/`organization`/`tenant-approved`); reuse `CreatedBy`, filter in SQL, promote via HAL, never upstream local data.
13. **Google rule fixed, adapter deferred.** Google Places may use Google Maps or no map only; non-Google map pairing fails closed. No-map results still carry Google branding. Storage/EEA/billing remain gated.
14. **One optional initial map integration.** MapLibre proceeds only after license, self-hosting, privacy, performance, cost, wrapper, and fallback gates; list/form remain complete.
15. **Martin/other providers deferred.** No auto-publication or provider/map abstraction until concrete need; future GeoNames satisfies CC BY 4.0 attribution/change notice, OSM use satisfies ODbL attribution/separation.

## Constraints And Rules To Remember

- Critical project invariants and `docs/QUICK_REFERENCE.md` outrank the report.
- Domain → Application → Infrastructure/Persistence → API/Blazor dependencies only.
- Repositories return entities; query/projection gateways use bounded Application-owned results, not Persistence rows.
- Validators are manually instantiated.
- GET defaults do not override privacy: address/origin operations intentionally use authenticated POST bodies.
- Write routes are `[Authorize]`; private/exact reads/writes use `private, no-store` and no output cache/ETag.
- HAL links are the only UI action-capability source.
- Every source/doc file starts with two `ABOUTME:` lines.
- EF migrations/snapshots and OpenAPI/generated clients are generated artifacts.
- No backward-compatibility aliases, dual contracts, or exact-distance fallbacks.
- Address autocomplete, normalized address storage, map rendering, and area-only discovery must not require PostgreSQL or PostGIS; geocoding and map providers may both be `None`.
- Manual address writes require resolved instance/tenant/organization policy plus server actor authorization; HAL is the UI authority.
- Local exact suggestions are tenant plus tenant-approved/current-organization/current-creator filtered before projection. Approval changes visibility, never source.
- Application-owned custom addresses never enter provider requests/imports/exports/datasets.
- Future `GooglePlaces` allows `GoogleMaps` or `None` only; other map pairing disables Google and reports a clear admin warning. Do not expose an unimplemented Google value.
- `Database__Capabilities__Postgis` defaults false. True is valid only with the PostgreSQL provider, and exact mode additionally requires successful PostGIS readiness.
- The primary `ExploreDbContext` model and migration history remain PostGIS/NTS-free; optional spatial migrations run only through the isolated context.
- ADR-013 must be `Accepted` with the actual date and named decider/role before Phase 7 spatial code begins.
- Cross-context lifecycle writes run inside the existing primary UoW execution-strategy delegate and share its exact `DbConnection`/`DbTransaction`; only the primary UoW may commit, roll back, or retry.
- Application contracts describe location discovery semantics, never database providers; do not add a provider factory or pretend another provider offers exact spatial behavior.
- No address, token, origin, coordinate, secret, tenant/location ID, SQL, or upstream payload in logs/traces/metrics/readiness/errors.
- A local primary PostGIS image/profile is opt-in only; do not alter Keycloak, Cerbos, Formbricks, or privacy-authority databases.

## External Research Record

| Topic | Primary source | Decision impact |
|---|---|---|
| Npgsql spatial mapping | https://www.npgsql.org/efcore/mapping/nts.html | Add the matching NTS plugin in Persistence and configure it only for the optional `PostgisDiscoveryDbContext`, with explicit geography mapping. |
| EF Core cross-context transactions | https://learn.microsoft.com/en-us/ef/core/saving/transactions | Both contexts must share the exact `DbConnection` and `DbTransaction`; enlist the spatial context with `UseTransactionAsync`. |
| Google Places policies | https://developers.google.com/maps/documentation/places/web-service/policies | Places content on a map requires Google Maps; no-map display is allowed with adjacent Google branding/attribution. Future matrix rejects non-Google maps. |
| Google Place IDs | https://developers.google.com/maps/documentation/places/web-service/place-id | Place IDs may be stored and should be refreshed when stale; other Places content follows stricter policy, so use field-level retention rather than a blanket 30-day rule. |
| Google session pricing | https://developers.google.com/maps/documentation/places/web-service/session-pricing | Future adapter must own session token/termination/field-mask semantics. |
| Photon | https://github.com/komoot/photon | Public demo is not production; self-hosting has material data/RAM/disk/update cost. |
| Pelias docs | https://github.com/pelias/documentation | Deferred until measured coverage need and operations approval. |
| Martin configuration | https://maplibre.org/martin/config-file | If ever adopted: explicit allowlist and `auto_publish: false`. |
| Martin PostgreSQL tables | https://maplibre.org/martin/sources-pg-tables | Connection-only auto-discovery can publish all spatial tables/columns; never use on application schema. |
| OSM tile policy | https://operations.osmfoundation.org/policies/tiles/ | Public OSM tiles are not a default production basemap/SLA. |
| OSM data license / collective databases | https://www.openstreetmap.org/copyright and https://osmfoundation.org/wiki/Licence/Community_Guidelines/Collective_Database_Guideline_Guideline | ODbL attribution/share-alike applies to OSM/derived database use; independent local event data stays separately licensed when not merged into an OSM-derived dataset. |
| PMTiles | https://github.com/protomaps/PMTiles | The format/reference runtime is BSD-3-Clause, but underlying map-data/style/font/sprite licenses and attribution must be evaluated separately. |
| GeoNames dumps / CC BY 4.0 | https://download.geonames.org/export/dump/readme.txt and https://creativecommons.org/licenses/by/4.0/ | Commercial adaptation is allowed without application-code copyleft; public/redistributed use needs attribution, license link, and change indication. |
| PostGIS image | https://hub.docker.com/r/postgis/postgis | If local opt-in is approved, pin the PG18/PostGIS image/tag/digest and follow PG18 volume rules; plain PostgreSQL stays the default. |

Tavily direct search/extraction supplied the verified primary-source text, including the CTO review follow-up for EF cross-context transactions, tile candidates, and GeoNames licensing. Context7 remains unavailable, so no Context7 evidence is claimed.

## Validation Baseline

For every implementation phase, run one Release build and at most one selected non-browser project test, once after phase tasks:

| Phase | Selected test project |
|---|---|
| 1 | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 2 | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 3 | `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` |
| 4 | `tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` |
| 5 | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` |
| 6 | `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` |
| 7 | `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` |
| 8 | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` |
| 9 | `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` |

Do not run live app/browser/Aspire/Docker as an automated phase gate. Real PostGIS behavior is proven inside the Persistence integration project using its container fixture after that fixture is intentionally updated.

## Current Known Risks / Unknowns

- **1.1:** ADR-013 must first codify the optional capability/migration boundary while remaining `Proposed`.
- **1.2:** Real flattened AutoMapper create behavior is not characterized by current mocked handler tests.
- **2.1–3.2:** Conservative setting defaults, organization context, source/visibility backfill, all-provider query translation, and cross-tenant/org/creator exact-PII isolation need implementation evidence.
- **4.1:** Photon production sizing, regional/planet dataset construction, update swap, and ownership; optional `None` needs no endpoint.
- **7.1:** The activation decider/date do not exist yet; the implementation owner may record a supplied decision but may not self-grant acceptance.
- **7.2:** A self-hoster can truthfully enable the flag only after the selected PostgreSQL service has the extension available/activated; managed and serverless activation procedures vary, and Aspire image override APIs require current-doc revalidation.
- **7.3:** Optional migrations must never leak PostGIS types, SQL, packages, or extension checks into the primary migration chain or capability-off startup.
- **7.5:** The shared-connection/`UseTransactionAsync` design is selected; integration must still prove same transaction identity, dual commit/rollback, transient retry with a fresh spatial context, cancellation, and zero spatial access while disabled.
- **8.1:** Representative data volume/query plan and cursor semantics.
- **9.1:** No candidate has passed the license, attribution, self-hosting, privacy, p95 performance, first-render, cost, and overage gate.
- **Deferred:** Google field-level storage/EEA/session/billing and adapter/UI scope (pairing already resolved), Pelias coverage benefit, GeoNames quality plus CC BY 4.0 attribution/change notice, OSM-derived database compliance, Martin safe dataset.

## Unrelated Worktree Guidance

- The repository is shared and may contain user/other-agent changes. Do not revert, reformat, or include unrelated files.
- This planning session intentionally changed only the three files under `dev/active/address-geocoding-and-spatial-discovery/`.
- Existing active workstreams remain authoritative and were not rebaselined.

## Handoff Notes

### Handoff — 2026-08-12 Europe/Brussels

- **Current state:** Senior CTO RC-1 through RC-5 plus licensing/provider-optionality/governed-local-address feedback are incorporated; 0/28 implementation tasks started.
- **Next action:** Confirm the rebaseline or approve implementation, then codify the resolved capability boundary in Task 1.1 and implement Phase 1 only.
- **Blockers:** ADR activation with named decider/date, Photon production/regional topology only if Photon is enabled, a tile source passing Phase 9 only if maps are enabled, Google legal/retention/budget before its deferred phase, and Context7 availability. Provider `None`, Google pairing, local governance, PostGIS optionality, and transaction mechanism are resolved designs.
- **Modified files:** The three workstream planning artifacts only.
- **Validation:** The 2026-08-12 licensing/governance re-baseline started from a Release build passing 37 projects with 0 errors/warnings. Final QA proved 28/28 task-name parity, 9/9 phase-name parity, one build/one selected test per phase in both plan and ledger, no stale task/phase counts, and clean scoped diffs.
- **Documentation impact:** Planning docs created; canonical docs remain unchanged until implementation decisions/actions occur.
- **Risks:** Cross-tenant/org local-address leakage, policy self-escalation, provider-license contamination, PII leakage, optional-migration leakage, managed PostGIS mismatch, incorrect shared-transaction retry behavior, Photon load, and tile license/privacy/performance/cost failure.
- **Notes for next contributor/agent:** Reuse the existing settings cascade and `CreatedBy`; keep source separate from visibility; filter local exact PII in SQL; never upstream local data; keep `None` healthy; do not expose Google until its complete activation phase; retain existing API/YARP/privacy paths; keep NTS/PostGIS out Domain/Application/primary EF model; and let only `EfCoreUnitOfWork` own the spatial lifecycle transaction.
