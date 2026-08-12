<!-- ABOUTME: Repository-grounded implementation plan for private address geocoding and governed spatial event discovery. -->
<!-- ABOUTME: Defines decision gates, clean architecture boundaries, phased delivery, verification, and deferred provider choices. -->

# Address Geocoding And Spatial Discovery — Implementation Plan

Last Updated: 2026-08-11 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Read `dev/report/address_geocoding_analysis.md` and produce an enterprise-grade implementation plan using repository conventions, Clean Architecture, current external research, and no backward-compatibility burden.
- **Task directory:** `dev/active/address-geocoding-and-spatial-discovery/`
- **Planning status:** Draft, awaiting user review and the ADR-013 activation decisions in Task 1.1.
- **Matched intents:** No single intent covers this cross-cutting capability. Use the composite contract for `add-cqrs-handler`, `add-write-endpoint`, `add-get-endpoint`, `add-ef-migration`, `update-repository-query`, `openapi-contract-change`, `blazor-component-affordance`, and `external-infrastructure-bootstrap`.
- **Relevant skills:** `implementation-plan`, `agentic-research`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `error-tracking`, and `aspire`.
- **Relevant rules:** `.claude/rules/domain.md`, `application-layer.md`, `api-controllers.md`, `efcore-persistence.md`, `efcore-migrations.md`, `blazor-server.md`, `blazor-client.md`, and `tests.md`.
- **Primary layers:** Domain, Application, Infrastructure, Persistence, API, generated OpenAPI client, Blazor Client, AppHost/Compose, tests, and canonical documentation.
- **Complexity:** **XL**. The full report crosses a PII-bearing aggregate, third-party provider boundaries, authenticated API contracts, generated clients, five database providers, optional PostGIS, public discovery semantics, browser accessibility, and deployment infrastructure. The plan deliberately ships one provider and one map integration before adding alternatives.
- **Baseline:** `dotnet build --configuration Release --verbosity quiet` passed on 2026-08-11 with 37 projects, 0 errors, and 0 warnings before planning artifacts were created.
- **Research:** Tavily direct extraction verified primary vendor documentation. Context7 was requested but no Context7 connector, tool, resource, or template is available in this session; no Context7 result is claimed. This limitation is an explicit implementation-time revalidation item.

### 0.1 Composite Contribution Contract

| Concern | Planning decision |
|---|---|
| Must-read sources | `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `GOVERNANCE.md`, `ARCHITECTURE.md`, `DOMAIN.md`, `API.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `CONFIGURATION.md`, `OPERATIONS.md`, `TESTING.md`, `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`, ADR-013, the source report, matched rules, and the workstream context/tasks. |
| Paths in scope | Existing location aggregate/commands/contracts, new Application geocoding contracts, Infrastructure provider adapter, API endpoints, generated artifacts, Blazor location dialogs/components, PostgreSQL-only spatial persistence, Home Discovery query/API/UI, AppHost/Compose, tests, and named docs. |
| Minimum verification | One Release build and at most one relevant project test per implementation phase, as listed in Section 6. Generated migrations and clients are regenerated, never hand-edited. |
| Documentation obligations | Update ADR-013 and canonical architecture/domain/API/security/configuration/self-hosting/operations/Blazor/testing documents in the phase that changes the corresponding behavior. |
| Forbidden without approval | Activating ADR-013, publishing exact venue geometry, changing every PostgreSQL deployment to require PostGIS, using public Photon or public OSM tiles as a production dependency, storing browser origins, sending raw address queries to logs/URLs, or adopting Google terms/billing. |
| Compatibility posture | Development mode: remove obsolete request fields and generated client shapes directly. Do not add aliases, dual contracts, or shims. |

## 1. Executive Summary

Deliver address autocomplete as an authenticated, private, server-mediated extension of the existing `Location`/`LocationPii` workflow. A normalized selection is returned with a short-lived, tamper-evident token; location commands consume that token and update the address bundle and coordinate pair atomically. Manual address entry remains available, but a manual address change clears stale coordinates. Tenant identity always comes from `ITenantContext`, never the request body.

After separate approval of ADR-013, add exact proximity only for PostgreSQL deployments configured for `postgis`. Exact PII coordinates remain in `LocationPii`; a separately approved `LocationDiscoveryPoint` persistence projection is created only by an authorized action. Nearby discovery remains occurrence-based, performs `ST_DWithin`/`ST_Distance` in PostgreSQL, returns distance and safe occurrence metadata but no point, and keeps the user's origin transient in a private `POST` request.

The first production provider is a self-hosted or operator-contracted Photon endpoint. The API uses the existing BFF/YARP path, so no bespoke BFF endpoint or new geocoding project is added. One accessible MapLibre-based component may be added only after an operator-approved basemap source and wrapper compatibility gate. Google Places/Google Maps, Pelias, the native GeoNames/SQLite dataset, Leaflet, and Martin are deferred until their distinct legal, operational, or product trigger exists.

### Explicit Non-Goals

- No `Point` property or spatial index on `LocationPii`.
- No generic spatial-provider abstraction or non-PostGIS exact-distance fallback.
- No four-provider implementation, three map libraries, or `IMapComponent` hierarchy in the first delivery.
- No direct production dependency on `photon.komoot.io`, `tile.openstreetmap.org`, MapLibre demo tiles, or an unpinned container tag.
- No exact coordinates in generic public location, Home Discovery, federation, AI, tile, log, trace, metric, cache-key, or analytics contracts.
- No separate `Explore.Geocoding` or BFF endpoint project while existing Application/Infrastructure/API/YARP ownership is sufficient.
- No automatic publication/backfill from `LocationPii` to discovery.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Exact address and coordinates are already isolated in a hard-deleteable 1:1 PII table. | `src/Explore.Domain/Location.cs`, `LocationPii.cs`, `LocationPiiConfiguration.cs`. | High | `Location` exposes non-mapped proxies; `LocationPii` stores address, postcode, latitude, and longitude. |
| Current create mapping is not proven end-to-end. | `CreateLocationCommandHandler.cs`, `LookupMappingProfile.cs`, and handler tests. | High | AutoMapper maps flattened fields into proxy setters that require attached PII, while tests mock `IMapper`. Characterize before changing the flow. |
| Create authorization currently reads a body-controlled tenant ID while persistence overwrites it from context. | `CreateLocationDto.cs`, `CreateLocationCommand.cs`, `CreateLocationCommandHandler.cs`. | High | Remove the body tenant field; one trusted tenant source is required. |
| Existing CRUD is authenticated, tenant-aware, concurrency-protected, and private/no-store on exact reads. | `LocationController.cs`, location commands/handlers, `LocationLinkPolicy.cs`. | High | Reuse these boundaries and HAL affordances. |
| Existing YARP BFF already proxies `/api/*` and owns browser-token forwarding. | `src/Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs`. | High | A separate `Explore.BFF/Endpoints` implementation proposed by the report is unnecessary. |
| Current public Home Discovery is area-only and explicitly strips exact discovery fields. | Home Discovery workstream and `DiscoveryPostgisSeparationArchitectureTests.cs`. | High | Exact proximity is planned but absent. |
| ADR-013 is Proposed and has a separate activation gate. | `docs/adr/ADR-013-postgis-proximity-discovery.md`. | High | A planning request is not silent approval to activate the schema. |
| The repository supports PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. | `PrimaryDatabaseProvider.cs`, `PrimaryDatabaseProviderComposition.cs`. | High | `postgis` can be valid only on PostgreSQL; other providers keep `area_only`/`disabled`. |
| PostgreSQL 18 is current, not PostgreSQL 17. | `AppHost.cs`, `docker-compose.yml`. | High | Only the primary application database may move to a PostGIS image. |
| Npgsql spatial support requires its NetTopologySuite plugin and explicit geography mapping. | [Npgsql spatial mapping](https://www.npgsql.org/efcore/mapping/nts.html). | High | Pin the plugin to the installed Npgsql provider version and call `UseNetTopologySuite()` only in the Npgsql branch. |
| Google Autocomplete sessions terminate with Place Details or Address Validation; incomplete sessions have different billing. | [Google session pricing](https://developers.google.com/maps/documentation/places/web-service/session-pricing). | High | A Google adapter needs explicit session lifecycle and field-mask design. |
| Google Places content has caching/attribution restrictions; place IDs have a storage exception and EEA terms differ. | [Google Places policies](https://developers.google.com/maps/documentation/places/web-service/policies). | High | The report's simple compatibility matrix is not an adequate legal design. |
| Martin connection-only configuration can publish every readable spatial table and every non-geometry column as feature tags. | [Martin table sources](https://maplibre.org/martin/sources-pg-tables), [configuration](https://maplibre.org/martin/config-file). | High | Never point auto-discovery at the application schema or `LocationPii`. |
| Public OSM raster tiles are not a production SLA and require attribution, identification, and caching. | [OSMF tile policy](https://operations.osmfoundation.org/policies/tiles/). | High | Select a contracted/self-hosted tile source before production map rollout. |
| Context7 cannot be queried in this session. | Tool/resource inventory inspected on 2026-08-11. | High | Revalidate framework/package details through Context7 if it becomes available before dependency adoption. |

### 2.2 Existing Implementation

#### Domain And Application

- `Location` is the tenant aggregate and owns privacy state, owner consent, concurrency, and irreversible PII erasure.
- `LocationPii` owns the exact address and nullable scalar coordinate pair. It has no provider provenance or spatial type.
- Create validates, AutoMaps, replaces `TenantId` from `ITenantContext`, then persists. Update loads the tracked aggregate with PII, checks `If-Match`, and applies independent PATCH groups.
- Existing coordinate validators enforce numeric ranges but do not guarantee a finite both-or-none pair or clear coordinates when address text changes.
- `EventLocation` disclosure services already centralize purpose-based exact disclosure, audit, and authorization. Geocoding and discovery must not create a parallel disclosure path.

#### Persistence And Database Composition

- `LocationRepository` returns entities, auto-includes PII for authorized management reads, uses no-tracking for lists, and physically deletes PII through `ForgetPiiAsync`.
- Tenant filters cover `Location`; `LocationPii` inherits isolation through its `Location` relationship.
- Provider configuration is a closed switch. PostgreSQL uses Npgsql; four other providers own separate migrations assemblies.
- PostgreSQL migrations currently live in `Explore.Persistence`. No PostGIS extension, NTS package, geography column, GiST index, or spatial readiness check exists.
- Primary AppHost and Compose images are `postgres:18-alpine`. The Compose primary volume target is `/var/lib/postgresql/data`; a PostGIS PG18 image changes the required mount target and therefore needs an explicit development reset or production backup/restore procedure.

#### API, BFF, And Blazor

- `LocationController` exposes authorized CRUD with named routes, RFC 7807 errors, strong `If-Match` on PATCH, and private no-store exact reads.
- HAL `create`, `edit`, and `delete` relations are the UI capability source. `TenantLookupTablesSection.razor` still requires correction because it renders location actions unconditionally.
- The generated `EventApiClient.g.cs` is the Blazor API boundary. Existing YARP forwarding hides server credentials and injects trusted auth/tenant context.
- Create/Edit dialogs use plain MudBlazor fields and permit manual latitude/longitude entry. There is no accessible autocomplete combobox or map component.

#### Discovery And Privacy

- Area-only Home Discovery is implemented and cacheable. It uses stable coarse areas and never reads exact PII.
- `EventDiscoveryItemDto` already has dormant distance/nearest-occurrence fields that area-only mapping deliberately clears.
- `DiscoveryPostgisSeparationArchitectureTests` currently proves the spatial runtime is absent. It must be rebaselined into positive boundary tests only after ADR activation.
- Event Location Privacy Tasks `ELP-515`, `ELP-520`, `ELP-530`, and `ELP-730` own erasure/correction/remediation dependencies. This workstream integrates with them; it does not create a second erasure saga.

### 2.3 Existing Tests And Verification Coverage

| Project / file | Existing protection | Planned extension |
|---|---|---|
| `Event.Application.UnitTests/.../Locations` | Validator and command behavior with mocked mapping/repositories. | Add real construction/mapping characterization, trusted tenancy, atomic address/coordinate behavior, and geocoding orchestration. |
| `Event.API.IntegrationTests/Features/LocationControllerTests.cs` | Authenticated CRUD and contract behavior. | Add private POST autocomplete/selection contracts, rate limiting, no-store, and removed request fields. |
| `Explore.Infrastructure.Tests` | Provider/HTTP infrastructure conventions. | Add Photon request/response, cancellation, timeout, retry, attribution, and redaction tests. |
| `Explore.Blazor.Client.Tests` location/admin tests | Location service/dialog behavior. | Add combobox semantics, debounce/cancellation, selection/manual fallback, error recovery, and HAL gating. |
| `Event.Persistence.IntegrationTests` | PostgreSQL provider composition, privacy erasure, location isolation. | Add real PostGIS migration, index, projection approval/revocation/erasure, and exact query-shape tests. |
| `Event.Architecture.Tests/DiscoveryPostgisSeparationArchitectureTests.cs` | Negative proof that spatial runtime is absent. | Rebaseline to keep NTS out Domain/Application, PII out discovery, and exact coordinates out public/tile contracts. |

### 2.4 Existing Documentation And Contracts

- Canonical: `docs/ARCHITECTURE.md`, `DOMAIN.md`, `API.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `CONFIGURATION.md`, `SELF_HOSTING.md`, `OPERATIONS.md`, `TESTING.md`, `BLAZOR.md`, `DESIGN_SYSTEM.md`, and `ACCESSIBILITY.md`.
- Decision: `docs/adr/ADR-013-postgis-proximity-discovery.md`.
- Generated contracts: `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, and `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- Overlapping workstreams: `dev/active/event-location-privacy/` and `dev/active/home-discovery-experience/`.
- Source analysis: `dev/report/address_geocoding_analysis.md`; it is research input, not higher authority than current ADRs and repository rules.

### 2.5 Current Pain Points / Improvement Areas

1. Location creation relies on an uncharacterized flattened AutoMapper path into PII proxy setters.
2. The request body contains a tenant field even though tenant context is authoritative.
3. Address and coordinate fields can drift independently, leaving stale coordinates after manual edits.
4. Manual coordinate entry trusts browser input and does not preserve provider provenance.
5. No bounded, authenticated geocoding boundary exists; sending address fragments in a GET would leak PII into URLs and access logs.
6. The report's Phase 0 makes PostGIS/Martin a dependency of autocomplete even though autocomplete has no spatial-database dependency.
7. The report's PostGIS 17 image and direct Martin URL do not match PostgreSQL 18, current browser routing, or the five-provider database contract.
8. Martin auto-publication would bypass tenant/application authorization and disclose exact vector geometry.
9. Three map wrappers and four provider adapters would multiply bundle size, operations, test matrices, and legal surface before demand is proven.
10. Current location admin actions are not consistently HAL-gated.

### 2.6 Unknowns After Investigation

| Unknown | Investigation performed | Owning resolution |
|---|---|---|
| Whether all PostgreSQL deployments may require PostGIS. | Compared ADR-013, provider matrix, migrations ownership, AppHost/Compose, and Npgsql docs. | Task 1.1 decision. Recommended because development mode avoids a second DbContext/migration chain; reject it only with an explicit separate-spatial-database design. |
| Production Photon topology and sizing. | Reviewed Photon repository policy and report sizing; public instance is unsuitable for production and planet data is large. | Task 3.1 operator decision and benchmark. |
| Exact Google storage/display obligations for the intended EEA flow. | Reviewed Google Places policies and session pricing. | Deferred Google gate; legal/product review required before a task is added. |
| Which MapLibre integration is maintainable on the current .NET/Blazor stack. | Reviewed current package metadata; wrapper adoption/maturity is not proven in this repo. | Task 8.1 compatibility gate. |
| Production basemap/tile source. | Reviewed OSMF policy and Martin behavior. | Task 8.1 operator decision; public OSM/demo tiles are forbidden defaults. |
| Context7 documentation verification. | No connector/tool/resource is installed. | Re-run before the first new NuGet/UI dependency is accepted, if available. |

## 3. Proposed Future State

### 3.1 Address Selection Flow

1. An authorized location editor types at least the configured minimum characters into an accessible combobox.
2. Blazor debounces and cancels superseded calls, then sends a first-party `POST` through the generated client and existing YARP BFF.
3. API rate limiting and authorization run before MediatR. The Application query calls `IAddressGeocoder`; Infrastructure calls the configured Photon endpoint through a named/typed `HttpClient` with bounded resilience.
4. Each suggestion contains safe display fields, attribution, exact coordinates only for the authorized editor, and a short-lived protected selection token. Raw queries and response bodies are not logged or cached.
5. Selecting a result submits the protected token in the normal create/PATCH command. Application unprotects it, rejects expiry/provider/config mismatches, and calls an aggregate method that atomically updates address, postcode, city, country, latitude, and longitude.
6. Manual entry remains valid but stores no coordinate pair. Editing any address component manually clears the old pair and requires a new geocoding selection before discovery approval.

### 3.2 Governed Discovery Flow

1. An authorized administrator sees a HAL approval action only when PostgreSQL/PostGIS readiness is healthy, exact PII coordinates form a valid pair, and privacy policy permits approval.
2. The approval command explicitly snapshots the selected coordinates into the tenant-scoped `location_discovery_points` persistence projection with approval evidence. There is no automatic copy or bulk default.
3. Coordinate changes, privacy erasure, revocation, or invalidation remove/deactivate the projection in the same database transaction and preserve bounded audit evidence.
4. After an explicit browser action, the client rounds the user's origin and sends it once in a private/no-store `POST` with bounded radius and filters.
5. PostgreSQL applies tenant/public/future-occurrence predicates and `ST_DWithin` before `ST_Distance`, selects the minimum eligible occurrence per event, and orders by distance, occurrence time, then event ID.
6. The API returns rounded distance and safe occurrence/location identifiers/names, never either point. Home Discovery renders the list and uses only honest proximity wording when `postgis` readiness is healthy.

### 3.3 Operator Experience

- `Geocoding:Provider=Photon` requires an approved endpoint and explicit production mode; public/demo endpoints fail production validation.
- `Discovery:Mode=postgis` is accepted only with `Database:Provider=PostgreSql` and healthy extension/table/index/query readiness. Other provider/mode combinations fail fast with actionable, non-sensitive errors.
- Image tags and package versions are centrally pinned; lock files are regenerated.
- Metrics expose provider, outcome, latency bucket, rate-limit/retry category, and spatial query health without query text, address, origin, coordinate, tenant, or location identifiers.

## 4. Non-Negotiable Constraints

1. `LocationPii` remains the only private exact-location source; generic DTOs remain coordinate-free unless already purpose-authorized management contracts.
2. Repositories return entities. Query-specific provider/projection gateways return bounded Application-owned result models and never Persistence rows.
3. Domain and Application do not reference Npgsql, PostGIS, or NetTopologySuite. Spatial types remain in Persistence.
4. Validators are manually instantiated in handlers.
5. Tenant identity comes from trusted context; user-provided tenant IDs are removed.
6. Geocoding and exact proximity use authenticated/private `POST` endpoints despite read semantics, because address/origin PII must not enter URLs or shared caches.
7. Write authorization is enforced server-side and exposed to Blazor through HAL affordances; the UI never inspects roles/claims locally.
8. No origin, raw address query, exact coordinate, protected selection token, or provider credential is logged, traced, metered as a label, cached, or persisted outside its approved store.
9. PostGIS is the only exact proximity engine. No browser/in-memory/Haversine/provider fallback is allowed.
10. EF migrations and snapshots, OpenAPI schema, API inventory, and generated client are regenerated, never hand-edited.
11. Breaking contract changes are direct. No obsolete field aliases, dual endpoints, or compatibility adapters.
12. Every new file starts with two `ABOUTME:` lines.

## 5. Architecture And Design Decisions

### Decision 1: Autocomplete ships before spatial discovery

- **Decision:** Deliver a complete Photon-backed address-selection slice independently of PostGIS and maps.
- **Why:** Geocoding improves location data entry immediately and does not require a spatial database or tile server.
- **Alternatives considered:** Report order with PostGIS/Martin first; rejected as unnecessary coupling.
- **Consequences:** Phases 1–5 can ship while ADR-013 remains unactivated.
- **Files/layers:** Location Domain/Application, Infrastructure geocoder, API, generated client, Blazor.

### Decision 2: One Application port, one initial adapter

- **Decision:** Application owns `IAddressGeocoder` and normalized models; Infrastructure owns Photon. Do not add `Explore.Geocoding`.
- **Why:** This preserves dependency direction while keeping project count and registrations small.
- **Alternatives considered:** Direct provider use in handlers; four adapters up front; separate project. Rejected for coupling or speculative scope.
- **Consequences:** Provider selection is a closed validated option. A second adapter can reuse the port when approved.
- **Files/layers:** Application contracts/features; Infrastructure adapter/composition.

### Decision 3: Protected stateless selection tokens

- **Decision:** API mints short-lived ASP.NET Core Data Protection tokens containing the normalized provider result and provenance; location commands unprotect them server-side.
- **Why:** This prevents browser tampering without server-side session state or a second provider call.
- **Alternatives considered:** Trust browser coordinates; cache server sessions; resolve every suggestion again. Rejected for integrity, PII-state, or latency reasons.
- **Consequences:** Tokens are private/no-store, purpose-bound, time-bounded, and invalid after provider/config version changes.
- **Files/layers:** Application protection abstraction; Infrastructure Data Protection implementation; API DTOs; location commands.

### Decision 4: Address bundles change atomically

- **Decision:** Add aggregate methods for manual and geocoded address changes. Manual changes clear coordinates; geocoded changes require a finite both-or-none pair.
- **Why:** Independent property setters permit stale or half-valid location state.
- **Alternatives considered:** Validator-only checks; keep raw coordinate PATCH fields. Rejected because aggregate invariants must hold for every caller.
- **Consequences:** Remove manual latitude/longitude create/PATCH fields and the fragile flattened create mapping.
- **Files/layers:** `Location`, location DTOs/validators/handlers/profile, generated client/UI.

### Decision 5: Existing API and BFF path is authoritative

- **Decision:** Add API controllers/operations and consume them through the generated client and existing YARP proxy.
- **Why:** Token forwarding, tenant context, antiforgery, and secret isolation already exist.
- **Alternatives considered:** Report's new BFF endpoints. Rejected as duplicate routing/orchestration.
- **Consequences:** No browser provider credentials and no BFF-specific geocoding service.
- **Files/layers:** API, OpenAPI, Event Web BFF unchanged except tests if forwarding policy needs a new assertion.

### Decision 6: PostGIS is mandatory only for PostgreSQL after approval

- **Decision:** Recommended ADR amendment: all PostgreSQL deployments use a pinned PostGIS-capable PostgreSQL 18 image/extension, while `Discovery:Mode` still controls feature activation. SQLite, SQL Server, MariaDB, and MySQL retain area-only/disabled behavior and ignore the spatial row mapping.
- **Why:** Development mode permits the image/migration reset, and this avoids a second DbContext, migrations chain, and cross-context erasure transaction.
- **Alternatives considered:** Optional extension inside one PostgreSQL migration chain; separate spatial DbContext/database. The former is not truly optional; the latter adds material transactional complexity.
- **Consequences:** Task 1.1 requires explicit approval. If rejected, stop before Phase 6 and re-plan a separate spatial store.
- **Files/layers:** ADR, packages, provider composition, primary AppHost/Compose database, PostgreSQL migrations, self-hosting docs.

### Decision 7: Discovery is a governed Persistence projection

- **Decision:** Keep `LocationPii` unchanged. Persistence owns the NTS `LocationDiscoveryPoint` row and PostGIS mapping; Application owns approval/query contracts and scalar models.
- **Why:** Exact private coordinates and public-discovery purpose have different lifecycles, and Domain must not depend on database spatial types.
- **Alternatives considered:** Add `Point` to `LocationPii`; automatically copy coordinates; put NTS in Domain. Rejected for privacy and dependency violations.
- **Consequences:** Approval is explicit, revocation/erasure is transactional, and coordinate updates invalidate approval.
- **Files/layers:** Application contracts/commands, Persistence row/config/store, privacy erasure integration, HAL/API.

### Decision 8: Occurrence-level server-side proximity only

- **Decision:** Query eligible future published occurrences with PostGIS, reduce to nearest occurrence per event, and use stable distance/time/event ordering.
- **Why:** An event may have multiple locations and sessions; event-row coordinates would be incorrect.
- **Alternatives considered:** Client Haversine, event-level point, generic spatial provider. Rejected by ADR-013.
- **Consequences:** Real PostGIS tests and representative `EXPLAIN (ANALYZE, BUFFERS)` evidence are acceptance requirements.
- **Files/layers:** Application query contract/handler, Persistence query, API/Home Discovery.

### Decision 9: One map component, no map-provider hierarchy

- **Decision:** After a compatibility and tile-source gate, add one app-owned MapLibre component. Do not add `IMapProvider`, `IMapComponent`, or three renderer implementations.
- **Why:** Only one map is required, and wrapper/package maturity is not proven.
- **Alternatives considered:** Google Maps, Leaflet, and MapLibre simultaneously. Rejected due bundle, support, and test-matrix cost.
- **Consequences:** A second renderer triggers a separate design decision based on measured need.
- **Files/layers:** Blazor component/CSS/optional JS, package/lock files, safe public map config.

### Decision 10: Martin cannot publish application spatial tables

- **Decision:** Defer Martin. If a future approved coarse/aggregate tile source exists, use a read-only role, explicit allowlisted view/function, `auto_publish: false`, pinned image, and same-origin route. It never reads `LocationPii` or exact discovery points.
- **Why:** Martin's default discovery publishes every readable spatial table and feature columns, bypassing application authorization.
- **Alternatives considered:** Zero-config auto-discovery and exact venue tiles. Rejected as privacy/tenant violations.
- **Consequences:** Initial MapLibre uses an operator-approved basemap and Application API data; exact venue pins are not public.
- **Files/layers:** Deferred AppHost/Compose/Martin config/BFF work only after a new approval.

## 6. Implementation Phases

### Phase 1: Governance And Location Integrity

- **Goal:** Resolve activation decisions and leave the existing manual location workflow internally consistent, tenant-authoritative, and ready for geocoding.
- **Depends on:** User review of this plan.
- **Relevant files:** Existing ADR-013, intents registry, `Location.cs`, location DTOs/validators/commands/handlers/profile, generated contract sources, and matching Application tests.
- **Related skills/rules:** Clean Architecture, CQRS/MediatR, domain/application/API rules.
- **Acceptance criteria:** ADR decision is explicit; manual create/update works without raw coordinate writes; tenant identity is context-owned; stale coordinate states are impossible.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert only this phase's contract/domain changes. Do not begin provider work while the real create path or decision record is unresolved.

#### Task 1.1: Approve And Reconcile The Architecture Contract

- **Type:** modify
- **Layer:** Docs / Architecture
- **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md` (existing), `.claude/contract/intents.yaml` (existing, only if a reusable intent is justified), `docs/ARCHITECTURE.md` and `docs/DOMAIN.md` (existing), this workstream (existing).
- **Description:** Record whether PostgreSQL universally adopts the PostGIS-capable image after activation, confirm Photon as the first provider, approve the private token flow, and keep Martin/exact public tiles deferred. Cross-link Home Discovery Phase 6 and Event Location Privacy `ELP-730`; do not duplicate their completed work.
- **Acceptance Criteria:**
  - [ ] ADR-013 status/decision matches explicit user/product/privacy/operations approval.
  - [ ] PostgreSQL image/extension policy and non-PostgreSQL behavior are unambiguous.
  - [ ] Exact geometry/tile publication remains forbidden unless separately approved.
  - [ ] Context7 unavailability and required dependency revalidation are recorded.
- **Dependencies:** None.
- **Effort:** M
- **Required Skills/Rules:** `implementation-plan`, `clean-architecture-rules`, `agentic-research`, `AGENTS.md` Contribution Contract.

#### Task 1.2: Make Location Address And Coordinate State Atomic

- **Type:** modify
- **Layer:** Domain / Application
- **Files:** `src/Explore.Domain/Location.cs` (existing), `src/Explore.Domain/LocationPii.cs` (existing), `src/Explore.Application/DTOs/Location/CreateLocationDto.cs` (existing), `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs` (existing), `src/Explore.Application/DTOs/Location/Validators/CreateLocationDtoValidator.cs` (existing), `src/Explore.Application/DTOs/Location/Validators/UpdateLocationDtoValidator.cs` (existing), `src/Explore.Application/Features/Locations/Handlers/Commands/CreateLocationCommandHandler.cs` (existing), `src/Explore.Application/Features/Locations/Handlers/Commands/UpdateLocationCommandHandler.cs` (existing), `src/Explore.Application/Profiles/LookupMappingProfile.cs` (existing), `tests/Event.Application.UnitTests/Features/Locations/Commands/CreateLocationCommandHandlerTests.cs` (existing), `tests/Event.Application.UnitTests/Features/Locations/Commands/UpdateLocationCommandHandlerTests.cs` (existing), `tests/Event.Application.UnitTests/Features/Locations/LocationAddressInvariantTests.cs` (new).
- **Description:** Characterize the real AutoMapper path, replace fragile flattened PII construction with explicit aggregate construction, add manual/geocoded address methods, require finite coordinate pairs, and clear coordinates on manual address changes. Remove raw latitude/longitude write groups without compatibility shims; management read contracts may retain authorized coordinates.
- **Acceptance Criteria:**
  - [ ] A real mapping/construction test proves create behavior without mocked `IMapper` assumptions.
  - [ ] No aggregate can hold one coordinate without the other or non-finite values.
  - [ ] Manual address changes clear existing coordinates and any discovery approval hook is invoked later by the owning transaction.
  - [ ] Existing irreversible erasure rules remain green.
- **Dependencies:** 1.1 decision record may remain Proposed for autocomplete-only work.
- **Effort:** L
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `domain.md`, `application-layer.md`.

#### Task 1.3: Remove Body-Controlled Tenancy And Regenerate The Contract

- **Type:** modify / delete
- **Layer:** Application / API / Blazor / Docs
- **Files:** `src/Explore.Application/DTOs/Location/CreateLocationDto.cs`, `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs`, `src/Explore.Application/Features/Locations/Requests/Commands/CreateLocationCommand.cs`, `src/Explore.Application/Features/Locations/Handlers/Commands/CreateLocationCommandHandler.cs`, `src/Explore.API/Controllers/LocationController.cs`, `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `src/Explore.Blazor.Client/Services/LocationService.cs`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor`, `tests/Event.Application.UnitTests/Features/Locations/Commands/CreateLocationCommandHandlerTests.cs`, `docs/API.md`, `docs/API_CHANGELOG.md` (all existing).
- **Description:** Remove `TenantId` and raw coordinate inputs from create/PATCH schemas, authorize and persist solely from `ITenantContext`, adapt the manual UI, regenerate OpenAPI/inventory/client artifacts, and document the intentional breaking change.
- **Acceptance Criteria:**
  - [ ] No location write accepts tenant identity or coordinate pairs from an untrusted body.
  - [ ] Manual location creation/update still succeeds and stores address PII with no coordinates.
  - [ ] Generated artifacts contain only the new shapes and no aliases.
  - [ ] Location UI actions remain or become HAL-gated.
- **Dependencies:** 1.2.
- **Effort:** L
- **Required Skills/Rules:** `auth-patterns`, `api-controllers.md`, `blazor-ui-conventions`, `openapi-contract-change` intent.

### Phase 2: Application Geocoding Contract

- **Goal:** Add provider-neutral Application semantics and location-command adoption without any network or UI dependency.
- **Depends on:** Phase 1.
- **Relevant files:** New Application geocoding contracts/DTOs/queries/validators plus existing location commands/handlers and Application tests.
- **Related skills/rules:** CQRS/MediatR, Clean Architecture, application-layer rule.
- **Acceptance criteria:** Search and protected-selection behavior is provider-neutral, bounded, cancellable, and fully enforced in Application tests.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove the unused Application port/models and restore manual-only commands; no database migration exists yet.

#### Task 2.1: Define Minimal Geocoding Ports And Models

- **Type:** create
- **Layer:** Application
- **Files:** `src/Explore.Application/Contracts/Geocoding/IAddressGeocoder.cs` (new), `src/Explore.Application/Contracts/Geocoding/AddressGeocodingModels.cs` (new), `src/Explore.Application/Contracts/Geocoding/IAddressSelectionProtector.cs` (new), `src/Explore.Application/Configuration/GeocodingOptions.cs` (new), `tests/Event.Application.UnitTests/Features/Geocoding/AddressGeocodingContractTests.cs` (new).
- **Description:** Define autocomplete and normalization models, provider provenance, attribution, culture/country bias, result limits, session ID, protected selection expiry, and explicit result/error categories. Keep HTTP, NTS, Google, and Photon types out of the contract.
- **Acceptance Criteria:**
  - [ ] Contracts support search plus server-trusted selection without provider-specific DTO leakage.
  - [ ] Query length, result count, supported culture/country, coordinate finiteness, and cancellation are bounded.
  - [ ] Configuration has a closed provider set and fails invalid combinations at startup.
- **Dependencies:** Phase 1.
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `application-layer.md`.

#### Task 2.2: Implement Address Autocomplete CQRS Orchestration

- **Type:** create
- **Layer:** Application
- **Files:** `src/Explore.Application/DTOs/Geocoding/AddressAutocompleteDtos.cs` (new), `src/Explore.Application/DTOs/Geocoding/Validators/AddressAutocompleteRequestDtoValidator.cs` (new), `src/Explore.Application/Features/Geocoding/Requests/Queries/SearchAddressesRequest.cs` (new), `src/Explore.Application/Features/Geocoding/Handlers/Queries/SearchAddressesRequestHandler.cs` (new), `tests/Event.Application.UnitTests/Features/Geocoding/SearchAddressesRequestHandlerTests.cs` (new).
- **Description:** Manually instantiate validation, invoke the selected port with cancellation, protect normalized results, map safe attribution/error data, and never log input/result PII.
- **Acceptance Criteria:**
  - [ ] Empty/short/oversized input fails without provider I/O.
  - [ ] Results are bounded, ordered as returned by the provider, and contain expiring protected tokens.
  - [ ] Timeout/unavailable/limited outcomes are distinguishable without exposing upstream bodies.
- **Dependencies:** 2.1.
- **Effort:** M
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `application-layer.md`.

#### Task 2.3: Consume Protected Selections In Location Commands

- **Type:** modify
- **Layer:** Application / Domain
- **Files:** `src/Explore.Domain/Location.cs`, `src/Explore.Application/DTOs/Location/CreateLocationDto.cs`, `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs`, both location validators, `src/Explore.Application/Features/Locations/Requests/Commands/CreateLocationCommand.cs`, `src/Explore.Application/Features/Locations/Requests/Commands/UpdateLocationCommand.cs`, both location command handlers, `tests/Event.Application.UnitTests/Features/Locations/Commands/CreateLocationCommandHandlerTests.cs`, and `tests/Event.Application.UnitTests/Features/Locations/Commands/UpdateLocationCommandHandlerTests.cs` (all existing).
- **Description:** Add an optional protected selection to create/PATCH. Unprotect before persistence, reject expiry/purpose/provider mismatch, then atomically apply the normalized bundle. Preserve manual fallback with coordinates cleared.
- **Acceptance Criteria:**
  - [ ] Tampered, expired, wrong-purpose, and invalid-coordinate tokens fail closed.
  - [ ] Successful selection writes a complete address and coordinate pair once.
  - [ ] No external provider call or unprotect operation occurs inside a database transaction.
  - [ ] Concurrency and privacy-erasure behavior remain authoritative.
- **Dependencies:** 2.1, 2.2.
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, `domain.md`.

### Phase 3: Photon Infrastructure Adapter

- **Goal:** Implement one production-capable geocoder adapter with validated topology and privacy-safe resilience.
- **Depends on:** Phase 2 and operator selection of a self-hosted/contracted Photon endpoint.
- **Relevant files:** New Infrastructure geocoding adapter/composition, existing configuration/secrets/Aspire/Compose docs, lock files, Infrastructure tests.
- **Related skills/rules:** Agentic research, error tracking, Aspire, configuration/security rules.
- **Acceptance criteria:** Photon calls are bounded, resilient, cancellable, observable without PII, and forbidden from using the public demo in production.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable geocoding and retain manual entry. No location data is lost.

#### Task 3.1: Fix The Photon Deployment Contract

- **Type:** investigate / modify
- **Layer:** DevOps / Docs
- **Files:** `src/Explore.AppHost/AppHost.cs` (existing, if locally hosted), `docker-compose.yml` (existing, if Compose-hosted), `.env.example` (existing), `docs/CONFIGURATION.md`, `SECRETS.md`, `SELF_HOSTING.md`, `OPERATIONS.md` (existing).
- **Description:** Benchmark the required regional/planet data footprint, choose external versus self-hosted topology, pin image/data versions and checksums, define update/swap/rollback capacity, and reject `photon.komoot.io` in production validation. Do not add a planet-scale container to lightweight profiles by default.
- **Acceptance Criteria:**
  - [ ] Production endpoint ownership, capacity, update cadence, health URL, TLS, backup/rebuild, and failure mode are documented.
  - [ ] Local-full may opt into the heavy service; local-default/core/lite remain lightweight.
  - [ ] No public demo endpoint is an implicit production fallback.
- **Dependencies:** 1.1 provider decision.
- **Effort:** L
- **Required Skills/Rules:** `agentic-research`, `aspire`, `external-infrastructure-bootstrap` intent.

#### Task 3.2: Implement Photon And Selection Protection Adapters

- **Type:** create
- **Layer:** Infrastructure
- **Files:** `src/Explore.Infrastructure/Geocoding/PhotonAddressGeocoder.cs` (new), `src/Explore.Infrastructure/Geocoding/PhotonApiModels.cs` (new), `src/Explore.Infrastructure/Geocoding/PhotonOptionsValidator.cs` (new), `src/Explore.Infrastructure/Geocoding/DataProtectionAddressSelectionProtector.cs` (new), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/PhotonAddressGeocoderTests.cs` (new), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/DataProtectionAddressSelectionProtectorTests.cs` (new).
- **Description:** Use `HttpClientFactory` and the installed standard resilience pipeline with bounded timeout/retries, cancellation, `Retry-After`, stable user agent, explicit language/country/result parameters, tolerant JSON parsing, attribution, and Data Protection purpose/version/expiry isolation. Do not retry semantic 4xx responses or log URI query strings.
- **Acceptance Criteria:**
  - [ ] Contract tests cover success, malformed payload, timeout, cancellation, 429/5xx, bounded retries, and redaction.
  - [ ] Token protection round-trips and fails for tamper, expiry, purpose, and key/config mismatch.
  - [ ] Metrics/logs contain only provider/outcome/latency categories.
- **Dependencies:** 2.1, 3.1.
- **Effort:** L
- **Required Skills/Rules:** `error-tracking`, `auth-patterns`, infrastructure conventions.

#### Task 3.3: Add Geocoding Readiness And Safe Configuration

- **Type:** create / modify
- **Layer:** Infrastructure / API / Docs
- **Files:** `src/Explore.Infrastructure/Geocoding/GeocodingReadinessProbe.cs` (new), `src/Explore.API/HealthChecks/GeocodingReadinessHealthCheck.cs` (new), `src/Explore.API/Program.cs` (existing), `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md` (existing), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/GeocodingReadinessProbeTests.cs` (new).
- **Description:** Validate provider/base URI/production policy at startup and expose a bounded readiness category without executing address lookups or exposing endpoints/secrets.
- **Acceptance Criteria:**
  - [ ] Misconfiguration fails startup with actionable non-secret text.
  - [ ] Readiness distinguishes disabled/configured/unreachable/limited without address I/O.
  - [ ] API keys remain server-side secret references; Photon has no fake secret setting.
- **Dependencies:** 3.2.
- **Effort:** M
- **Required Skills/Rules:** `error-tracking`, `aspire`, `CONFIGURATION.md`, `SECURITY-MODEL.md`.

### Phase 4: Private Geocoding API Contract

- **Goal:** Expose the Application query through a secured, rate-limited, no-store API and regenerate all consumers.
- **Depends on:** Phase 3.
- **Relevant files:** New API controller/rate-limit policy, route names, OpenAPI/generated client/docs, API integration tests.
- **Related skills/rules:** API controllers, auth patterns, BFF patterns, OpenAPI intent.
- **Acceptance criteria:** Browser calls use a named authenticated POST through existing YARP; PII is absent from URLs/logs/caches; generated contracts are current.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove/disable the endpoint and retain manual location entry; provider infrastructure may remain disabled.

#### Task 4.1: Add Authenticated Autocomplete POST

- **Type:** create
- **Layer:** API
- **Files:** `src/Explore.API/Controllers/GeocodingController.cs` (new), `src/Explore.API/Hateoas/RouteNames.cs` (existing), `src/Explore.API/Extensions/RateLimitingExtensions.cs` (existing), `tests/Event.API.IntegrationTests/Features/GeocodingControllerTests.cs` (new).
- **Description:** Add a named `POST /api/geocoding/address-suggestions` operation with `[Authorize]`, authenticated classification, bounded request body, RFC 7807 errors, `PrivateNoStore`, cancellation, and a dedicated per-user/tenant/IP abuse policy. Do not accept provider names or credentials from the client.
- **Acceptance Criteria:**
  - [ ] Anonymous, cross-tenant, oversized, malformed, and rate-limited requests fail correctly.
  - [ ] Response and errors carry `Cache-Control: private, no-store`; no output cache/ETag applies.
  - [ ] Endpoint/access logging tests prove request body and query text are not emitted.
- **Dependencies:** Phase 3.
- **Effort:** M
- **Required Skills/Rules:** `api-controllers.md`, `auth-patterns`, `add-write-endpoint` intent.

#### Task 4.2: Publish Geocoding Through HAL Where Executable

- **Type:** modify
- **Layer:** API / Application
- **Files:** `src/Explore.API/Hateoas/Policies/LocationLinkPolicy.cs` (existing), `src/Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs` (existing), `src/Explore.Application/DTOs/Location/LocationDto.cs` (existing), `tests/Event.API.IntegrationTests/Features/Hateoas/LocationHateoasTests.cs` (existing).
- **Description:** Advertise the autocomplete relation from authorized location-management resources only when geocoding readiness and authorization permit it. Fail closed on missing/malformed capability metadata.
- **Acceptance Criteria:**
  - [ ] Authorized managers receive the relation; unauthorized/unready clients do not.
  - [ ] Blazor needs no local role/claim/provider inspection.
  - [ ] HAL targets the named route and preserves tenant context.
- **Dependencies:** 4.1.
- **Effort:** M
- **Required Skills/Rules:** `blazor-ui-conventions`, HAL invariant, API HATEOAS rules.

#### Task 4.3: Regenerate And Document The API Contract

- **Type:** modify
- **Layer:** API / Blazor / Docs
- **Files:** `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md` (existing).
- **Description:** Regenerate schema/inventory/client once, document POST-for-private-read rationale, rate limits, no-store semantics, suggestion/token shapes, and intentional removal of raw coordinate writes.
- **Acceptance Criteria:**
  - [ ] OpenAPI parity passes and operation IDs are stable.
  - [ ] Generated client has the new method and only the current location shapes.
  - [ ] Documentation contains no example address, coordinate, token, or secret that resembles real PII.
- **Dependencies:** 4.1, 4.2.
- **Effort:** M
- **Required Skills/Rules:** `openapi-contract-change` intent, documentation style guide.

### Phase 5: Accessible Location Editing Experience

- **Goal:** Replace raw coordinate fields with an accessible, resilient autocomplete that consumes HAL and preserves manual entry.
- **Depends on:** Phase 4.
- **Relevant files:** New Blazor component/CSS, location service/dialogs, lookup section HAL flow, generated client, Blazor tests/docs.
- **Related skills/rules:** Blazor UI conventions, CSS isolation, design system, accessibility.
- **Acceptance criteria:** Keyboard/screen-reader users can search, select, clear, recover, and manually enter an address; no provider detail leaks into page code.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Hide the autocomplete relation/component and leave the manual address form usable with no coordinates.

#### Task 5.1: Build The Address Autocomplete Component

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor` (new), `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor.cs` (new only if markup-backed code is not concise), `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor.css` (new), `tests/Explore.Blazor.Client.Tests/Components/Locations/AddressAutocompleteTests.cs` (new).
- **Description:** Implement the WAI-ARIA combobox/listbox interaction with explicit label/help/error/status text, minimum query length, debounce, cancellation of superseded calls, bounded results, keyboard navigation, focus management, live announcements, attribution, loading/empty/error states, and RTL/logical CSS.
- **Acceptance Criteria:**
  - [ ] Arrow/Home/End/Enter/Escape/Tab behavior and focus semantics are tested.
  - [ ] Only the latest request can update results; disposal cancels pending work.
  - [ ] Reduced-motion, mobile touch targets, localization, and manual fallback are preserved.
- **Dependencies:** 4.3.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `ACCESSIBILITY.md`.

#### Task 5.2: Integrate Create And Edit Dialogs

- **Type:** modify / delete
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor.cs`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor.cs`, `src/Explore.Blazor.Client/Services/LocationService.cs` (existing), `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs` (new), `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs` (existing).
- **Description:** Replace latitude/longitude inputs with autocomplete selection; atomically populate normalized fields/token, let manual edits clear the selection, preserve PATCH concurrency, and render provider failure as recoverable inline status.
- **Acceptance Criteria:**
  - [ ] Create and edit submit the protected selection only while it matches visible fields.
  - [ ] Manual edits remove stale token/coordinates without blocking save.
  - [ ] Provider unavailable/rate-limited states never discard typed input.
- **Dependencies:** 5.1.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, generated-client-only API access.

#### Task 5.3: Enforce HAL-Gated Location Affordances

- **Type:** modify
- **Layer:** Blazor / API
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` (existing), `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationsTests.cs` (existing), `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs` (existing).
- **Description:** Preserve HAL links through the client service and gate create/edit/delete/autocomplete actions only by link presence. Remove unconditional action rendering and any local claim/role/provider logic.
- **Acceptance Criteria:**
  - [ ] Missing relations remove the corresponding control.
  - [ ] Present relations invoke their advertised URL/method.
  - [ ] Tests cover authorized, unauthorized, and geocoder-unready resources.
- **Dependencies:** 4.2, 5.2.
- **Effort:** M
- **Required Skills/Rules:** HAL invariant, `blazor-component-affordance` intent.

### Phase 6: Approved PostgreSQL/PostGIS Foundation

- **Goal:** After explicit ADR activation, add a PostgreSQL-only governed spatial projection with real migrations, transactional privacy behavior, and readiness.
- **Depends on:** Phase 1 Task 1.1 approval; Event Location Privacy migration baseline `ELP-230C`; erasure/remediation dependencies `ELP-515`, `ELP-520`, `ELP-530` reconciled.
- **Relevant files:** Central packages/locks, provider composition, primary AppHost/Compose database, DbContext/spatial row/config/store, generated PostgreSQL migration, privacy erasure flow, docs/tests.
- **Related skills/rules:** EF Core, Clean Architecture, auth/privacy, Aspire, migration rules.
- **Acceptance criteria:** PostgreSQL has `geography(Point,4326)` plus GiST and tenant indexes; other providers remain area-only; approval is explicit; revocation/erasure is transactional.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Set discovery mode to `area_only`, stop writes, back up, and use the documented development reset or production restore path. Never emulate proximity elsewhere.

#### Task 6.1: Add PostgreSQL Spatial Capability And Image

- **Type:** modify
- **Layer:** Persistence / DevOps
- **Files:** `Directory.Packages.props`, `src/Explore.Persistence/Explore.Persistence.csproj`, `src/Explore.Persistence/packages.lock.json`, `tests/Event.Persistence.IntegrationTests/packages.lock.json`, `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`, `src/Explore.Persistence/ExploreDbContext.cs`, `src/Explore.AppHost/AppHost.cs`, `docker-compose.yml`, `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs`, `tests/Event.Architecture.Tests/AppHostTopologyArchitectureTests.cs`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md` (existing).
- **Description:** Pin `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` to the installed Npgsql version, call `UseNetTopologySuite()` only for the Application Npgsql context, switch only the primary PostgreSQL 18 image to a pinned PostGIS tag/digest, correct the PG18 volume target, and validate provider/mode combinations. Leave Keycloak/Cerbos/privacy-authority databases unchanged.
- **Acceptance Criteria:**
  - [ ] PostgreSQL configuration enables NTS; four other providers do not.
  - [ ] `postgis` mode on a non-PostgreSQL provider fails startup clearly.
  - [ ] Development reset and production backup/restore steps cover the volume-layout change.
  - [ ] No `latest` tag or unpinned spatial package enters the repository.
- **Dependencies:** 1.1 approval, ELP-230C migration integrity.
- **Effort:** L
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `aspire`, `efcore-migrations.md`.

#### Task 6.2: Generate The Governed Discovery Projection Migration

- **Type:** create / modify
- **Layer:** Persistence
- **Files:** `src/Explore.Persistence/Spatial/LocationDiscoveryPoint.cs` (new Persistence row), `src/Explore.Persistence/Spatial/LocationDiscoveryPointConfiguration.cs` (new), `src/Explore.Persistence/Spatial/LocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Contracts/Persistence/ILocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Contracts/Discovery/LocationDiscoveryPointModels.cs` (new), `src/Explore.Persistence/ExploreDbContext.DbSets.cs` (existing), `src/Explore.Persistence/ExploreDbContext.cs` (existing), `src/Explore.Persistence/Migrations/<generated>_AddLocationDiscoveryPoints.cs` (generated), `src/Explore.Persistence/Migrations/ExploreDbContextModelSnapshot.cs` (generated), `tests/Event.Persistence.IntegrationTests/Repositories/LocationDiscoveryPointStoreTests.cs` (new), `tests/Event.Persistence.IntegrationTests/Migrations/LocationDiscoveryPointMigrationTests.cs` (new).
- **Description:** Map a tenant/location-unique row with NTS `Point`, explicit `geography(Point,4326)`, active approval evidence/concurrency, FK, GiST point index, and relational tenant/location indexes. Apply/ignore the mapping by provider so non-PostgreSQL migrations remain unchanged. Generate the migration via `dotnet ef`; never edit generated files.
- **Acceptance Criteria:**
  - [ ] Migration enables PostGIS and creates the exact constrained column/indexes.
  - [ ] Domain/Application assemblies have no NTS/PostGIS dependency.
  - [ ] Generic/public DTOs contain no point/coordinates.
  - [ ] Real PostGIS tests inspect extension, column type, index method, tenant uniqueness, and provider isolation.
- **Dependencies:** 6.1.
- **Effort:** XL
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`, `efcore-persistence.md`, `efcore-migrations.md`.

#### Task 6.3: Implement Explicit Approval And Revocation

- **Type:** create
- **Layer:** Application / Persistence
- **Files:** `src/Explore.Application/Features/Locations/Requests/Commands/ApproveLocationDiscoveryPointCommand.cs` (new), `RevokeLocationDiscoveryPointCommand.cs` (new), matching handlers/validators under `Features/Locations/` (new), `src/Explore.Application/Contracts/Persistence/ILocationDiscoveryPointStore.cs` (new), `src/Explore.Persistence/Spatial/LocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Services/LocationPrivacyGovernanceMutationService.cs` (existing), `tests/Event.Application.UnitTests/Features/Locations/Commands/LocationDiscoveryPointCommandHandlerTests.cs` (new), `tests/Event.Persistence.IntegrationTests/Repositories/LocationDiscoveryPointStoreTests.cs` (new).
- **Description:** Require management authorization, tenant ownership, active non-erased PII, valid coordinate pair, explicit approval version/evidence, and readiness. Reapproval replaces the point only through a concurrency-safe command; coordinate changes revoke rather than silently republish.
- **Acceptance Criteria:**
  - [ ] No location gains a discovery point implicitly or through backfill defaults.
  - [ ] Cross-tenant, erased, private-policy-ineligible, stale-concurrency, and invalid-coordinate approvals fail closed.
  - [ ] Revocation makes the point immediately ineligible while retaining bounded audit evidence.
- **Dependencies:** 6.2, ELP-530 semantics.
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, location privacy contract.

#### Task 6.4: Integrate Erasure, Correction, And Readiness

- **Type:** modify / create
- **Layer:** Application / Persistence / Infrastructure / Docs
- **Files:** `src/Explore.Persistence/Repositories/UserLocationPrivacyErasureRepository.cs`, `src/Explore.Application/Services/LocationPrivacyGovernanceMutationService.cs`, `src/Explore.Application/Services/LocationPrivacyOutboxMessageFactory.cs`, `src/Explore.Persistence/Spatial/LocationDiscoveryPointStore.cs`, `src/Explore.API/HealthChecks/PostgisDiscoveryReadinessHealthCheck.cs` (new), `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs`, `tests/Event.Architecture.Tests/DiscoveryPostgisSeparationArchitectureTests.cs`, `dev/active/event-location-privacy/event-location-privacy-tasks.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md` (existing unless marked new).
- **Description:** Delete/deactivate the projection in the same transaction as PII erasure, invalidate it on correction/address changes, reuse existing idempotent outbox/remediation mechanisms, and add bounded readiness checks for image/extension/table/index/query/mode consistency.
- **Acceptance Criteria:**
  - [ ] Erasure cannot commit while an active discovery point survives.
  - [ ] Retry/replay is idempotent and does not recreate a point.
  - [ ] Readiness exposes categories only, never SQL, identifiers, addresses, origins, points, or connection strings.
  - [ ] Implementation evidence, not planning text, is used to reconcile `ELP-730` and Home Discovery Phase 6.
- **Dependencies:** 6.3, ELP-515/520/530.
- **Effort:** XL
- **Required Skills/Rules:** `error-tracking`, privacy/outbox patterns already owned by Event Location Privacy, migration rules.

### Phase 7: Exact Nearby Occurrence Discovery

- **Goal:** Add the ADR-approved PostGIS query and private API/Home integration without exposing points or introducing a fallback.
- **Depends on:** Phase 6.
- **Relevant files:** Application request/result/handler, Persistence spatial query, Public Experience controller/handler/contracts, generated client/docs, API tests.
- **Related skills/rules:** CQRS, EF query optimization, API/privacy/HAL.
- **Acceptance criteria:** Exact results are tenant-safe, occurrence-correct, stable, private/no-store, and index-backed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Configure `area_only`; UI removes exact wording/actions and uses the existing area flow. Never run a client/in-memory fallback.

#### Task 7.1: Implement The PostGIS Occurrence Query

- **Type:** create
- **Layer:** Application / Persistence
- **Files:** `src/Explore.Application/Contracts/Persistence/IPostgisNearbyOccurrenceQuery.cs` (new), `src/Explore.Application/DTOs/PublicExperience/NearbyEventDiscoveryDtos.cs` (new), `src/Explore.Application/Features/PublicExperience/Requests/Queries/GetNearbyEventDiscoveryRequest.cs` (new), `src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetNearbyEventDiscoveryRequestHandler.cs` (new), `src/Explore.Persistence/Spatial/PostgisNearbyOccurrenceQuery.cs` (new), `tests/Event.Persistence.IntegrationTests/Repositories/PostgisNearbyOccurrenceQueryTests.cs` (new).
- **Description:** Validate rounded origin/radius/filters, apply tenant/public/published/future/in-person/active-point predicates, `ST_DWithin`, minimum `ST_Distance` per event, and stable distance/start/event ordering before pagination. Keep all spatial evaluation server-side.
- **Acceptance Criteria:**
  - [ ] Tests cover inside/on/outside radius, ties, multi-location events, past/online/draft/private/deleted exclusions, tenant isolation, and cancellation.
  - [ ] Result exposes only rounded distance and safe occurrence/location metadata.
  - [ ] Representative `EXPLAIN (ANALYZE, BUFFERS)` evidence uses GiST without loading all points/client evaluation.
- **Dependencies:** Phase 6.
- **Effort:** XL
- **Required Skills/Rules:** `optimizing-ef-core-queries`, `cqrs-mediatr-guidelines`, ADR-013.

#### Task 7.2: Add Private Nearby And Approval API Operations

- **Type:** create / modify
- **Layer:** API
- **Files:** `src/Explore.API/Controllers/PublicExperienceController.cs` (existing nearby route owner), `src/Explore.API/Controllers/LocationDiscoveryController.cs` (new approval route owner), `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Hateoas/Policies/LocationLinkPolicy.cs`, `src/Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs` (existing), `tests/Event.API.IntegrationTests/Features/PublicExperienceNearbyControllerTests.cs` (new), `tests/Event.API.IntegrationTests/Features/LocationDiscoveryControllerTests.cs` (new), generated OpenAPI/client artifacts (existing/generated).
- **Description:** Add named authenticated POST nearby and approve/revoke operations with tenant context, server authorization, RFC 7807, bounded bodies, private/no-store, no output cache/ETag, and HAL gating. The nearby origin is never present in routes, logs, errors, metrics, or durable settings.
- **Acceptance Criteria:**
  - [ ] Unsupported/unready modes fail honestly and advertise no HAL action.
  - [ ] Nearby request/response privacy headers and logging boundaries are tested.
  - [ ] Approve/revoke operations require executable location-management authorization and concurrency.
- **Dependencies:** 6.3, 7.1.
- **Effort:** L
- **Required Skills/Rules:** `auth-patterns`, `api-controllers.md`, HAL invariant.

#### Task 7.3: Integrate Home Discovery And Canonical Contracts

- **Type:** modify
- **Layer:** Application / API / Blazor / Docs
- **Files:** `src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetHomeDiscoveryQueryHandler.cs`, `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs`, `src/Explore.Blazor.Client/Services/HomeDiscoveryService.cs`, `schemas/openapi_islamu-event.json`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`, `tests/Event.API.IntegrationTests/Features/PublicExperienceHomeDiscoveryControllerTests.cs`, `tests/Explore.Blazor.Client.Tests/Services/HomeDiscoveryServiceTests.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/BLAZOR.md`, `docs/SELF_HOSTING.md` (existing).
- **Description:** Activate dormant distance/nearest-occurrence fields only in PostGIS mode, keep area responses cacheable and coordinate-free, keep nearby responses private/no-store, and switch wording/actions based on HAL/capability rather than local configuration guesses.
- **Acceptance Criteria:**
  - [ ] Area-only behavior and cache semantics are unchanged.
  - [ ] Exact mode shows rounded distance/nearest occurrence only after explicit user action.
  - [ ] Origin is never stored in preferences; only area ID/mode/bounded radius may persist if approved.
  - [ ] Generated contracts and overlapping workstream evidence are reconciled.
- **Dependencies:** 7.2.
- **Effort:** L
- **Required Skills/Rules:** `blazor-bff-patterns`, `blazor-ui-conventions`, Home Discovery contract.

### Phase 8: One Accessible Map Experience (Decision-Gated)

- **Goal:** Add one maintainable map only after approving a production basemap source and confirming MapLibre integration compatibility.
- **Depends on:** Phase 5 for admin preview; Phase 7 for nearby discovery; explicit tile-source/product decision.
- **Relevant files:** Central package/lock files if a wrapper is selected, new isolated Blazor map component/CSS/optional JS module, safe map config contract, location/Home components, Blazor tests/docs.
- **Related skills/rules:** Blazor UI/CSS/design system/accessibility, agentic research.
- **Acceptance criteria:** The map is supplementary, keyboard/screen-reader-safe, RTL/responsive, and never exposes exact public event coordinates.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove/hide the map and keep the complete textual address/discovery list. Mapping is never required to create a location or browse nearby results.

#### Task 8.1: Select The Production Map Integration And Tile Source

- **Type:** investigate / modify
- **Layer:** Blazor / DevOps / Docs
- **Files:** `Directory.Packages.props`, `src/Explore.Blazor.Client/Explore.Blazor.Client.csproj`, `src/Explore.Blazor.Client/packages.lock.json`, `src/Explore.Application/Models/PublicExperience/PublicExperienceHomeBlocksConfig.cs`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/adr/ADR-013-postgis-proximity-discovery.md` (existing; create a separate ADR only if the accepted map-data decision is not covered).
- **Description:** Prove current .NET/Blazor compatibility, SSR/prerender behavior, disposal, CSP, accessibility hooks, RTL, bundle size, and maintenance health for the report's MapLibre wrapper. Select a licensed contracted/self-hosted basemap/style URL and attribution. If the wrapper fails, stop the phase; do not add Leaflet plus a custom MapLibre abstraction in the same slice.
- **Acceptance Criteria:**
  - [ ] One integration and one production tile source are approved and pinned.
  - [ ] Public OSM/demo tiles and internal-only URLs are rejected as production defaults.
  - [ ] Only safe style URL/provider ID/attribution reach the browser; keys follow provider-specific public-token restrictions.
- **Dependencies:** Explicit product/operations decision.
- **Effort:** M
- **Required Skills/Rules:** `agentic-research`, `blazor-ui-conventions`, design system/accessibility docs.

#### Task 8.2: Implement The Supplementary Map Component

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor` (new), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.cs` (new only if needed), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.css` (new), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.js` (new only if needed), `tests/Explore.Blazor.Client.Tests/Components/Maps/EventDiscoveryMapTests.cs` (new).
- **Description:** Render an authorized admin preview for the current selected location and a public nearby context using only the user's local origin/coarse public context; do not render exact event pins. Provide a complete adjacent list, semantic heading/instructions, focus-safe controls, keyboard zoom/pan alternatives, reduced motion, responsive sizing, and deterministic disposal.
- **Acceptance Criteria:**
  - [ ] Every map fact/action has a non-map textual equivalent.
  - [ ] No point, address, token, provider key, or internal tile URL appears in public HTML/telemetry.
  - [ ] Component survives prerender, navigation, JS failure, and provider outage with the list/form intact.
- **Dependencies:** 8.1.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, CSS isolation, `ACCESSIBILITY.md`.

#### Task 8.3: Integrate Map Preview And Nearby Context

- **Type:** modify
- **Layer:** Blazor / Docs
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor`, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor`, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor.css`, `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs`, `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs` (new), `tests/Explore.Blazor.Client.Tests/Components/Discovery/HomeDiscoveryExperienceTests.cs`, `docs/BLAZOR.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md` (existing unless marked new).
- **Description:** Show the map only when its HAL/config capability exists. Admin preview may display the editor-authorized selection; public Home remains list-first and uses only coarse/user-local context. Ensure map failure does not affect save/discovery behavior.
- **Acceptance Criteria:**
  - [ ] UI is HAL/config-gated and has stable loading/empty/error/fallback states.
  - [ ] Location save and nearby list work with maps disabled.
  - [ ] Responsive, RTL, keyboard, focus, announcement, and reduced-motion tests pass.
- **Dependencies:** 8.2, 5.2, 7.3.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, `design-system`, HAL invariant.

## 7. Testing Strategy

Each phase owns one Release build and one selected non-browser project test, run once after all phase tasks. Phase 1 and Phase 2 intentionally repeat `Event.Application.UnitTests`: Phase 1 protects breaking location aggregate/command changes; Phase 2 protects the new geocoding orchestration/token boundary. Phase 4 and Phase 7 intentionally repeat `Event.API.IntegrationTests`: they cover distinct private contracts, first autocomplete and later PostGIS nearby/approval.

Tests added during a phase belong to that phase's selected project wherever practical. Intent-mandated architecture/persistence/client coverage that cannot fit the selected project is placed in the later phase whose selected project owns that surface. Do not schedule solution-level `dotnet test`, browser automation, live Aspire/Docker startup, or a separate verification phase.

## 8. Documentation, Configuration, And Operations Impact

- **Architecture/domain/privacy:** ADR-013, `ARCHITECTURE.md`, `DOMAIN.md`, `SECURITY-MODEL.md`, Event Location Privacy/Home Discovery evidence.
- **API/contracts:** `API.md`, `API_CHANGELOG.md`, `API_CONTRACT_INVENTORY.md`, `schemas/openapi_islamu-event.json`, generated Blazor client.
- **Provider/config/secrets:** `CONFIGURATION.md`, `SECRETS.md`, `.env.example`; provider selection, endpoint, culture/country bias, timeouts, result limit, token lifetime, and production validation. No raw connection strings.
- **Deployment:** `SELF_HOSTING.md`, `OPERATIONS.md`, AppHost/Compose profiles, pinned images, health/readiness, Photon data lifecycle, PostgreSQL volume migration, PostGIS backup/restore/rollback.
- **UI:** `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`; combobox behavior, map fallback, RTL, CSS isolation, HAL gating.
- **Testing:** `TESTING.md` plus the exact commands in each phase.
- **Package locks:** regenerate every affected committed `packages.lock.json` after package changes.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Address queries, normalized results, exact coordinates, protected tokens, and user origins are PII. They use authenticated POST bodies and `private, no-store` responses and are excluded from logs/traces/metrics/cache keys/errors.
- Provider credentials and Photon internal URLs remain server-side. Browser-safe map tokens, if any, require origin/domain restrictions and separate secret classification.
- Tenant comes only from `ITenantContext`; all provider/spatial operations re-check tenant ownership and fail closed.
- Location/discovery writes use server-side authorization and concurrency. Blazor uses HAL links only.
- Autocomplete has minimum/maximum length, bounded results, debounce/cancellation, per-user/tenant/IP rate limits, upstream timeouts, and circuit/outcome metrics. It must not become an open proxy.
- Data Protection tokens are purpose/version/provider/config-bound and short-lived. They are not persisted or accepted across unrelated operations.
- Discovery approval is explicit. Coordinate change, revocation, or privacy erasure invalidates it transactionally.
- Nearby origin is rounded client-side, used once, and never stored. No analytics, screenshots, support dump, or error payload may capture it.
- Martin remains deferred because application-schema auto-publication bypasses these controls.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Requirement |
|---|---|---|
| Multi-tenancy | Applicable | Tenant context owns every request; spatial rows and queries have tenant keys/filters; cross-tenant tests are mandatory. |
| Federation | Applicable | Geocoding provenance, PII, protected tokens, origins, and discovery points never enter AT Protocol/publication snapshots. Only already-approved safe event metadata may federate. |
| Localization | Applicable | Provider requests use validated culture/country bias; displayed provider labels are not treated as localized canonical values; UI/error strings use localization infrastructure. |
| RTL | Applicable | Combobox and map use logical CSS, correct key behavior, and no hard-coded physical layout assumptions. |
| Accessibility | Applicable | WAI-ARIA combobox/listbox, live status, keyboard operation, focus restoration, reduced motion, and complete non-map equivalents are acceptance requirements. |
| White-label/product | Applicable | Provider/attribution remains compliant but does not leak operator configuration. Product wording distinguishes area-only from exact nearby mode. |
| SEO | Not applicable to private autocomplete; applicable to public Home | Exact nearby POST results are not indexable/cacheable; base Home SEO remains area-safe. |

## 11. Observability And Operations

- Metrics: request count, outcome, latency histogram, cancellation, rate limit, retry, provider readiness, spatial readiness, and bounded query latency/cardinality. No high-cardinality tenant/location/session labels.
- Logs: structured event IDs and provider/outcome category only; redact request URI queries, bodies, upstream payloads, tokens, addresses, origins, coordinates, and connection strings.
- Traces: suppress/sanitize sensitive HTTP/database parameters and never attach address/origin tags.
- Health: geocoding configured/reachable categories; PostGIS image/extension/table/index/bounded-query/mode consistency. Health payloads remain non-sensitive.
- Recovery: manual address entry for geocoder outages; `area_only` for PostGIS outages; documented Photon rebuild/swap and PostgreSQL backup/restore. No silent semantic fallback.

## 12. Migration And Compatibility Plan

1. Phases 1–5 are schema-free. They intentionally remove `TenantId`, raw latitude, and raw longitude from location write contracts, regenerate clients, and update all in-repo callers in one slice.
2. Before Phase 6, reconcile Event Location Privacy's current migration baseline (`ELP-230C`) so spatial generation starts from a clean model snapshot.
3. After explicit approval, change only the primary PostgreSQL application image to pinned PostGIS 18, correct volume mounts, regenerate locks, and document development reset versus production backup/restore.
4. Generate the PostgreSQL migration from model/configuration changes. Do not edit migrations or snapshots. Other provider migration projects must show no unintended spatial diff.
5. Deploy image/readiness first, then migration, then projection approval capability, then nearby endpoint/UI. No default backfill occurs.
6. Rollback uses `Discovery:Mode=area_only` and stops approval/query traffic. Schema removal is not the first incident response; restore from the documented backup if migration rollback is necessary.
7. No compatibility aliases, dual write shapes, legacy coordinate inputs, or alternate proximity engines are added.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| PostGIS requirement conflicts with optional/multi-provider contract. | High | High | Explicit Task 1.1 decision; recommended PostgreSQL-only mandatory extension; fail non-PG `postgis` mode. | Startup validation/provider migration diff. | 1.1, 6.1 |
| Spatial migration corrupts/reset volume due PG18 mount change. | Medium | Critical | Pin image, backup/restore rehearsal, distinct dev reset path, readiness before migration. | Failed preflight/volume version check. | 6.1, 6.4 |
| Address/coordinate PII leaks through logs, URLs, caches, tokens, or telemetry. | Medium | Critical | POST/no-store, redaction tests, protected tokens, bounded labels, log inspection. | Security tests/telemetry scan. | 2.2, 3.2, 4.1, 7.2 |
| Photon endpoint is under-sized or public-demo dependent. | High | High | Benchmark, heavy-profile opt-in, production validation, explicit outage fallback to manual entry. | Readiness/latency/error rate. | 3.1–3.3 |
| Stale coordinates survive a manual address edit. | Medium | High | Aggregate methods clear pair and revoke discovery; regression tests. | Domain/Application test failure. | 1.2, 6.4 |
| Exact discovery exposes private or cross-tenant venues. | Medium | Critical | Explicit approval, tenant keys/filters, privacy erasure transaction, no point response. | Persistence/API isolation tests. | 6.2–7.2 |
| Query scans or produces unstable pagination. | Medium | High | GiST/relational indexes, database predicates, stable ordering, EXPLAIN evidence. | Query plan/latency/cardinality metrics. | 7.1 |
| Map wrapper or basemap is immature/non-compliant. | High | Medium | Decision gate, one integration, list-first fallback, no public OSM/demo default. | Compatibility/bundle/provider review. | 8.1 |
| Google terms are implemented from an oversimplified matrix. | High if activated | Critical | Keep deferred; require legal/EEA/storage/attribution/session review. | Approval absent or terms change. | Deferred |
| Martin auto-publishes exact/application data. | High if adopted naively | Critical | Defer; explicit allowlist/read-only/coarse source/auto_publish false/new approval. | Config architecture test/source inventory. | Deferred |

## 14. Success Metrics And Definition Of Done

- Authorized editors can search and select an address with keyboard/screen reader, save a normalized `LocationPii` bundle, and continue with manual entry during provider failure.
- No location write accepts body tenant ID or raw coordinate fields; no stale/partial coordinate pair can exist.
- Provider credentials, addresses, tokens, origins, and exact coordinates are absent from URLs, shared caches, logs, traces, metrics, public DTOs, federation, and public tile sources.
- Photon is production-validated, bounded, resilient, and observable; its public demo is not a fallback.
- If ADR-013 is activated, PostgreSQL/PostGIS readiness, migration, geography/indexes, approval/revocation/erasure, occurrence query, and stable ordering are proven against real PostGIS.
- Non-PostgreSQL deployments remain honest area-only/disabled and never emulate exact proximity.
- Blazor affordances are HAL-gated; map failure never blocks forms or the discovery list.
- Every phase has exactly one passing Release build and its selected project test, and generated/docs/task/context artifacts are current.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Future implementation agents must:

1. At first start, read this plan, context, and tasks once; on cold resume read context/tasks first and only the current plan sections/changed decisions.
2. Start from the highest-priority unchecked task unless the user overrides it.
3. Treat `tasks.md` as the hot ledger; mark substantial work in progress and check completed implementation promptly, no later than phase end.
4. Keep task completion separate from phase verification; a phase completes only after its one Release build and selected test pass.
5. Update status summary, count, priority, next slice, discovered/deferred work, and date whenever state changes.
6. Update context after a phase, decision, blocker, failed validation, material discovery, pause/compaction/transfer; update this plan only for strategy/scope/acceptance/risk changes.
7. Record failed validation with cause and recovery; never mark the phase complete.
8. Reconcile affected tasks and add a dated context handoff before pause, PR, or transfer. Name unrelated dirty files and do not modify them.
9. Run phase verification only after all phase tasks, once per unchanged input; never start live services/browser for the automated gate.
10. Never hand-edit migrations/snapshots/generated clients, weaken privacy tests, add compatibility shims, expose exact points, or implement deferred providers without their trigger/approval.
11. Revalidate new package/framework details through Context7 if it becomes available; otherwise use primary official documentation and record the substitution.
12. Every implementation summary teaches what changed, responsibilities/files, data/control flow, patterns/libraries/infrastructure, privacy/reliability choices, exact verification, remaining work, and dev-doc status.

## 16. Progress Reporting Contract

After each implementation slice, report:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason; tasks reconciled yes/no; context/plan updated or unchanged with trigger reason
```

## 17. Potential Risks & Unknowns

The most consequential unresolved decision is whether every PostgreSQL deployment may require the PostGIS extension. The recommended development-mode answer is yes for PostgreSQL only, because it keeps one DbContext/migration/transaction boundary; rejecting it materially changes Phase 6 and requires a separate spatial-store design. The second risk is operational rather than code: self-hosted Photon and a production map tile source have real capacity, data-update, licensing, and availability obligations that the report understates. Do not begin those dependencies from demo endpoints.

### Deferred Provider And Infrastructure Work

| Deferred item | Why it is not in the initial implementation | Trigger to add a scoped phase |
|---|---|---|
| Google Places + Google Maps | EEA terms, caching/storage, attribution, billing/session termination, field masks, and map-display obligations require legal/product approval. | Approved legal memo, budget/quotas, exact data retention contract, and product requirement for premium coverage. |
| Pelias | Large deployment/data operations and no measured Photon coverage failure. | Regional benchmark proves Photon insufficient and operations accepts Pelias capacity/update burden. |
| Native GeoNames/SQLite FTS5 | Separate importer, licensing/attribution, dataset refresh, search-quality, and storage workstream. | Documented air-gapped/minimal deployment requirement with accepted coverage limits. |
| LeafletForBlazor | No second renderer need and current package maturity is volatile. | Measured WebGL/MapLibre incompatibility on supported clients. |
| Martin | No approved safe tile dataset; exact discovery points cannot be public tiles. | Approved coarse/aggregate tile product contract and tenant/cache policy; then explicit allowlist, read-only role, `auto_publish: false`, pinned image, same-origin route. |
| Generic map-provider/component hierarchy | One map implementation does not justify it. | A second approved renderer produces concrete shared behavior worth extracting. |
