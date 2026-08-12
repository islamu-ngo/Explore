<!-- ABOUTME: Repository-grounded implementation plan for private address geocoding and governed spatial event discovery. -->
<!-- ABOUTME: Defines decision gates, clean architecture boundaries, phased delivery, verification, and deferred provider choices. -->

# Address Geocoding And Spatial Discovery — Implementation Plan

Last Updated: 2026-08-12 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Read `dev/report/address_geocoding_analysis.md` and produce an enterprise-grade implementation plan using repository conventions, Clean Architecture, current external research, and no backward-compatibility burden.
- **Task directory:** `dev/active/address-geocoding-and-spatial-discovery/`
- **Planning status:** Senior CTO review disposition: **Approve with Required Changes**. RC-1 through RC-5 plus the 2026-08-12 provider-licensing, provider-optionality, and governed-local-address feedback are incorporated; runtime implementation and ADR-013 activation have not started.
- **Matched intents:** No single intent covers this cross-cutting capability. Use the composite contract for `add-cqrs-handler`, `add-write-endpoint`, `add-get-endpoint`, `add-ef-migration`, `update-repository-query`, `openapi-contract-change`, `blazor-component-affordance`, and `external-infrastructure-bootstrap`.
- **Relevant skills:** `implementation-plan`, `agentic-research`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `error-tracking`, and `aspire`.
- **Relevant rules:** `.claude/rules/domain.md`, `application-layer.md`, `api-controllers.md`, `efcore-persistence.md`, `efcore-migrations.md`, `blazor-server.md`, `blazor-client.md`, and `tests.md`.
- **Primary layers:** Domain, Application, Infrastructure, Persistence, API, generated OpenAPI client, Blazor Client, AppHost/Compose, tests, and canonical documentation.
- **Complexity:** **XL**. The full report crosses a PII-bearing aggregate, five-tier address-creation governance, scoped local autocomplete, third-party license boundaries, authenticated API contracts, generated clients, five database providers, optional PostGIS, public discovery semantics, browser accessibility, and deployment infrastructure. The plan deliberately ships one opt-in provider and one opt-in map integration before adding alternatives.
- **Baseline:** `dotnet build --configuration Release --verbosity quiet` passed again on 2026-08-12 with 37 projects, 0 errors, and 0 warnings before this re-baseline.
- **Research:** Tavily direct extraction verified primary vendor documentation. Context7 was requested but no Context7 connector, tool, resource, or template is available in this session; no Context7 result is claimed. This limitation is an explicit implementation-time revalidation item.

### 0.1 Composite Contribution Contract

| Concern | Planning decision |
|---|---|
| Must-read sources | `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `GOVERNANCE.md`, `ARCHITECTURE.md`, `DOMAIN.md`, `API.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `CONFIGURATION.md`, `OPERATIONS.md`, `TESTING.md`, `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`, ADR-013, the source report, matched rules, and the workstream context/tasks. |
| Paths in scope | Existing location aggregate/commands/contracts, new Application geocoding contracts, Infrastructure provider adapter, API endpoints, generated artifacts, Blazor location dialogs/components, an opt-in PostGIS context/migration chain isolated from the primary provider-neutral model, Home Discovery query/API/UI, AppHost/Compose, tests, and named docs. |
| Minimum verification | One Release build and at most one relevant project test per implementation phase, as listed in Section 6. Generated migrations and clients are regenerated, never hand-edited. |
| Documentation obligations | Update ADR-013 and canonical architecture/domain/API/security/configuration/self-hosting/operations/Blazor/testing documents in the phase that changes the corresponding behavior. |
| Forbidden without approval | Activating ADR-013, publishing exact venue geometry, adding PostGIS/NTS objects to the primary migration chain, treating PostgreSQL as proof of PostGIS capability, using public Photon or public OSM tiles as a production dependency, storing browser origins, sending raw address queries to logs/URLs, or adopting Google terms/billing. |
| Compatibility posture | Development mode: remove obsolete request fields and generated client shapes directly. Do not add aliases, dual contracts, or shims. |

### 0.2 Senior CTO Review Disposition

| Finding | Disposition in this re-baseline |
|---|---|
| RC-1 — cross-context transaction mechanism | **Resolved in design.** Existing `EfCoreUnitOfWork` remains the sole owner of the retryable transaction. The optional spatial context is created on the same open `DbConnection` and enlisted with `UseTransactionAsync` against the current `DbTransaction`; it never commits, rolls back, or owns that connection. |
| RC-2 — ADR-013 acceptance ownership | **Added as Task 7.1.** After explicit activation approval, the Phase 7 implementation owner changes ADR-013 from `Proposed` to `Accepted` and records the actual date plus named decider/role before spatial code begins. |
| RC-3 — tile-source decision framework | **Added to the Phase 9 gate.** License, attribution, self-hosting, privacy, performance, cost, and operational criteria now govern comparison of PMTiles/Protomaps, MapTiler, Stadia Maps, or another evidenced candidate. |
| RC-4 — Photon resilience defaults | **Specified.** Total timeout defaults to 5 seconds, maximum retries to 2, and retry delays to 200 ms then 500 ms, bounded by the total budget and cancellation. |
| RC-5 — GeoNames attribution | **Recorded.** A future native provider must treat the GeoNames CC BY 4.0 attribution as an implementation and UI acceptance requirement. |

### 0.3 Provider Licensing And Local-Address Governance Disposition

| Feedback | Disposition in this re-baseline |
|---|---|
| Google Places map pairing | **Specified as a future-adapter invariant.** `GooglePlaces + GoogleMaps` and `GooglePlaces + None` are allowed; `GooglePlaces` with any non-Google map provider is invalid and fails closed. The no-map case still renders required Google branding/attribution beside Places content. |
| Providers are optional | **Specified.** `Geocoding:Provider=None` and `Maps:Provider=None` are healthy defaults. Address storage, existing approved-location reuse, event creation without a physical venue, and area-only discovery do not require a geocoder or map. |
| Manual/local address governance | **Added.** The existing lockable five-tier settings engine supplies the instance ceiling, tenant mode, and organization grant. Server authorization supplies the actor decision; HAL supplies the UI affordance. |
| Local autocomplete pollution | **Added.** Provider results remain transient provider suggestions. Persisted locations are filtered to tenant-approved, current-organization, or creator-private visibility before exact PII is read. |
| Licensing isolation | **Added.** Local manual addresses remain in application-owned tables and are never submitted to, merged into, or exported as an upstream provider dataset. Source provenance and approval visibility are separate state. |

## 1. Executive Summary

Deliver address acquisition as an authenticated, private, server-mediated extension of the existing `Location`/`LocationPii` workflow. A configured provider is optional: `Geocoding:Provider=None` is healthy and still permits scoped reuse of approved/local locations. When a provider is enabled, a normalized selection is returned with a short-lived, tamper-evident token; location commands consume that token and update the address bundle and coordinate pair atomically. Manual address creation is available only when the effective instance/tenant/organization policy and actor authorization allow it, and a manual address change clears stale coordinates. Tenant identity always comes from `ITenantContext`, never the request body.

After separate approval of ADR-013, add exact proximity only when the primary provider is PostgreSQL **and** the operator explicitly declares `Database__Capabilities__Postgis=true`. The flag defaults to `false`; PostgreSQL alone never implies the extension exists or is activated. A dedicated PostGIS context and migrations history share the configured primary PostgreSQL database but remain outside `ExploreDbContext` and its canonical migration chain. Exact PII coordinates remain in `LocationPii`; a separately approved discovery projection is created only by an authorized action. Nearby discovery remains occurrence-based, performs `ST_DWithin`/`ST_Distance` in the PostGIS adapter, returns distance and safe occurrence metadata but no point, and keeps the user's origin transient in a private `POST` request.

The first opt-in production provider is a self-hosted or operator-contracted Photon endpoint. No provider is the default, and provider failure never removes policy-authorized local entry/reuse. The API uses the existing BFF/YARP path, so no bespoke BFF endpoint or new geocoding project is added. Maps also default to none; one accessible MapLibre-based component may be added only after an operator-approved basemap source and wrapper compatibility gate. Google Places/Google Maps, Pelias, the native GeoNames/SQLite dataset, Leaflet, and Martin are deferred until their distinct legal, operational, or product trigger exists. The deferred Google activation contract already fixes its allowed pairing matrix and fail-closed UI/API behavior.

### Explicit Non-Goals

- No `Point` property or spatial index on `LocationPii`.
- No PostGIS extension, spatial table, NTS mapping, or spatial migration in the primary provider-neutral `ExploreDbContext` migration chain.
- No generic spatial-provider abstraction or non-PostGIS exact-distance fallback.
- No four-provider implementation, three map libraries, or `IMapComponent` hierarchy in the first delivery.
- No direct production dependency on `photon.komoot.io`, `tile.openstreetmap.org`, MapLibre demo tiles, or an unpinned container tag.
- No requirement to configure an address provider, map provider, PostgreSQL, or PostGIS to run the platform.
- No unconditional manual-address bypass around instance/tenant/organization policy or actor authorization.
- No upload, feedback, synchronization, or dataset merge that sends application-owned custom addresses to Photon, OSM, Google, GeoNames, or another provider.
- No exact coordinates in generic public location, Home Discovery, federation, AI, tile, log, trace, metric, cache-key, or analytics contracts.
- No separate `Explore.Geocoding` or BFF endpoint project while existing Application/Infrastructure/API/YARP ownership is sufficient.
- No automatic publication/backfill from `LocationPii` to discovery.
- No loss of geocoding, address editing, maps, or area-only discovery when PostGIS is absent, disabled, unsupported by the selected database, or not activated by a managed PostgreSQL service.

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
| PostGIS must be fully optional, including on PostgreSQL. | User architecture decision on 2026-08-12. | High | Use `Database__Capabilities__Postgis=false` by default; only `PostgreSql` plus explicit `true` may register/migrate/use the adapter. |
| The repository already isolates an optional co-located concern behind a dedicated DbContext and migrations history. | `CoLocatedPrivacyErasureAuthorityDbContext`, its design-time factory, `PrimaryDatabaseMigrationTarget`, and `ExploreDatabaseMigrator`. | High | Reuse the pattern for a separate PostGIS context; do not conditionally mutate the primary EF model. |
| EF Core cross-context transactions require both contexts to share one `DbConnection` and `DbTransaction`. | [EF Core transactions — cross-context transaction](https://learn.microsoft.com/en-us/ef/core/saving/transactions), `EfCoreUnitOfWork`, `RetainedAuthorityPrivacyErasureWorkflow`, and `PrivacyErasureApplier`. | High | Enlist a short-lived spatial context with `UseTransactionAsync`; retain the primary UoW as sole commit/rollback/retry owner. |
| PostgreSQL 18 is current, not PostgreSQL 17. | `AppHost.cs`, `docker-compose.yml`. | High | Plain PostgreSQL stays the default; only an explicitly opted-in primary application database may use a PostGIS image. |
| Npgsql spatial support requires its NetTopologySuite plugin and explicit geography mapping. | [Npgsql spatial mapping](https://www.npgsql.org/efcore/mapping/nts.html). | High | Pin the plugin to the installed Npgsql provider version and call `UseNetTopologySuite()` only while configuring the optional `PostgisDiscoveryDbContext`, never `ExploreDbContext`. |
| Google Autocomplete sessions terminate with Place Details or Address Validation; incomplete sessions have different billing. | [Google session pricing](https://developers.google.com/maps/documentation/places/web-service/session-pricing). | High | A Google adapter needs explicit session lifecycle and field-mask design. |
| Google Places content shown on a map must be shown on Google Maps; Places content may be shown without a map when required Google branding/attribution is adjacent. | [Google Places policies](https://developers.google.com/maps/documentation/places/web-service/policies). | High | Future validation allows `GooglePlaces + GoogleMaps` or `GooglePlaces + None` and rejects every non-Google map pairing. |
| Google Places content is generally subject to storage/caching restrictions, while Place IDs have an explicit storage exception and should be refreshed when stale. | [Google Places policies](https://developers.google.com/maps/documentation/places/web-service/policies), [Place IDs](https://developers.google.com/maps/documentation/places/web-service/place-id). | High | Do not encode the report's blanket 30-day claim. A future Google adapter needs an approved field-by-field persistence/retention contract. |
| Martin connection-only configuration can publish every readable spatial table and every non-geometry column as feature tags. | [Martin table sources](https://maplibre.org/martin/sources-pg-tables), [configuration](https://maplibre.org/martin/config-file). | High | Never point auto-discovery at the application schema or `LocationPii`. |
| Public OSM raster tiles are not a production SLA and require attribution, identification, and caching. | [OSMF tile policy](https://operations.osmfoundation.org/policies/tiles/). | High | Select a contracted/self-hosted tile source before production map rollout. |
| PMTiles separates an open archive/runtime format from the license and attribution of the map data placed inside it. | [PMTiles repository](https://github.com/protomaps/PMTiles), [Protomaps security/privacy guidance](https://docs.protomaps.com/guide/security-privacy). | High | Treat PMTiles as a self-hosting candidate, not as automatic permission to redistribute an arbitrary tileset. |
| OSM data is ODbL 1.0: public use requires attribution, and distributed/publicly used derived databases carry share-alike; independent non-OSM data in a collective database remains separately licensed. | [OSM copyright/license](https://www.openstreetmap.org/copyright), [OSMF collective-database guideline](https://osmfoundation.org/wiki/Licence/Community_Guidelines/Collective_Database_Guideline_Guideline). | High | Keep local event/location tables independent from OSM extracts and never merge custom addresses into a redistributed OSM-derived database. |
| GeoNames gazetteer dumps are licensed under CC BY 4.0. | [GeoNames dump readme](https://download.geonames.org/export/dump/readme.txt), [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). | High | Commercial use/adaptation is allowed; attribution, license link, and change indication must accompany public/redistributed use. This is data licensing, not application-source copyleft. |
| The repository already has a lockable Instance → Tenant → Organization → Group → User setting cascade and `Location.CreatedBy`. | `IHierarchicalSettingsResolver`, `SettingContext`, `SettingScope`, `Location`. | High | Reuse the existing engine and audit creator instead of adding a parallel policy subsystem or duplicate creator property. |
| Context7 cannot be queried in this session. | Tool/resource inventory inspected on 2026-08-11. | High | Revalidate framework/package details through Context7 if it becomes available before dependency adoption. |

### 2.2 Existing Implementation

#### Domain And Application

- `Location` is the tenant aggregate and owns privacy state, owner consent, concurrency, and irreversible PII erasure.
- `LocationPii` owns the exact address and nullable scalar coordinate pair. It has no provider provenance or spatial type.
- `Location.CreatedBy` already records the creating actor, but `Location` has no address-source, organization ownership, approval visibility, or scoped-autocomplete state.
- The shared settings engine already resolves lockable configuration through Instance → Tenant → Organization → Group → User. Security-sensitive address creation must stop at the intended scope and combine settings with server authorization; it must not let a user grant themselves permission.
- Create validates, AutoMaps, replaces `TenantId` from `ITenantContext`, then persists. Update loads the tracked aggregate with PII, checks `If-Match`, and applies independent PATCH groups.
- Existing coordinate validators enforce numeric ranges but do not guarantee a finite both-or-none pair or clear coordinates when address text changes.
- `EventLocation` disclosure services already centralize purpose-based exact disclosure, audit, and authorization. Geocoding and discovery must not create a parallel disclosure path.

#### Persistence And Database Composition

- `LocationRepository` returns entities, auto-includes PII for authorized management reads, uses no-tracking for lists, and physically deletes PII through `ForgetPiiAsync`.
- Tenant filters cover `Location`; `LocationPii` inherits isolation through its `Location` relationship.
- Provider configuration is a closed switch. PostgreSQL uses Npgsql; four other providers own separate migrations assemblies.
- PostgreSQL migrations currently live in `Explore.Persistence`. No PostGIS extension, NTS package, geography column, GiST index, or spatial readiness check exists.
- Primary AppHost and Compose images are `postgres:18-alpine`. The Compose primary volume target is `/var/lib/postgresql/data`; a PostGIS PG18 image changes the required mount target and therefore needs an explicit development reset or production backup/restore procedure.
- The existing co-located privacy-erasure authority proves the repository can keep an optional concern in a dedicated DbContext, design-time factory, migrations history, and conditional migration branch while reusing the structured primary connection contract.

#### API, BFF, And Blazor

- `LocationController` exposes authorized CRUD with named routes, RFC 7807 errors, strong `If-Match` on PATCH, and private no-store exact reads.
- HAL `create`, `edit`, and `delete` relations are the UI capability source. `TenantLookupTablesSection.razor` still requires correction because it renders location actions unconditionally.
- The generated `EventApiClient.g.cs` is the Blazor API boundary. Existing YARP forwarding hides server credentials and injects trusted auth/tenant context.
- Create/Edit dialogs use plain MudBlazor fields and permit unconditional manual address plus latitude/longitude entry. There is no accessible autocomplete combobox, governed local-address state, provider-status warning, or map component.

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
| `Explore.Blazor.Client.Tests` location/admin tests | Location service/dialog behavior. | Add combobox semantics, debounce/cancellation, provider/local source labels, policy-authorized manual recovery, admin warnings, and HAL gating. |
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
7. The report's unconditional PostGIS 17 image and direct Martin URL do not match PostgreSQL 18, optional managed/self-hosted database capabilities, current browser routing, or the five-provider database contract.
8. Martin auto-publication would bypass tenant/application authorization and disclose exact vector geometry.
9. Three map wrappers and four provider adapters would multiply bundle size, operations, test matrices, and legal surface before demand is proven.
10. Current location admin actions are not consistently HAL-gated.
11. Manual address creation is unconditional and persisted locations have no source/visibility metadata, so one actor can pollute tenant-wide lookup results.
12. Configuration does not model healthy `None` providers or a server-authoritative geocoder/map compatibility matrix.
13. Local application addresses have no explicit invariant preventing future adapters/jobs from upstreaming them into provider datasets.

### 2.6 Unknowns After Investigation

| Unknown | Investigation performed | Owning resolution |
|---|---|---|
| Whether all PostgreSQL deployments may require PostGIS. | Compared ADR-013, provider matrix, migrations ownership, AppHost/Compose, and Npgsql docs; user decided on 2026-08-12. | **Resolved:** no. PostGIS is an explicit optional capability isolated from the primary migration chain. Task 1.1 codifies the decision. |
| Whether the selected managed/self-hosted PostgreSQL service has PostGIS binaries and permits extension activation. | Current structured database settings cannot prove server extensions; managed services differ in activation/privilege workflow. | Task 7.2 preflight and operator documentation. The capability flag is an assertion, not autodetection or installation magic. |
| Production Photon topology and sizing. | Reviewed Photon repository policy and report sizing; public instance is unsuitable for production and planet data is large. | Task 4.1 operator decision and benchmark, including an opt-in local profile and operator-owned regional dataset procedure. |
| How optional spatial cleanup shares the existing privacy-erasure transaction. | Inspected `EfCoreUnitOfWork`, `RetainedAuthorityPrivacyErasureWorkflow`, `PrivacyErasureApplier`, and EF Core cross-context transaction guidance. | **Resolved in design:** Task 7.5 uses the same `DbConnection`/`DbTransaction` and `UseTransactionAsync`; integration evidence remains required. |
| Whether Google Places always requires Google Maps. | Reviewed current official Places display/attribution policies. | **Resolved:** no map is allowed; if a map displays Places content it must be Google Maps. A non-Google map pairing is invalid. Storage/EEA/billing remain in the deferred Google gate. |
| How custom-address policy and visibility fit repository conventions. | Inspected `IHierarchicalSettingsResolver`, `SettingContext`, `SettingScope`, location audit/ownership, and HAL authorization rules. | **Resolved in design:** reuse the five-tier engine for ceiling/mode/org grant, server authorization for the actor decision, `Location.CreatedBy` for creator scope, and separate source vs visibility state. |
| Which MapLibre integration is maintainable on the current .NET/Blazor stack. | Reviewed current package metadata; wrapper adoption/maturity is not proven in this repo. | Task 9.1 compatibility gate. |
| Production basemap/tile source. | Reviewed OSMF policy, Martin behavior, PMTiles self-hosting, and current MapTiler/Stadia candidate surfaces. | Task 9.1 compares candidates against explicit license, attribution, self-hosting, privacy, performance, and cost criteria; public OSM/demo tiles are forbidden defaults. |
| Context7 documentation verification. | No connector/tool/resource is installed. | Re-run before the first new NuGet/UI dependency is accepted, if available. |

## 3. Proposed Future State

### 3.1 Address Selection Flow

1. The server resolves the effective address policy from instance, tenant, and, when present, organization context, then combines it with actor authorization. HAL advertises only the operations that are executable for that caller.
2. An authorized editor types at least the configured minimum characters into an accessible combobox. Blazor debounces/cancels superseded calls and sends a first-party private `POST` through the generated client and existing YARP BFF.
3. The Application query always searches eligible local locations and, only when a provider is configured and ready, also calls `IAddressGeocoder`. `Geocoding:Provider=None` performs no provider I/O and is a healthy state.
4. Local results are tenant-filtered before exact PII is read: tenant-approved locations are tenant-wide; unapproved locations are visible only to their creator or owning organization. Provider results are transient suggestions, not application database rows. The response de-duplicates without co-mingling datasets and labels source/visibility honestly.
5. Each provider suggestion contains safe display fields, provider-required attribution, exact coordinates only for the authorized editor, and a short-lived protected selection token. Raw queries, local exact addresses, provider responses, and tokens are not logged or cached.
6. Selecting a provider result submits the protected token in the normal create/PATCH command. Application unprotects it, rejects expiry/provider/config mismatches, applies the provider's approved persistence profile, and atomically updates the address bundle/coordinate pair.
7. Manual creation is offered only when the effective policy and actor authorization allow it. It stores application-owned data with no coordinate pair, marks the current source as local manual, and initially assigns creator-private or organization visibility. Manual edits clear stale coordinates/provider tokens and revoke discovery approval.
8. A separately authorized promotion command changes local visibility to tenant-approved. It never changes the address-source classification and never submits local data to an external provider.

Effective manual-creation modes are code-defined and server-enforced:

| Mode | Effective actor rule | Initial visibility |
|---|---|---|
| `Disabled` *(default)* | Nobody may create a local manual address. Existing eligible local addresses remain reusable. | Not applicable |
| `AdminOnly` | Only callers authorized for the tenant-level custom-address management action. No Blazor role check. | `TenantApproved` only when the same command is authorized to approve; otherwise creator-private |
| `OrganizationGoverned` | Requires an organization context, an effective organization grant, and the named custom-address creation authorization for that organization. | `OrganizationScoped` |
| `OpenWithModeration` | Any caller whom the authorization provider permits to create the owning event/location; settings alone never grant it. | `CreatorPrivate`, or `OrganizationScoped` when created for an authorized organization |

The instance value is the conservative default/ceiling and may be locked; tenant and organization overrides can only operate within the existing setting-definition scope/lock rules. Invalid or unavailable resolution behaves as `Disabled`.

### 3.2 Governed Discovery Flow

1. An authorized administrator sees a HAL approval action only when the primary provider is PostgreSQL, `Database__Capabilities__Postgis=true`, the isolated PostGIS schema is ready, exact PII coordinates form a valid pair, and privacy policy permits approval.
2. The approval command uses the database-neutral `ILocationDiscoveryPointStore` Application port; the conditionally registered PostGIS adapter snapshots the selected coordinates into its tenant-scoped projection with approval evidence. There is no automatic copy, bulk default, provider switch, or fallback implementation.
3. Coordinate changes, privacy erasure, revocation, or invalidation remove/deactivate the projection in the same database transaction and preserve bounded audit evidence.
4. After an explicit browser action, the client rounds the user's origin and sends it once in a private/no-store `POST` with bounded radius and filters.
5. The PostGIS persistence adapter applies tenant/public/future-occurrence predicates and `ST_DWithin` before `ST_Distance`, selects the minimum eligible occurrence per event, and orders by distance, occurrence time, then event ID.
6. The API returns rounded distance and safe occurrence/location identifiers/names, never either point. Home Discovery renders the list and uses only honest proximity wording when `postgis` readiness is healthy.

### 3.3 Operator Experience

- `Geocoding:Provider=None` and `Maps:Provider=None` are healthy defaults. Disabled providers register no outbound adapter/client, perform no readiness network call, expose no provider-only HAL relation, and do not block local approved-location reuse or policy-authorized manual entry.
- `Geocoding:Provider=Photon` is opt-in and requires an approved endpoint and explicit production mode; public/demo endpoints fail production validation.
- Photon resilience defaults are `Geocoding:Photon:TimeoutSeconds=5`, `Geocoding:Photon:MaxRetries=2`, and `Geocoding:Photon:RetryBackoffMilliseconds=[200,500]`. The timeout is the total autocomplete budget; retry delays and any bounded `Retry-After` must fit inside it, and cancellation stops the pipeline immediately.
- Photon country/region scoping is an operator-owned dataset/import concern documented with the deployment profile, not a new application provider mode. Application country/language bias narrows queries but does not reduce Photon storage or RAM.
- `Database__Capabilities__Postgis` defaults to `false`. With `false`, no PostGIS context, NTS services, optional migrations, spatial health probe, approval action, or exact query is registered or executed, even when `Database__Provider=PostgreSql`.
- `Database__Capabilities__Postgis=true` is valid only with `Database__Provider=PostgreSql`. It asserts that the selected service has PostGIS binaries and that the migrator may activate the extension, or that the operator has already activated it through the managed-service workflow.
- `Discovery__Mode=postgis` is accepted only when the provider is PostgreSQL, the capability flag is true, and extension/schema/index/query readiness succeeds. `area_only` remains the database-neutral default for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
- Local AppHost/Compose continue to use plain PostgreSQL by default. An explicit opt-in image/profile accompanies the capability flag for local PostGIS; an external/serverless operator activates the extension according to the provider's control-plane or SQL policy.
- Image tags and package versions are centrally pinned; lock files are regenerated.
- Metrics expose provider, outcome, latency bucket, rate-limit/retry category, and spatial query health without query text, address, origin, coordinate, tenant, or location identifiers.

The server owns provider compatibility validation; client validation is advisory and mirrors the same matrix for immediate admin feedback:

| Geocoding provider | `Maps:Provider=None` | `Maps:Provider=GoogleMaps` | Any non-Google map provider |
|---|---:|---:|---:|
| `None` | Allowed | Allowed when the map provider itself is configured | Allowed when the map provider itself is configured |
| `Photon` | Allowed | Allowed | Allowed |
| `GooglePlaces` *(deferred)* | Allowed; show required Google branding/attribution beside Places content | Allowed | **Invalid; fail closed and disable Google Places** |

- Google values are not exposed or accepted until the separately approved Google adapter phase exists. That phase must add composition/API validation, the instance-admin form rule, a clear warning/status response, and `.env.example` examples for the two allowed Google combinations plus the forbidden case.
- An invalid `GooglePlaces + non-Google map` configuration never falls back to another geocoder, never sends a Google request, and never relies on UI-only blocking. A settings-write API rejects the pair. If deployment-owned environment configuration contains it, the host remains available in degraded mode with Google disabled so the admin surface can report that Google Places may be paired only with Google Maps or no map provider.

## 4. Non-Negotiable Constraints

1. `LocationPii` remains the only private exact-location source; generic DTOs remain coordinate-free unless already purpose-authorized management contracts.
2. Repositories return entities. Query-specific provider/projection gateways return bounded Application-owned result models and never Persistence rows.
3. Domain and Application do not reference Npgsql, PostGIS, or NetTopologySuite. Spatial types remain in Persistence.
4. Validators are manually instantiated in handlers.
5. Tenant identity comes from trusted context; user-provided tenant IDs are removed.
6. Geocoding and exact proximity use authenticated/private `POST` endpoints despite read semantics, because address/origin PII must not enter URLs or shared caches.
7. Write authorization is enforced server-side and exposed to Blazor through HAL affordances; the UI never inspects roles/claims locally.
8. No origin, raw address query, exact coordinate, protected selection token, or provider credential is logged, traced, metered as a label, cached, or persisted outside its approved store.
9. Geocoding and map providers are independently optional. `None/None` is healthy; no physical-address feature may assume provider or map availability.
10. Manual/custom address creation requires the effective hierarchical policy plus server authorization. Local suggestions are tenant-filtered and creator/organization/tenant-approved scoped before exact PII is materialized.
11. Address source and visibility/approval are separate. Promotion changes visibility only. Application-owned custom addresses are never sent to, merged into, or exported as a provider dataset.
12. Geocoding, normalized address storage, map rendering, and area-only discovery remain database-agnostic. Exact proximity is an optional capability, not a prerequisite for those features.
13. `Database__Capabilities__Postgis=false` is the default. The primary `ExploreDbContext` model/migrations remain PostGIS-free for every provider, including PostgreSQL.
14. PostGIS is the only implemented exact proximity engine. Application owns semantic discovery ports, but there is no runtime provider selector, browser/in-memory/Haversine fallback, or fabricated distance behavior.
15. EF migrations and snapshots, OpenAPI schema, API inventory, and generated client are regenerated, never hand-edited.
16. Breaking contract changes are direct. No obsolete field aliases, dual endpoints, or compatibility adapters.
17. Every new file starts with two `ABOUTME:` lines.

## 5. Architecture And Design Decisions

### Decision 1: Autocomplete ships before spatial discovery

- **Decision:** Deliver a complete Photon-backed address-selection slice independently of PostGIS and maps.
- **Why:** Geocoding improves location data entry immediately and does not require a spatial database or tile server.
- **Alternatives considered:** Report order with PostGIS/Martin first; rejected as unnecessary coupling.
- **Consequences:** Phases 1–6 can ship while ADR-013 remains unactivated.
- **Files/layers:** Location Domain/Application, Infrastructure geocoder, API, generated client, Blazor.

### Decision 2: Optional provider, one Application port, one initial adapter

- **Decision:** `Geocoding:Provider=None` is the default. Application owns `IAddressGeocoder` and normalized models; Infrastructure owns the first opt-in adapter, Photon. Do not add `Explore.Geocoding`.
- **Why:** This preserves dependency direction while keeping project count and registrations small.
- **Alternatives considered:** Direct provider use in handlers; four adapters up front; separate project. Rejected for coupling or speculative scope.
- **Consequences:** `None` performs no provider I/O. Provider selection is closed and startup-validated. A second adapter can reuse the port when approved; a future Google adapter must enforce `GooglePlaces + (GoogleMaps | None)` and reject every other map pairing.
- **Files/layers:** Application contracts/features; Infrastructure adapter/composition.

### Decision 3: Protected stateless selection tokens

- **Decision:** API mints short-lived ASP.NET Core Data Protection tokens containing the normalized provider result and provenance; location commands unprotect them server-side.
- **Why:** This prevents browser tampering without server-side session state or a second provider call.
- **Alternatives considered:** Trust browser coordinates; cache server sessions; resolve every suggestion again. Rejected for integrity, PII-state, or latency reasons.
- **Consequences:** Tokens are private/no-store, purpose-bound, time-bounded, and invalid after provider/config version changes.
- **Files/layers:** Application protection abstraction; Infrastructure Data Protection implementation; API DTOs; location commands.

### Decision 4: Address bundles change atomically behind policy

- **Decision:** Add aggregate methods for manual and geocoded address changes. Manual changes clear coordinates; geocoded changes require a finite both-or-none pair. Application handlers, not Domain entities, enforce the effective creation policy and actor authorization before invoking those methods.
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

### Decision 6: PostGIS is a fully optional declared database capability

- **Decision:** Add `Database__Capabilities__Postgis` with default `false`. Only `Database__Provider=PostgreSql` plus explicit `true` registers the PostGIS adapter, optional context, migration chain, and readiness probe. PostgreSQL without the flag remains plain PostgreSQL; SQLite, SQL Server, MariaDB, and MySQL remain fully supported for all database-neutral features.
- **Why:** A provider name cannot prove that an extension exists, is activated, or is permitted on a managed/serverless service. An explicit declaration keeps self-hosting honest and prevents spatial types from contaminating the primary multi-provider model.
- **Alternatives considered:** Require PostGIS for all PostgreSQL deployments; conditionally alter `ExploreDbContext` based on configuration; detect extensions and silently enable exact mode. Rejected because they make the canonical migration chain non-portable, create EF model-cache/migration variants, or let environmental accidents change product behavior.
- **Consequences:** The operator must opt in and satisfy preflight. `Discovery__Mode=postgis` plus a false/unsupported capability is invalid configuration; a declared capability that later fails readiness hides exact HAL actions and falls back to honest area-only behavior while reporting degraded readiness.
- **Files/layers:** database capability options/validation, conditional Persistence/API/MigrationService composition, AppHost/Compose opt-in, configuration/self-hosting/operations docs.

### Decision 7: PostGIS owns an isolated optional context and migration chain

- **Decision:** Keep `LocationPii` and the primary `ExploreDbContext` unchanged. Add `PostgisDiscoveryDbContext`, a design-time factory, and `__EFPostgisDiscoveryMigrationsHistory` in `Explore.Persistence`. The context is configured and migrated only when the capability flag is true, reusing the structured primary PostgreSQL connection/schema. Application owns scalar approval/query contracts; the optional context owns NTS rows and mapping. For lifecycle writes, existing `EfCoreUnitOfWork` begins and owns the transaction. A Persistence-local `PostgisDiscoveryTransactionCoordinator` creates a short-lived spatial context from `ExploreDbContext.Database.GetDbConnection()`, requires `ExploreDbContext.Database.CurrentTransaction`, and calls `UseTransactionAsync(CurrentTransaction.GetDbTransaction())` before executing spatial changes.
- **Why:** Exact private coordinates and public-discovery purpose have different lifecycles, and Domain must not depend on database spatial types.
- **Alternatives considered:** Add `Point` to `LocationPii`; map the optional row conditionally in `ExploreDbContext`; add spatial objects to the normal PostgreSQL migration chain; automatically copy coordinates; use ambient `TransactionScope`; or let handlers open/commit two contexts. Rejected for privacy, model-cache, migration-portability, capability-isolation, ambient-enlistment/escalation risk, or split transaction ownership.
- **Consequences:** Plain PostgreSQL and every non-PostgreSQL provider apply no spatial migration. The optional context follows the repository's existing separate-context/history pattern. The coordinator runs only inside the existing retryable UoW delegate, never begins/commits/rolls back/disposes the shared transaction or connection, and propagates failure so `EfCoreUnitOfWork` rolls back both contexts. Each retry receives a fresh short-lived spatial context. Capability-off returns before resolving a spatial context or issuing a spatial command. Tests must prove commit, rollback from each side, transient retry, cancellation, and absence of spatial access while disabled; no distributed transaction or eventual-delete gap is accepted.
- **Files/layers:** Application semantic contracts/commands, optional Persistence context/factory/row/config/store/migrations, migration worker, privacy erasure integration, HAL/API.

### Decision 8: Occurrence-level server-side proximity only

- **Decision:** Application owns the database-neutral semantic port `INearbyOccurrenceQuery`; the only planned adapter uses PostGIS to query eligible future published occurrences, reduce to the nearest occurrence per event, and use stable distance/time/event ordering.
- **Why:** An event may have multiple locations and sessions; event-row coordinates would be incorrect.
- **Alternatives considered:** Client Haversine, event-level point, a configurable provider factory, or pretending every database has equivalent spatial semantics. Rejected by ADR-013 and the no-fabricated-fallback rule.
- **Consequences:** Real PostGIS tests and representative `EXPLAIN (ANALYZE, BUFFERS)` evidence are acceptance requirements.
- **Files/layers:** Application query contract/handler, Persistence query, API/Home Discovery.

### Decision 9: Maps are optional; one initial map component

- **Decision:** `Maps:Provider=None` is the default. After a compatibility and tile-source gate, add one app-owned MapLibre component. Do not add `IMapProvider`, `IMapComponent`, or three renderer implementations.
- **Why:** Only one map is required, and wrapper/package maturity is not proven.
- **Alternatives considered:** Google Maps, Leaflet, and MapLibre simultaneously. Rejected due bundle, support, and test-matrix cost.
- **Consequences:** Forms, local address search, and discovery lists remain complete without maps. A future Google Places activation that also selects a map necessarily triggers the separately scoped Google Maps renderer; Google Places with no map remains valid with adjacent Google branding/attribution.
- **Files/layers:** Blazor component/CSS/optional JS, package/lock files, safe public map config.

### Decision 10: Martin cannot publish application spatial tables

- **Decision:** Defer Martin. If a future approved coarse/aggregate tile source exists, use a read-only role, explicit allowlisted view/function, `auto_publish: false`, pinned image, and same-origin route. It never reads `LocationPii` or exact discovery points.
- **Why:** Martin's default discovery publishes every readable spatial table and feature columns, bypassing application authorization.
- **Alternatives considered:** Zero-config auto-discovery and exact venue tiles. Rejected as privacy/tenant violations.
- **Consequences:** Initial MapLibre uses an operator-approved basemap and Application API data; exact venue pins are not public.
- **Files/layers:** Deferred AppHost/Compose/Martin config/BFF work only after a new approval.

### Decision 11: Govern local addresses with existing settings, authorization, and HAL

- **Decision:** Add code-defined address-governance settings to the existing `IHierarchicalSettingsResolver`: a lockable instance/tenant creation mode (`Disabled`, `AdminOnly`, `OrganizationGoverned`, `OpenWithModeration`) and an organization-level grant used only by `OrganizationGoverned`. Combine the resolved policy with a named server authorization action. HAL exposes create/promote operations only after both checks pass.
- **Why:** Configuration decides whether a class of actor may create local addresses; authorization decides whether this caller may perform the action. Neither alone is sufficient, and a user-editable preference must never grant a security capability.
- **Alternatives considered:** New policy tables/service, role checks in Blazor, appsettings-only switch, or a user-level setting. Rejected because the repository already owns a lockable cascade, UI claims are not authoritative, appsettings cannot express tenant/org governance, and users must not self-authorize.
- **Consequences:** Instance administrators can lock a restrictive ceiling; tenant administrators choose the tenant mode; organization grants and Cerbos/application authorization narrow it further. Invalid/unresolved policy fails closed and HAL omits the action.
- **Files/layers:** governance keys/definitions/typed group, Application policy resolver/authorization actions, API HAL policy, existing admin settings surfaces, tests.

### Decision 12: Keep address source, local visibility, and provider licensing separate

- **Decision:** Persist current address source (`ProviderSelection` or `LocalManual`) separately from local visibility (`CreatorPrivate`, `OrganizationScoped`, `TenantApproved`). Reuse `Location.CreatedBy`; add only nullable owning-organization state where required. Provider-specific identifiers/content are persisted only under an approved provider retention profile. Local manual data never enters an upstream provider dataset.
- **Why:** “Tenant approved” is a moderation state, not an origin. Keeping the axes separate prevents approval from erasing provenance, supports correct badges/attribution, and makes tenant-scoped query predicates explicit.
- **Alternatives considered:** `CustomTenantApproved` as an origin, a duplicate `CreatedByUserId`, putting custom rows into Photon/OSM imports, or returning all tenant locations then filtering in memory. Rejected for semantic drift, redundant state, licensing contamination, cross-tenant/PII risk, and poor query behavior.
- **Consequences:** New persisted locations start creator-private or organization-scoped regardless of whether the address was provider-selected or manually entered; tenant-wide reuse requires promotion. Local autocomplete queries apply tenant plus visibility predicates in the database before projecting exact PII. Provider suggestions remain transient and are merged only at the Application response boundary.
- **Files/layers:** Domain lookup-backed state, provider-neutral primary migrations/configuration, Application local-suggestion port/policy, Persistence query, API/HAL, Blazor source badges/admin moderation.

## 6. Implementation Phases

### Phase 1: Governance And Location Integrity

- **Goal:** Codify the optional provider/capability/licensing/governance policy and leave the existing location workflow internally consistent, tenant-authoritative, and ready for governed address acquisition.
- **Depends on:** Implementation approval; the architectural re-baseline was user-reviewed on 2026-08-12.
- **Relevant files:** Existing ADR-013, intents registry, `Location.cs`, location DTOs/validators/commands/handlers/profile, generated contract sources, and matching Application tests.
- **Related skills/rules:** Clean Architecture, CQRS/MediatR, domain/application/API rules.
- **Acceptance criteria:** ADR/provider decisions are explicit; `None` providers are healthy; manual create/update works only when later policy checks allow it and never accepts raw coordinate writes; tenant identity is context-owned; stale coordinate states are impossible.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert only this phase's contract/domain changes. Do not begin provider work while the real create path or decision record is unresolved.

#### Task 1.1: Codify The Approved Optional Capability Contract

- **Type:** modify
- **Layer:** Docs / Architecture
- **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md` (existing), `.claude/contract/intents.yaml` (existing, only if a reusable intent is justified), `docs/ARCHITECTURE.md` and `docs/DOMAIN.md` (existing), this workstream (existing).
- **Description:** Amend ADR-013 and canonical docs to make PostGIS fully optional. Define `Database__Capabilities__Postgis=false`, `Geocoding:Provider=None`, and `Maps:Provider=None` as healthy defaults; require explicit PostgreSQL/PostGIS opt-in for the spatial context; record Photon as the first optional geocoder; codify the deferred Google pairing matrix and provider-specific retention/attribution obligations; define hierarchical local-address governance and the never-upstream invariant; retain the private token flow; and keep Martin/exact public tiles deferred. Cross-link Home Discovery Phase 6 and Event Location Privacy `ELP-730`; do not duplicate their completed work.
- **Acceptance Criteria:**
  - [ ] ADR-013 records the approved optionality policy while retaining its separate runtime activation gate.
  - [ ] The configuration matrix covers PostgreSQL/non-PostgreSQL, capability true/false, discovery mode, extension activation, and readiness outcomes.
  - [ ] Primary migrations remain PostGIS-free; optional schema ownership and migrations history are explicit.
  - [ ] Geocoding, address editing, maps, and area-only discovery remain database-agnostic.
  - [ ] `None/None`, provider-only, and map-only deployments are documented; future Google validation permits only Google Maps or no map.
  - [ ] Custom-address creation/visibility/promotion and the never-upstream boundary are documented against the existing settings/auth/HAL model.
  - [ ] Exact geometry/tile publication remains forbidden unless separately approved.
  - [ ] Context7 unavailability and required dependency revalidation are recorded.
- **Dependencies:** User architecture decision recorded on 2026-08-12; runtime implementation still requires approval.
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
- **Description:** Remove `TenantId` and raw coordinate inputs from create/PATCH schemas, authorize and persist solely from `ITenantContext`, adapt the manual UI to await the effective-policy/HAL slice rather than assuming manual creation, regenerate OpenAPI/inventory/client artifacts, and document the intentional breaking change.
- **Acceptance Criteria:**
  - [ ] No location write accepts tenant identity or coordinate pairs from an untrusted body.
  - [ ] The manual command shape stores address PII with no coordinates only when the later effective-policy decision authorizes it; no unconditional UI bypass remains.
  - [ ] Generated artifacts contain only the new shapes and no aliases.
  - [ ] Location UI actions remain or become HAL-gated.
- **Dependencies:** 1.2.
- **Effort:** L
- **Required Skills/Rules:** `auth-patterns`, `api-controllers.md`, `blazor-ui-conventions`, `openapi-contract-change` intent.

### Phase 2: Application Address Acquisition And Governance Contract

- **Goal:** Add provider-neutral provider/local suggestion semantics, hierarchical creation policy, promotion semantics, and location-command adoption without a network or UI dependency.
- **Depends on:** Phase 1.
- **Relevant files:** New Application geocoding/local-suggestion/policy contracts, governance keys/definitions, DTOs/queries/validators/commands, existing location handlers, authorization actions, and Application tests.
- **Related skills/rules:** CQRS/MediatR, Clean Architecture, application-layer rule.
- **Acceptance criteria:** `None` provider behavior, scoped local/provider merge, protected selection, governed manual creation, and promotion are provider-neutral, bounded, cancellable, fail-closed, and enforced in Application tests.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Disable new HAL actions and remove the unused Application contracts. Do not restore unconditional manual creation or body-controlled coordinates.

#### Task 2.1: Define Minimal Address Acquisition And Policy Contracts

- **Type:** create
- **Layer:** Application
- **Files:** new `src/Explore.Application/Contracts/Geocoding/IAddressGeocoder.cs`, `AddressGeocodingModels.cs`, `IAddressSelectionProtector.cs`; new `src/Explore.Application/Contracts/Persistence/ILocalAddressSuggestionQuery.cs`; new address-policy result/service contract and typed setting group; existing `GovernanceSettingKeys.cs`, new address-governance setting definitions; new `src/Explore.Application/Configuration/GeocodingOptions.cs`; `AuthorizationActions.cs`; and `tests/Event.Application.UnitTests/Features/Geocoding/AddressAcquisitionContractTests.cs`.
- **Description:** Define provider and local suggestion models, current source/visibility classifications, provider attribution/persistence metadata, culture/country bias, result limits, protected selection expiry, explicit outcomes, and the effective address-creation decision. Register a lockable instance/tenant mode (`Disabled`, `AdminOnly`, `OrganizationGoverned`, `OpenWithModeration`) plus an organization grant, stopping security settings above user scope. Keep HTTP, EF, NTS, Google, Photon, and Cerbos transport types out of contracts.
- **Acceptance Criteria:**
  - [ ] Contracts support optional-provider search, scoped local search, server-trusted selection, creation policy, and promotion without provider/persistence DTO leakage.
  - [ ] Query length, result count, supported culture/country, coordinate finiteness, and cancellation are bounded.
  - [ ] Current configuration accepts only implemented values (`None`, `Photon`; maps independently `None` plus the implemented map after its gate). The deferred Google matrix is a mandatory activation contract, not a speculative accepted runtime value.
  - [ ] Settings default to disabled, can be locked at instance/tenant scope, and cannot be loosened by a user preference.
- **Dependencies:** Phase 1.
- **Effort:** M
- **Required Skills/Rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `application-layer.md`.

#### Task 2.2: Merge Optional Provider And Scoped Local Suggestions

- **Type:** create
- **Layer:** Application
- **Files:** new address autocomplete DTO/validator/request/handler, new effective-policy resolver/handler support, promotion command/handler, existing authorization contracts/actions, and `tests/Event.Application.UnitTests/Features/Geocoding/SearchAddressesRequestHandlerTests.cs` plus address-policy/promotion tests.
- **Description:** Manually instantiate validation, resolve tenant/organization/actor policy, query only visible local locations, optionally invoke the configured provider, de-duplicate at the response boundary without merging datasets, protect provider results, label source/visibility, and map safe attribution/outcomes. `None` skips provider resolution entirely. Promotion requires a named authorization decision and changes visibility only.
- **Acceptance Criteria:**
  - [ ] Empty/short/oversized input fails without provider I/O; `None` performs local search only.
  - [ ] Local results include only tenant-approved, current-organization, or creator-private rows; cross-tenant/cross-organization/private rows never reach mapping.
  - [ ] Provider results stay provider-attributed and contain expiring protected tokens; local rows stay locally classified and are never upstreamed.
  - [ ] Timeout/unavailable/limited outcomes are distinguishable without exposing upstream bodies.
  - [ ] Promotion changes visibility to tenant-approved without changing source/provenance and fails closed without policy plus authorization.
- **Dependencies:** 2.1.
- **Effort:** M
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `application-layer.md`.

#### Task 2.3: Enforce Protected Or Governed Manual Location Writes

- **Type:** modify
- **Layer:** Application / Domain
- **Files:** `src/Explore.Domain/Location.cs`, `src/Explore.Application/DTOs/Location/CreateLocationDto.cs`, `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs`, both location validators, `src/Explore.Application/Features/Locations/Requests/Commands/CreateLocationCommand.cs`, `src/Explore.Application/Features/Locations/Requests/Commands/UpdateLocationCommand.cs`, both location command handlers, `tests/Event.Application.UnitTests/Features/Locations/Commands/CreateLocationCommandHandlerTests.cs`, and `tests/Event.Application.UnitTests/Features/Locations/Commands/UpdateLocationCommandHandlerTests.cs` (all existing).
- **Description:** Add an optional protected selection to create/PATCH. Unprotect before persistence, reject expiry/purpose/provider/config/persistence-profile mismatch, then atomically apply the normalized bundle. For a manual write, resolve the effective setting chain and named actor authorization first; assign creator-private or organization-scoped visibility, clear coordinates/provider selection, and never invoke a provider. Unresolved policy fails closed.
- **Acceptance Criteria:**
  - [ ] Tampered, expired, wrong-purpose, and invalid-coordinate tokens fail closed.
  - [ ] Successful selection writes a complete address and coordinate pair once.
  - [ ] Manual creation/update is denied when disabled or unauthorized and assigns the correct initial local visibility when allowed.
  - [ ] Application reuses `Location.CreatedBy`; no duplicate creator field or local role/claim shortcut is introduced.
  - [ ] No external provider call or unprotect operation occurs inside a database transaction.
  - [ ] Concurrency and privacy-erasure behavior remain authoritative.
- **Dependencies:** 2.1, 2.2.
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, `domain.md`.

### Phase 3: Provider-Neutral Local Address Persistence

- **Goal:** Persist source/visibility/organization ownership and implement scoped local lookup/promotion without coupling the primary database to a geocoding or spatial provider.
- **Depends on:** Phase 2.
- **Relevant files:** `Location`, lookup enums/entities/seeding, provider-neutral EF configuration and repositories, generated migrations/snapshots for all five providers, and Persistence integration tests.
- **Related skills/rules:** EF Core guidelines, migrations rule, tenant isolation, Clean Architecture.
- **Acceptance criteria:** Every provider has schema parity; exact local results are filtered in SQL by tenant plus visibility; promotion is concurrency-safe; local data never enters provider storage.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Regenerate unapplied development migrations from corrected entity/configuration state. Never hand-edit a migration or snapshot; no provider-specific SQL/type enters the model.

#### Task 3.1: Add Source, Visibility, And Organization Ownership State

- **Type:** modify / generate
- **Layer:** Domain / Persistence
- **Files:** `src/Explore.Domain/Location.cs`; new lookup enums/entities only where existing lookup conventions require them; lookup configuration/seeding; `LocationConfiguration.cs`; `ExploreDbContext` sets/filters only as required; generated PostgreSQL/SQLite/SQL Server/MariaDB/MySQL migrations and snapshots; domain/persistence tests.
- **Description:** Add lookup-backed current address source (`ProviderSelection`, `LocalManual`) and local visibility (`CreatorPrivate`, `OrganizationScoped`, `TenantApproved`), plus nullable owning organization. Reuse the existing `CreatedBy` audit field for creator scope. Keep provider code/external ID with the PII/provenance bundle only when the selected provider's approved retention profile permits it. Generate every unapplied development migration through `dotnet ef`.
- **Acceptance Criteria:**
  - [ ] Source and visibility are independent; promotion cannot rewrite source.
  - [ ] Every unapproved location has exactly one valid private scope: creator or organization.
  - [ ] Tenant-approved visibility is explicit and tenant-bound; organization ownership is referentially constrained.
  - [ ] All five primary providers have model/snapshot parity and no spatial/provider-specific database type.
  - [ ] Development databases/migration baseline are reset/regenerated as needed; no heuristic legacy visibility/provenance backfill or compatibility shim is added.
  - [ ] No duplicate `CreatedByUserId`, arbitrary JSON policy field, or provider dataset table is added.
- **Dependencies:** Phase 2 contracts.
- **Effort:** XL
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `efcore-persistence.md`, `efcore-migrations.md`, migrations generation invariant.

#### Task 3.2: Implement Scoped Local Search And Promotion Persistence

- **Type:** create / modify
- **Layer:** Persistence
- **Files:** existing/new location repository/query implementation and specification/configuration files; promotion persistence path; `tests/Event.Persistence.IntegrationTests/Repositories/LocalAddressSuggestionQueryTests.cs` and promotion/isolation tests.
- **Description:** Implement `ILocalAddressSuggestionQuery` as a bounded, no-tracking database query that applies tenant and `(TenantApproved OR CreatedByCurrentActor OR OwnerOrganizationCurrent)` predicates before selecting exact PII. Normalize search using provider-neutral EF operations supported across all five databases; use provider-specific optimization only after evidence and behind the repository adapter. Persist promotion with concurrency and tenant predicates.
- **Acceptance Criteria:**
  - [ ] Cross-tenant, cross-organization, and other-creator exact addresses are absent for every supported provider fixture.
  - [ ] Query bounds, ordering, cancellation, and no-tracking projection are deterministic; no in-memory authorization filter or N+1 path exists.
  - [ ] Promotion updates visibility only, is idempotent/concurrency-safe, and cannot promote an erased/foreign location.
  - [ ] Provider-disabled deployments can search eligible local data; no query calls or mutates an upstream provider dataset.
- **Dependencies:** 3.1.
- **Effort:** L
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `optimizing-ef-core-queries`, tenant filter/repository invariants.

### Phase 4: Photon Infrastructure Adapter

- **Goal:** Implement one production-capable geocoder adapter with validated topology and privacy-safe resilience.
- **Depends on:** Phase 3 and operator selection of a self-hosted/contracted Photon endpoint.
- **Relevant files:** New Infrastructure geocoding adapter/composition, existing configuration/secrets/Aspire/Compose docs, lock files, Infrastructure tests.
- **Related skills/rules:** Agentic research, error tracking, Aspire, configuration/security rules.
- **Acceptance criteria:** Photon calls are bounded, resilient, cancellable, observable without PII, and forbidden from using the public demo in production.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Set `Geocoding:Provider=None`; eligible local reuse remains available and policy-authorized manual entry remains available. No location data is lost.

#### Task 4.1: Fix The Photon Deployment Contract

- **Type:** investigate / modify
- **Layer:** DevOps / Docs
- **Files:** `src/Explore.AppHost/AppHost.cs` (existing, if locally hosted), `docker-compose.yml` (existing, if Compose-hosted), `.env.example` (existing), `docs/CONFIGURATION.md`, `SECRETS.md`, `SELF_HOSTING.md`, `OPERATIONS.md` (existing).
- **Description:** Benchmark the required regional/planet data footprint, choose external versus self-hosted topology, pin image/data versions and checksums, define update/swap/rollback capacity, and reject `photon.komoot.io` in production validation. Document an opt-in local Photon container/profile for development plus the operator-owned country/region extraction/import procedure when full planet data is unsuitable. Do not add a planet-scale container to lightweight profiles by default, and do not pretend application query bias reduces the deployed dataset.
- **Acceptance Criteria:**
  - [ ] Production endpoint ownership, capacity, update cadence, health URL, TLS, backup/rebuild, and failure mode are documented.
  - [ ] Local-full may opt into a pinned local Photon service; local-default/core/lite remain lightweight and do not call the public demo implicitly.
  - [ ] Regional dataset generation, attribution, refresh, and swap remain explicit operator responsibilities with a documented supported procedure.
  - [ ] `Geocoding:Provider=None` is the documented default; no public demo endpoint is an implicit development or production fallback.
  - [ ] `.env.example` documents current `None`/`Photon` values and the future Google constraint without advertising `GooglePlaces` as implemented.
- **Dependencies:** 1.1 provider decision.
- **Effort:** L
- **Required Skills/Rules:** `agentic-research`, `aspire`, `external-infrastructure-bootstrap` intent.

#### Task 4.2: Implement Photon And Selection Protection Adapters

- **Type:** create
- **Layer:** Infrastructure
- **Files:** `src/Explore.Infrastructure/Geocoding/PhotonAddressGeocoder.cs` (new), `src/Explore.Infrastructure/Geocoding/PhotonApiModels.cs` (new), `src/Explore.Infrastructure/Geocoding/PhotonOptionsValidator.cs` (new), `src/Explore.Infrastructure/Geocoding/DataProtectionAddressSelectionProtector.cs` (new), `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs` (existing), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/PhotonAddressGeocoderTests.cs` (new), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/DataProtectionAddressSelectionProtectorTests.cs` (new).
- **Description:** Use `HttpClientFactory` and the installed standard resilience pipeline with a 5-second total timeout, at most two retries delayed by 200 ms then 500 ms, cancellation, bounded `Retry-After`, stable user agent, explicit language/country/result parameters, tolerant JSON parsing, attribution, and Data Protection purpose/version/expiry isolation. Retry only transient transport failures, 408, 429, and 5xx when another attempt fits the total budget; do not retry other 4xx responses or log URI query strings.
- **Acceptance Criteria:**
  - [ ] Contract tests cover success, malformed payload, timeout, cancellation, 429/5xx, bounded retries, and redaction.
  - [ ] Defaults and overrides prove the 5-second total budget, two-retry ceiling, 200/500 ms delays, bounded `Retry-After`, and immediate cancellation without time-based flaky tests.
  - [ ] Token protection round-trips and fails for tamper, expiry, purpose, and key/config mismatch.
  - [ ] Metrics/logs contain only provider/outcome/latency categories.
- **Dependencies:** 2.1, 4.1.
- **Effort:** L
- **Required Skills/Rules:** `error-tracking`, `auth-patterns`, infrastructure conventions.

#### Task 4.3: Add Geocoding Readiness And Safe Configuration

- **Type:** create / modify
- **Layer:** Infrastructure / API / Docs
- **Files:** `src/Explore.Infrastructure/Geocoding/GeocodingReadinessProbe.cs` (new), `src/Explore.API/HealthChecks/GeocodingReadinessHealthCheck.cs` (new), `src/Explore.API/Program.cs` (existing), `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md` (existing), `tests/Explore.Infrastructure.Tests/Infrastructure/Geocoding/GeocodingReadinessProbeTests.cs` (new).
- **Description:** Validate provider/base URI/production policy and resilience ranges at startup, register no outbound geocoder for `None`, document the defaults in `CONFIGURATION.md`, and expose a bounded disabled/configured readiness category without executing address lookups or exposing endpoints/secrets. Keep one server-authoritative compatibility validator so a future Google adapter adds its matrix at the same boundary rather than relying on Blazor; that future cross-provider mismatch disables Google and reports degraded status instead of taking down the admin surface.
- **Acceptance Criteria:**
  - [ ] Misconfiguration fails startup with actionable non-secret text.
  - [ ] Readiness treats disabled as healthy and distinguishes configured/unreachable/limited without address I/O.
  - [ ] Future invalid Google/non-Google-map deployment config is feature-fatal but host-nonfatal: zero Google calls, degraded status, clear admin warning; settings mutation rejects it.
  - [ ] API keys remain server-side secret references; Photon has no fake secret setting.
- **Dependencies:** 4.2.
- **Effort:** M
- **Required Skills/Rules:** `error-tracking`, `aspire`, `CONFIGURATION.md`, `SECURITY-MODEL.md`.

### Phase 5: Private Geocoding API Contract

- **Goal:** Expose the Application query through a secured, rate-limited, no-store API and regenerate all consumers.
- **Depends on:** Phase 4.
- **Relevant files:** New API controller/rate-limit policy, route names, OpenAPI/generated client/docs, API integration tests.
- **Related skills/rules:** API controllers, auth patterns, BFF patterns, OpenAPI intent.
- **Acceptance criteria:** Browser calls use a named authenticated POST through existing YARP; PII is absent from URLs/logs/caches; generated contracts are current.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove/disable the endpoint and retain manual location entry; provider infrastructure may remain disabled.

#### Task 5.1: Add Authenticated Autocomplete POST

- **Type:** create
- **Layer:** API
- **Files:** `src/Explore.API/Controllers/GeocodingController.cs` (new), `src/Explore.API/Hateoas/RouteNames.cs` (existing), `src/Explore.API/Extensions/RateLimitingExtensions.cs` (existing), `tests/Event.API.IntegrationTests/Features/GeocodingControllerTests.cs` (new).
- **Description:** Add a named `POST /api/geocoding/address-suggestions` operation with `[Authorize]`, authenticated classification, bounded request body, RFC 7807 errors, `PrivateNoStore`, cancellation, and a dedicated per-user/tenant/IP abuse policy. Do not accept provider names or credentials from the client. With provider `None`, return eligible local results without resolving an external adapter.
- **Acceptance Criteria:**
  - [ ] Anonymous, cross-tenant, oversized, malformed, and rate-limited requests fail correctly.
  - [ ] Response and errors carry `Cache-Control: private, no-store`; no output cache/ETag applies.
  - [ ] Endpoint/access logging tests prove request body and query text are not emitted.
- **Dependencies:** Phase 4.
- **Effort:** M
- **Required Skills/Rules:** `api-controllers.md`, `auth-patterns`, `add-write-endpoint` intent.

#### Task 5.2: Publish Address Acquisition And Moderation Through HAL

- **Type:** modify
- **Layer:** API / Application
- **Files:** existing location/admin HAL policies and assemblers, location DTOs, new/existing address-governance status DTO/query, promotion endpoint/route, settings endpoints only where the existing generic settings surface is insufficient, and matching API/HAL integration tests.
- **Description:** Advertise local autocomplete when eligible local search is executable, provider autocomplete only when its provider is ready, `create_custom_address` only when effective hierarchical policy plus actor authorization permit it, and `approve_tenant_address` only for an eligible scoped location and authorized moderator. Expose non-secret effective provider/map compatibility and policy status to the instance/tenant admin surface. Omit actions on unresolved policy, denial, invalid pairing, missing scope, or erased data.
- **Acceptance Criteria:**
  - [ ] Provider `None` does not suppress eligible local search; unavailable provider results do not suppress policy-authorized manual entry.
  - [ ] Custom-create and tenant-approval relations exactly reflect server policy/authorization and use named routes/methods.
  - [ ] Invalid future Google/non-Google-map state exposes no Google action and returns a clear non-secret admin warning.
  - [ ] Blazor needs no local claim, role, provider, readiness, or policy authority check.
- **Dependencies:** 3.2, 5.1.
- **Effort:** M
- **Required Skills/Rules:** `blazor-ui-conventions`, HAL invariant, API HATEOAS rules.

#### Task 5.3: Regenerate And Document The API Contract

- **Type:** modify
- **Layer:** API / Blazor / Docs
- **Files:** `schemas/openapi_islamu-event.json`, `docs/API_CONTRACT_INVENTORY.md`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md` (existing).
- **Description:** Regenerate schema/inventory/client once, document POST-for-private-read rationale, rate limits, no-store semantics, suggestion/token shapes, and intentional removal of raw coordinate writes.
- **Acceptance Criteria:**
  - [ ] OpenAPI parity passes and operation IDs are stable.
  - [ ] Generated client has the new method and only the current location shapes.
  - [ ] Documentation contains no example address, coordinate, token, or secret that resembles real PII.
- **Dependencies:** 5.1, 5.2.
- **Effort:** M
- **Required Skills/Rules:** `openapi-contract-change` intent, documentation style guide.

### Phase 6: Accessible Location Editing Experience

- **Goal:** Replace raw coordinate fields with accessible scoped autocomplete and policy-authorized manual entry that consume HAL and expose clear optional-provider status.
- **Depends on:** Phase 5.
- **Relevant files:** New Blazor component/CSS, location service/dialogs, lookup section HAL flow, generated client, Blazor tests/docs.
- **Related skills/rules:** Blazor UI conventions, CSS isolation, design system, accessibility.
- **Acceptance criteria:** Keyboard/screen-reader users can search scoped local/provider results, select, clear, recover, and manually enter only when allowed; admins see clear provider/policy compatibility status; page code owns no authorization decision.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Hide unavailable relations/components. Eligible saved locations and policy-authorized manual entry remain usable with no coordinates; denied manual creation stays denied.

#### Task 6.1: Build The Address Autocomplete Component

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor` (new), `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor.cs` (new only if markup-backed code is not concise), `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.razor.css` (new), `tests/Explore.Blazor.Client.Tests/Components/Locations/AddressAutocompleteTests.cs` (new).
- **Description:** Implement the WAI-ARIA combobox/listbox interaction with explicit label/help/error/status text, minimum query length, debounce, cancellation of superseded calls, bounded results, keyboard navigation, focus management, live announcements, provider-required attribution, source/visibility badges (`provider`, `tenant-approved`, `organization`, `mine`), loading/empty/error states, and RTL/logical CSS.
- **Acceptance Criteria:**
  - [ ] Arrow/Home/End/Enter/Escape/Tab behavior and focus semantics are tested.
  - [ ] Only the latest request can update results; disposal cancels pending work.
  - [ ] Reduced-motion, mobile touch targets, localization, and policy-authorized manual recovery are preserved.
- **Dependencies:** 5.3.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `ACCESSIBILITY.md`.

#### Task 6.2: Integrate Create And Edit Dialogs

- **Type:** modify / delete
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor.cs`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor.cs`, `src/Explore.Blazor.Client/Services/LocationService.cs` (existing), `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs` (new), `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs` (existing).
- **Description:** Replace latitude/longitude inputs with autocomplete selection; atomically populate normalized fields/token, let policy/HAL-authorized manual edits clear the selection, preserve PATCH concurrency, and render provider-disabled/failure as recoverable inline status. Add the smallest existing-admin-pattern section needed to edit address governance and show effective provider/map status. Do not make deployment-owned endpoints/credentials browser-editable. When the deferred Google adapter is activated, the same form must allow `GoogleMaps` or `None`, reject every other map value inline, and display the server warning without treating client validation as authority.
- **Acceptance Criteria:**
  - [ ] Create and edit submit the protected selection only while it matches visible fields.
  - [ ] Manual controls appear only through the server relation; authorized edits remove stale token/coordinates without blocking save.
  - [ ] Provider unavailable/rate-limited states never discard typed input.
  - [ ] `None/None` is represented as a valid, non-error status; policy and provider/map warnings are explicit and accessible.
  - [ ] Current UI does not offer unimplemented `GooglePlaces`; its activation task must add both allowed pairings and forbidden-pairing validation together.
- **Dependencies:** 6.1.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, generated-client-only API access.

#### Task 6.3: Enforce HAL-Gated Location Affordances

- **Type:** modify
- **Layer:** Blazor / API
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Tenant/Components/TenantLookupTablesSection.razor` (existing), `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationsTests.cs` (existing), `tests/Explore.Blazor.Client.Tests/Services/LocationServiceTests.cs` (existing).
- **Description:** Preserve HAL links through the client service and gate create/edit/delete/autocomplete/custom-create/tenant-approve actions only by link presence. Remove unconditional action rendering and any local claim/role/provider/policy logic.
- **Acceptance Criteria:**
  - [ ] Missing relations remove the corresponding control.
  - [ ] Present relations invoke their advertised URL/method.
  - [ ] Tests cover authorized, unauthorized, provider-disabled/unready, policy-disabled, organization-scoped, and tenant-approved resources.
- **Dependencies:** 5.2, 6.2.
- **Effort:** M
- **Required Skills/Rules:** HAL invariant, `blazor-component-affordance` intent.

### Phase 7: Optional PostGIS Capability Package

- **Goal:** After explicit ADR activation, add an opt-in PostGIS adapter/context/migration chain that never changes the default primary database contract or database-neutral features.
- **Depends on:** Phase 1 Task 1.1 codification; explicit Phase 7 activation approval; Event Location Privacy migration baseline `ELP-230C`; erasure/remediation dependencies `ELP-515`, `ELP-520`, `ELP-530` reconciled.
- **Relevant files:** Database capability configuration, conditional provider/migration composition, optional PostGIS context/factory/row/config/store/migrations, opt-in AppHost/Compose topology, privacy erasure flow, docs/tests.
- **Related skills/rules:** EF Core, Clean Architecture, auth/privacy, Aspire, migration rules.
- **Acceptance criteria:** Capability-off deployments apply no PostGIS schema or runtime registration; explicit PostgreSQL capability-on deployments have an isolated `geography(Point,4326)` projection plus GiST/tenant indexes; every provider retains database-neutral behavior; approval is explicit; revocation/erasure is transactional.
- **Gate entry checklist:** A repository owner/product architecture authority has explicitly approved activation; Task 7.1 has changed ADR-013 to `Accepted` with the actual date and named decider/role; `ELP-230C`, `ELP-515`, `ELP-520`, and `ELP-530` evidence is reconciled; the target PostgreSQL service's PostGIS installation/activation path is documented; capability-off rollback is confirmed. No spatial implementation task begins before every item is satisfied.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Set `Discovery__Mode=area_only` and `Database__Capabilities__Postgis=false`, which unregisters the adapter and leaves any optional schema dormant. Do not remove primary migrations or emulate proximity elsewhere; remove the optional schema only through its dedicated migration/restore procedure when explicitly required.

#### Task 7.1: Accept ADR-013 And Record The Activation Decision

- **Type:** modify
- **Layer:** Docs / Architecture
- **Files:** `docs/adr/ADR-013-postgis-proximity-discovery.md` (existing), this workstream (existing), and `docs/ARCHITECTURE.md` only if its decision index/status is duplicated there.
- **Description:** After, and only after, explicit activation approval from the repository owner/product architecture authority, the Phase 7 implementation owner changes ADR-013 from `Proposed` to `Accepted`, records the actual acceptance date and named decider/role, and confirms that the accepted text includes the optional-capability, isolated-migration, origin-privacy, and exact-discovery boundaries. The implementation owner records the decision; they do not invent or self-grant it.
- **Acceptance Criteria:**
  - [ ] The approval source identifies the decider and authority to activate Phase 7.
  - [ ] ADR-013 is `Accepted` with the actual date and decider/role, with no placeholder metadata.
  - [ ] The accepted ADR and this plan agree that PostgreSQL does not imply PostGIS and that the primary EF model/migrations remain spatial-free.
  - [ ] If approval is absent or withdrawn, ADR-013 stays `Proposed` and Tasks 7.2–8.3 remain blocked.
- **Dependencies:** Task 1.1 codification, explicit Phase 7 activation approval, and reconciled Event Location Privacy prerequisites.
- **Effort:** S
- **Required Skills/Rules:** architecture governance, `DOCUMENTATION_STYLE_GUIDE.md`, ADR-013 activation contract.

#### Task 7.2: Add Explicit Optional PostGIS Capability Composition

- **Type:** create / modify
- **Layer:** Persistence / DevOps
- **Files:** `src/Explore.Secrets/Database/PrimaryDatabaseCapabilityOptions.cs` (new), `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` (existing), `src/Explore.Persistence/PersistenceServicesRegistration.cs` (existing), `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing), `src/Event.MigrationService/Worker.cs` (existing), `.env.example` (existing), `src/Explore.AppHost/AppHost.cs` (existing), `docker-compose.yml` (existing), `tests/Event.Persistence.IntegrationTests/Database/PrimaryDatabaseProviderCompositionTests.cs` (existing), `tests/Event.Architecture.Tests/AppHostTopologyArchitectureTests.cs` (existing), `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md` (existing).
- **Description:** Bind `Database:Capabilities:Postgis` with default `false`; map Compose input `DATABASE_CAPABILITIES_POSTGIS` to `Database__Capabilities__Postgis`; validate the provider/capability/discovery-mode matrix; and conditionally register/migrate the optional PostGIS context only for explicit PostgreSQL opt-in. Preserve `postgres:18-alpine` as the local default. For opt-in local development, select a pinned PostGIS image/profile explicitly; for external/serverless PostgreSQL, document operator activation/privilege prerequisites. Leave `ExploreDbContext`, its migrations, and Keycloak/Cerbos/privacy-authority databases unchanged.
- **Acceptance Criteria:**
  - [ ] The flag defaults to false for every deployment, including PostgreSQL, and false registers/runs no PostGIS service, probe, migration, or schema access.
  - [ ] Capability true on SQLite, SQL Server, MariaDB, or MySQL fails configuration validation clearly; `Discovery__Mode=postgis` with capability false also fails clearly.
  - [ ] Capability true on PostgreSQL selects the isolated context/migrator only; it never changes `ExploreDbContext` or the canonical PostgreSQL migration history.
  - [ ] Plain PostgreSQL remains the AppHost/Compose default; opt-in PostGIS images are pinned and managed PostgreSQL activation steps are explicit.
  - [ ] The current Aspire PostgreSQL container-image override API is revalidated from official documentation at implementation time before changing AppHost topology.
  - [ ] Capability/configuration/readiness diagnostics expose no connection or extension details beyond bounded operator-safe categories.
- **Dependencies:** 7.1 and ELP-230C migration integrity.
- **Effort:** L
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `aspire`, `efcore-migrations.md`.

#### Task 7.3: Generate The Isolated PostGIS Projection Migration

- **Type:** create / modify
- **Layer:** Persistence
- **Files:** `Directory.Packages.props` (existing), `src/Explore.Persistence/Explore.Persistence.csproj` and affected `packages.lock.json` files (existing), `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryDbContext.cs` (new), `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryDbContextFactory.cs` (new), `src/Explore.Persistence/Spatial/Postgis/LocationDiscoveryPointRow.cs` (new), `src/Explore.Persistence/Spatial/Postgis/LocationDiscoveryPointConfiguration.cs` (new), `src/Explore.Persistence/Spatial/Postgis/PostgisLocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Contracts/Persistence/ILocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Contracts/Discovery/LocationDiscoveryPointModels.cs` (new), `src/Explore.Persistence/Migrations/PostgisDiscovery/<generated>_InitialPostgisDiscovery.cs` (generated), `src/Explore.Persistence/Migrations/PostgisDiscovery/PostgisDiscoveryDbContextModelSnapshot.cs` (generated), `tests/Event.Persistence.IntegrationTests/Repositories/LocationDiscoveryPointStoreTests.cs` (new), `tests/Event.Persistence.IntegrationTests/Migrations/PostgisDiscoveryMigrationTests.cs` (new).
- **Description:** Pin the Npgsql NTS plugin only in Persistence; configure it only on `PostgisDiscoveryDbContext`; give that context `__EFPostgisDiscoveryMigrationsHistory`; and map a tenant/location-unique row with explicit `geography(Point,4326)`, approval evidence/concurrency, FK, GiST point index, and relational indexes. Its generated migration may idempotently activate PostGIS when the migrator is permitted; managed services that restrict activation require operator preactivation. Generate with the dedicated design-time context and never edit generated files.
- **Acceptance Criteria:**
  - [ ] The primary `ExploreDbContext` snapshot and all five normal provider migration chains have no PostGIS/NTS/spatial diff.
  - [ ] The dedicated history/migration activates or verifies PostGIS and creates the exact constrained column/indexes only when the capability is enabled.
  - [ ] Domain/Application assemblies have no NTS/PostGIS dependency.
  - [ ] Generic/public DTOs contain no point/coordinates.
  - [ ] Capability-off plain PostgreSQL tests prove no optional history/table is created; real PostGIS tests inspect extension, column type, index method, tenant uniqueness, and isolation.
- **Dependencies:** 7.2.
- **Effort:** XL
- **Required Skills/Rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`, `efcore-persistence.md`, `efcore-migrations.md`.

#### Task 7.4: Implement Explicit Approval And Revocation

- **Type:** create
- **Layer:** Application / Persistence
- **Files:** `src/Explore.Application/Features/Locations/Requests/Commands/ApproveLocationDiscoveryPointCommand.cs` (new), `RevokeLocationDiscoveryPointCommand.cs` (new), matching handlers/validators under `Features/Locations/` (new), `src/Explore.Application/Contracts/Persistence/ILocationDiscoveryPointStore.cs` (new), `src/Explore.Persistence/Spatial/Postgis/PostgisLocationDiscoveryPointStore.cs` (new), `src/Explore.Application/Services/LocationPrivacyGovernanceMutationService.cs` (existing), `tests/Event.Application.UnitTests/Features/Locations/Commands/LocationDiscoveryPointCommandHandlerTests.cs` (new), `tests/Event.Persistence.IntegrationTests/Repositories/LocationDiscoveryPointStoreTests.cs` (new).
- **Description:** Keep the command/port scalar and database-neutral. Advertise/execute it only when exact discovery capability is ready. The PostGIS adapter requires management authorization, tenant ownership, active non-erased PII, valid coordinate pair, explicit approval version/evidence, and readiness. Reapproval replaces the point only through a concurrency-safe command; coordinate changes revoke rather than silently republish.
- **Acceptance Criteria:**
  - [ ] No location gains a discovery point implicitly or through backfill defaults.
  - [ ] Capability-off and non-PostgreSQL deployments never resolve or call the PostGIS store and expose no approval HAL relation.
  - [ ] Cross-tenant, erased, private-policy-ineligible, stale-concurrency, and invalid-coordinate approvals fail closed.
  - [ ] Revocation makes the point immediately ineligible while retaining bounded audit evidence.
- **Dependencies:** 7.3, ELP-530 semantics.
- **Effort:** L
- **Required Skills/Rules:** `cqrs-mediatr-guidelines`, `auth-patterns`, location privacy contract.

#### Task 7.5: Integrate Erasure, Correction, And Readiness

- **Type:** modify / create
- **Layer:** Application / Persistence / Infrastructure / Docs
- **Files:** `src/Explore.Persistence/Repositories/UserLocationPrivacyErasureRepository.cs`, `src/Explore.Application/Services/LocationPrivacyGovernanceMutationService.cs`, `src/Explore.Application/Services/LocationPrivacyOutboxMessageFactory.cs`, `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryDbContext.cs`, `src/Explore.Persistence/Spatial/Postgis/PostgisLocationDiscoveryPointStore.cs`, `src/Explore.Persistence/Spatial/Postgis/PostgisDiscoveryTransactionCoordinator.cs` (new), `src/Explore.API/HealthChecks/PostgisDiscoveryReadinessHealthCheck.cs` (new), `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs`, `tests/Event.Persistence.IntegrationTests/Privacy/PostgisDiscoveryTransactionCoordinatorTests.cs` (new), `tests/Event.Architecture.Tests/DiscoveryPostgisSeparationArchitectureTests.cs`, `dev/active/event-location-privacy/event-location-privacy-tasks.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/SECURITY-MODEL.md`, `docs/TESTING.md` (existing unless marked new).
- **Description:** Keep the current `EfCoreUnitOfWork.ExecuteSerializableAsync` boundary as sole transaction and execution-strategy owner. Inside its delegate, the Persistence coordinator requires `ExploreDbContext.Database.CurrentTransaction`, builds a fresh short-lived `PostgisDiscoveryDbContext` with the same open `DbConnection`, and enlists it with `UseTransactionAsync(CurrentTransaction.GetDbTransaction())`. It applies spatial delete/deactivation and saves without committing, rolling back, closing, or disposing the shared transaction/connection; any exception propagates to the UoW, which rolls back both contexts and recreates the spatial context on retry. When capability is disabled, return before constructing the spatial context or executing a table/extension command. Reuse existing idempotent outbox/remediation mechanisms and add bounded readiness for declared capability, extension activation, dedicated migration, table/index/query, and mode consistency.
- **Acceptance Criteria:**
  - [ ] Erasure cannot commit while an active discovery point survives.
  - [ ] The coordinator fails fast outside an active primary transaction and proves both contexts use the same `DbConnection` and `DbTransaction`.
  - [ ] Success commits both contexts; a primary or spatial failure rolls both back; transient retry creates a fresh spatial context and remains idempotent; cancellation commits neither.
  - [ ] Only `EfCoreUnitOfWork` owns begin/commit/rollback/retry. No `TransactionScope`, second connection, nested transaction, or distributed transaction is introduced.
  - [ ] Capability-off erasure remains provider-neutral and never references a nonexistent optional table.
  - [ ] Retry/replay is idempotent and does not recreate a point.
  - [ ] Readiness exposes categories only, never SQL, identifiers, addresses, origins, points, or connection strings.
  - [ ] Implementation evidence, not planning text, is used to reconcile `ELP-730` and Home Discovery Phase 6.
- **Dependencies:** 7.4, ELP-515/520/530.
- **Effort:** XL
- **Required Skills/Rules:** `error-tracking`, privacy/outbox patterns already owned by Event Location Privacy, migration rules.

### Phase 8: Exact Nearby Occurrence Discovery

- **Goal:** Add the ADR-approved exact-nearby semantics through a database-neutral Application port and the optional PostGIS adapter, without exposing points or introducing a fallback.
- **Depends on:** Phase 7.
- **Relevant files:** Application request/result/handler, Persistence spatial query, Public Experience controller/handler/contracts, generated client/docs, API tests.
- **Related skills/rules:** CQRS, EF query optimization, API/privacy/HAL.
- **Acceptance criteria:** Exact results are tenant-safe, occurrence-correct, stable, private/no-store, and index-backed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Configure `area_only` or disable the PostGIS capability; UI removes exact wording/actions and uses the existing database-neutral area flow. Never run a client/in-memory fallback.

#### Task 8.1: Implement The PostGIS Occurrence Query

- **Type:** create
- **Layer:** Application / Persistence
- **Files:** `src/Explore.Application/Contracts/Persistence/INearbyOccurrenceQuery.cs` (new), `src/Explore.Application/DTOs/PublicExperience/NearbyEventDiscoveryDtos.cs` (new), `src/Explore.Application/Features/PublicExperience/Requests/Queries/GetNearbyEventDiscoveryRequest.cs` (new), `src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetNearbyEventDiscoveryRequestHandler.cs` (new), `src/Explore.Persistence/Spatial/Postgis/PostgisNearbyOccurrenceQuery.cs` (new), `tests/Event.Persistence.IntegrationTests/Repositories/PostgisNearbyOccurrenceQueryTests.cs` (new).
- **Description:** Keep request/result semantics database-neutral, then implement the only approved adapter with rounded origin/radius/filter validation, tenant/public/published/future/in-person/active-point predicates, `ST_DWithin`, minimum `ST_Distance` per event, and stable distance/start/event ordering before pagination. Keep all spatial evaluation server-side and register the adapter only when capability readiness succeeds.
- **Acceptance Criteria:**
  - [ ] Tests cover inside/on/outside radius, ties, multi-location events, past/online/draft/private/deleted exclusions, tenant isolation, and cancellation.
  - [ ] Result exposes only rounded distance and safe occurrence/location metadata.
  - [ ] Representative `EXPLAIN (ANALYZE, BUFFERS)` evidence uses GiST without loading all points/client evaluation.
  - [ ] Capability-off and non-PostgreSQL paths retain area-only behavior and never resolve the exact-query adapter.
- **Dependencies:** Phase 7.
- **Effort:** XL
- **Required Skills/Rules:** `optimizing-ef-core-queries`, `cqrs-mediatr-guidelines`, ADR-013.

#### Task 8.2: Add Private Nearby And Approval API Operations

- **Type:** create / modify
- **Layer:** API
- **Files:** `src/Explore.API/Controllers/PublicExperienceController.cs` (existing nearby route owner), `src/Explore.API/Controllers/LocationDiscoveryController.cs` (new approval route owner), `src/Explore.API/Hateoas/RouteNames.cs`, `src/Explore.API/Hateoas/Policies/LocationLinkPolicy.cs`, `src/Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs`, `src/Explore.API/Extensions/RateLimitingExtensions.cs` (existing), `tests/Event.API.IntegrationTests/Features/PublicExperienceNearbyControllerTests.cs` (new), `tests/Event.API.IntegrationTests/Features/LocationDiscoveryControllerTests.cs` (new), generated OpenAPI/client artifacts (existing/generated).
- **Description:** Add named authenticated POST nearby and approve/revoke operations with tenant context, server authorization, RFC 7807, bounded bodies, private/no-store, no output cache/ETag, and HAL gating. The nearby origin is never present in routes, logs, errors, metrics, or durable settings.
- **Acceptance Criteria:**
  - [ ] Unsupported/unready modes fail honestly and advertise no HAL action.
  - [ ] Nearby request/response privacy headers and logging boundaries are tested.
  - [ ] Approve/revoke operations require executable location-management authorization and concurrency.
- **Dependencies:** 7.4, 8.1.
- **Effort:** L
- **Required Skills/Rules:** `auth-patterns`, `api-controllers.md`, HAL invariant.

#### Task 8.3: Integrate Home Discovery And Canonical Contracts

- **Type:** modify
- **Layer:** Application / API / Blazor / Docs
- **Files:** `src/Explore.Application/Features/PublicExperience/Handlers/Queries/GetHomeDiscoveryQueryHandler.cs`, `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs`, `src/Explore.Blazor.Client/Services/HomeDiscoveryService.cs`, `schemas/openapi_islamu-event.json`, `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs`, `docs/API_CONTRACT_INVENTORY.md`, `tests/Event.API.IntegrationTests/Features/PublicExperienceHomeDiscoveryControllerTests.cs`, `tests/Explore.Blazor.Client.Tests/Services/HomeDiscoveryServiceTests.cs`, `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/BLAZOR.md`, `docs/SELF_HOSTING.md` (existing).
- **Description:** Activate dormant distance/nearest-occurrence fields only in PostGIS mode, keep area responses cacheable and coordinate-free, keep nearby responses private/no-store, and switch wording/actions based on HAL/capability rather than local configuration guesses.
- **Acceptance Criteria:**
  - [ ] Area-only behavior and cache semantics are unchanged.
  - [ ] Exact mode shows rounded distance/nearest occurrence only after explicit user action.
  - [ ] Origin is never stored in preferences; only area ID/mode/bounded radius may persist if approved.
  - [ ] Generated contracts and overlapping workstream evidence are reconciled.
- **Dependencies:** 8.2.
- **Effort:** L
- **Required Skills/Rules:** `blazor-bff-patterns`, `blazor-ui-conventions`, Home Discovery contract.

### Phase 9: One Accessible Map Experience

- **Goal:** Add one maintainable map only after approving a production basemap source and confirming MapLibre integration compatibility.
- **Depends on:** Phase 6 for admin preview; Phase 8 for nearby discovery; explicit tile-source/product decision.
- **Relevant files:** Central package/lock files if a wrapper is selected, new isolated Blazor map component/CSS/optional JS module, safe map config contract, location/Home components, Blazor tests/docs.
- **Related skills/rules:** Blazor UI/CSS/design system/accessibility, agentic research.
- **Acceptance criteria:** The map is supplementary, keyboard/screen-reader-safe, RTL/responsive, and never exposes exact public event coordinates.
- **Gate entry checklist:** Compare at least one operator-controlled option (PMTiles/Protomaps on owned object storage or equivalent) with any hosted candidate such as MapTiler or Stadia Maps using current official terms. The decision record must: identify every software/data/style/font/sprite license and required attribution; confirm production/commercial, caching/CDN, redistribution/offline, and self-hosting rights for the chosen topology; document hosted-provider privacy/DPA/data-region implications; prove p95 tile response at or below 750 ms and first useful map render at or below 3 seconds on the supported median device/representative 4G profile; record storage/egress/operations TCO for self-hosting or an approved hard monthly spend ceiling plus overage behavior for hosted service; and retain a complete no-map experience. A format/runtime license such as PMTiles BSD-3-Clause does not license the underlying map data.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Remove/hide the map and keep the complete textual address/discovery list. Mapping is never required to create a location or browse nearby results.

#### Task 9.1: Select The Production Map Integration And Tile Source

- **Type:** investigate / modify
- **Layer:** Blazor / DevOps / Docs
- **Files:** `Directory.Packages.props`, `src/Explore.Blazor.Client/Explore.Blazor.Client.csproj`, `src/Explore.Blazor.Client/packages.lock.json`, `src/Explore.Application/Models/PublicExperience/PublicExperienceHomeBlocksConfig.cs`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/adr/ADR-013-postgis-proximity-discovery.md` (existing; create a separate ADR only if the accepted map-data decision is not covered).
- **Description:** Prove current .NET/Blazor compatibility, SSR/prerender behavior, disposal, CSP, accessibility hooks, RTL, bundle size, and maintenance health for the report's MapLibre wrapper. Compare PMTiles/Protomaps self-hosting, MapTiler, Stadia Maps, and any better evidenced candidate against the phase gate; the shortlist is not a preselection. Select one licensed contracted/self-hosted basemap/style URL and attribution. If the wrapper or every source fails the gate, stop the phase; do not add Leaflet plus a custom MapLibre abstraction in the same slice.
- **Acceptance Criteria:**
  - [ ] One integration and one production tile source are approved and pinned.
  - [ ] The decision record covers software, map-data, style, glyph, sprite, attribution, production/commercial, caching/CDN, redistribution/offline, self-hosting, privacy/DPA/data-region, and termination/export obligations.
  - [ ] Representative evidence meets the 750 ms p95 tile and 3-second first-useful-render budgets, with bundle size and supported-device/WebGL behavior recorded.
  - [ ] Self-hosted storage/egress/operations TCO or a hosted hard monthly cost ceiling and overage policy is approved; unbounded request-based spend is rejected.
  - [ ] Public OSM/demo tiles and internal-only URLs are rejected as production defaults.
  - [ ] Only safe style URL/provider ID/attribution reach the browser; keys follow provider-specific public-token restrictions.
- **Dependencies:** Explicit product/operations decision.
- **Effort:** M
- **Required Skills/Rules:** `agentic-research`, `blazor-ui-conventions`, design system/accessibility docs.

#### Task 9.2: Implement The Supplementary Map Component

- **Type:** create
- **Layer:** Blazor
- **Files:** `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor` (new), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.cs` (new only if needed), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.css` (new), `src/Explore.Blazor.Client/Components/Maps/EventDiscoveryMap.razor.js` (new only if needed), `tests/Explore.Blazor.Client.Tests/Components/Maps/EventDiscoveryMapTests.cs` (new).
- **Description:** Render an authorized admin preview for the current selected location and a public nearby context using only the user's local origin/coarse public context; do not render exact event pins. Provide a complete adjacent list, semantic heading/instructions, focus-safe controls, keyboard zoom/pan alternatives, reduced motion, responsive sizing, deterministic disposal, and a visible `<noscript>`/no-WebGL fallback that points users to the complete textual experience.
- **Acceptance Criteria:**
  - [ ] Every map fact/action has a non-map textual equivalent.
  - [ ] No point, address, token, provider key, or internal tile URL appears in public HTML/telemetry.
  - [ ] Component survives prerender, navigation, JS failure, and provider outage with the list/form intact.
  - [ ] Disabled JavaScript, unavailable WebGL, and unsupported/low-end devices receive an accessible fallback message without losing any address or discovery action.
- **Dependencies:** 9.1.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, CSS isolation, `ACCESSIBILITY.md`.

#### Task 9.3: Integrate Map Preview And Nearby Context

- **Type:** modify
- **Layer:** Blazor / Docs
- **Files:** `src/Explore.Blazor.Client/Pages/Admin/Dialogs/CreateLocationDialog.razor`, `src/Explore.Blazor.Client/Pages/Admin/Dialogs/EditLocationDialog.razor`, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor`, `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.razor.css`, `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs`, `tests/Explore.Blazor.Client.Tests/Pages/Admin/LocationDialogTests.cs` (new), `tests/Explore.Blazor.Client.Tests/Components/Discovery/HomeDiscoveryExperienceTests.cs`, `docs/BLAZOR.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md` (existing unless marked new).
- **Description:** Show the map only when its HAL/config capability exists. Admin preview may display the editor-authorized selection; public Home remains list-first and uses only coarse/user-local context. Ensure map failure does not affect save/discovery behavior.
- **Acceptance Criteria:**
  - [ ] UI is HAL/config-gated and has stable loading/empty/error/fallback states.
  - [ ] Location save and nearby list work with maps disabled.
  - [ ] Responsive, RTL, keyboard, focus, announcement, and reduced-motion tests pass.
- **Dependencies:** 9.2, 6.2, 8.3.
- **Effort:** L
- **Required Skills/Rules:** `blazor-ui-conventions`, `design-system`, HAL invariant.

## 7. Testing Strategy

Each phase owns one Release build and one selected non-browser project test, run once after all phase tasks. Phase 1 and Phase 2 intentionally repeat `Event.Application.UnitTests`: Phase 1 protects breaking location aggregate/command changes; Phase 2 protects address policy, scoped merge, promotion, and token boundaries. Phase 3 uses Persistence integration tests for all-provider schema/query isolation. Phase 5 and Phase 8 intentionally repeat `Event.API.IntegrationTests`: they cover distinct private contracts, first address acquisition/moderation and later PostGIS nearby/approval.

Tests added during a phase belong to that phase's selected project wherever practical. Intent-mandated architecture/persistence/client coverage that cannot fit the selected project is placed in the later phase whose selected project owns that surface. Do not schedule solution-level `dotnet test`, browser automation, live Aspire/Docker startup, or a separate verification phase.

## 8. Documentation, Configuration, And Operations Impact

- **Architecture/domain/privacy:** ADR-013, `ARCHITECTURE.md`, `DOMAIN.md`, `SECURITY-MODEL.md`, Event Location Privacy/Home Discovery evidence.
- **API/contracts:** `API.md`, `API_CHANGELOG.md`, `API_CONTRACT_INVENTORY.md`, `schemas/openapi_islamu-event.json`, generated Blazor client.
- **Provider/config/secrets:** `CONFIGURATION.md`, `SECRETS.md`, `.env.example`; healthy `Geocoding:Provider=None` and `Maps:Provider=None`, current `Photon` opt-in, endpoint, culture/country bias, `Geocoding:Photon:TimeoutSeconds=5`, `Geocoding:Photon:MaxRetries=2`, `Geocoding:Photon:RetryBackoffMilliseconds=[200,500]`, result limit, token lifetime, future Google pairing rule, provider attribution/retention profiles, `Database__Capabilities__Postgis=false`, Compose mapping `DATABASE_CAPABILITIES_POSTGIS`, discovery mode, and production validation. No raw connection strings.
- **Governance/licensing:** `DOMAIN.md`, `AUTHORIZATION.md`, `CONFIGURATION.md`, `SELF_HOSTING.md`, and license/credits UI describe the instance/tenant/organization address policy, server actor authorization, local visibility/promotion lifecycle, never-upstream rule, GeoNames CC BY attribution/change notice, OSM ODbL attribution/database separation, and Google content display/storage constraints.
- **Deployment:** `SELF_HOSTING.md`, `OPERATIONS.md`, AppHost/Compose profiles, plain PostgreSQL default, opt-in pinned PostGIS image or managed-service activation steps, separate optional migration history, health/readiness, Photon regional/planet data lifecycle, local development profile, and PostGIS backup/rollback.
- **UI/maps:** `BLAZOR.md`, `DESIGN_SYSTEM.md`, `ACCESSIBILITY.md`; combobox source/visibility badges, accessible policy/provider warnings, no-JS/no-WebGL fallback, RTL, CSS isolation, HAL gating, complete attribution, and the selected tile source's license/privacy/performance/cost decision record.
- **Testing:** `TESTING.md` plus the exact commands in each phase.
- **Package locks:** regenerate every affected committed `packages.lock.json` after package changes.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- Address queries, normalized results, exact coordinates, protected tokens, and user origins are PII. They use authenticated POST bodies and `private, no-store` responses and are excluded from logs/traces/metrics/cache keys/errors.
- Provider credentials and Photon internal URLs remain server-side. Browser-safe map tokens, if any, require origin/domain restrictions and separate secret classification.
- Tenant comes only from `ITenantContext`; all provider/spatial operations re-check tenant ownership and fail closed.
- Location/discovery writes use server-side authorization and concurrency. Manual creation additionally requires resolved hierarchical policy; a missing organization or unresolved setting cannot widen access. Blazor uses HAL links only.
- Local exact suggestions are filtered in the database by tenant plus tenant-approved/current-organization/current-creator scope before projection. Source badges are not authorization evidence.
- Local manual addresses are application-owned PII and never enter provider requests, feedback endpoints, imports, exports, or provider datasets.
- Autocomplete has minimum/maximum length, bounded results, debounce/cancellation, per-user/tenant/IP rate limits, upstream timeouts, and circuit/outcome metrics. It must not become an open proxy.
- Data Protection tokens are purpose/version/provider/config-bound and short-lived. They are not persisted or accepted across unrelated operations.
- Discovery approval is explicit. Coordinate change, revocation, or privacy erasure invalidates it transactionally.
- Nearby origin is rounded client-side, used once, and never stored. No analytics, screenshots, support dump, or error payload may capture it.
- Martin remains deferred because application-schema auto-publication bypasses these controls.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Requirement |
|---|---|---|
| Multi-tenancy | Applicable | Tenant context owns every request; local suggestions apply tenant and visibility predicates before PII projection; spatial rows/queries have tenant keys/filters; cross-tenant and cross-organization tests are mandatory. |
| Federation | Applicable | Geocoding provenance, PII, protected tokens, origins, and discovery points never enter AT Protocol/publication snapshots. Only already-approved safe event metadata may federate. |
| Localization | Applicable | Provider requests use validated culture/country bias; displayed provider labels are not treated as localized canonical values; UI/error strings use localization infrastructure. |
| RTL | Applicable | Combobox and map use logical CSS, correct key behavior, and no hard-coded physical layout assumptions. |
| Accessibility | Applicable | WAI-ARIA combobox/listbox, live status, keyboard operation, focus restoration, reduced motion, and complete non-map equivalents are acceptance requirements. |
| White-label/product | Applicable | Mandatory provider/data attribution cannot be hidden by white-labeling. Product wording distinguishes provider/local source, private/tenant-approved visibility, area-only, and exact nearby mode. |
| SEO | Not applicable to private autocomplete; applicable to public Home | Exact nearby POST results are not indexable/cacheable; base Home SEO remains area-safe. |

## 11. Observability And Operations

- Metrics: request count, outcome, latency histogram, cancellation, rate limit, retry, provider readiness, spatial readiness, and bounded query latency/cardinality. No high-cardinality tenant/location/session labels.
- Logs: structured event IDs and provider/outcome category only; redact request URI queries, bodies, upstream payloads, tokens, addresses, origins, coordinates, and connection strings.
- Traces: suppress/sanitize sensitive HTTP/database parameters and never attach address/origin tags.
- Health: geocoding disabled/configured/reachable categories; map disabled/configured compatibility; PostGIS declared/disabled, extension activation, optional migration/table/index/bounded-query/mode consistency. Disabled capabilities are healthy and do not probe external services or the database for optional features. Health payloads remain non-sensitive.
- Recovery: scoped local reuse and policy-authorized manual entry for geocoder outages; `area_only` for PostGIS outages; complete no-map UI; documented Photon rebuild/swap and PostgreSQL backup/restore. No silent semantic or provider fallback.

## 12. Migration And Compatibility Plan

1. Phases 1–2 are schema-free. They remove `TenantId`, raw latitude, and raw longitude from location writes and establish the provider/local policy contracts without compatibility shims.
2. Phase 3 adds source, visibility, and owning-organization state through generated migrations for PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. Correct entity/configuration state first, then delete/regenerate only unapplied development migrations; never hand-edit artifacts.
3. Because this is pre-v1 development and compatibility is explicitly out of scope, reset/regenerate the unapplied development migration baseline and development databases rather than inventing source, approval, creator, or organization provenance for legacy rows. Do not ship a heuristic backfill that could widen address visibility.
4. Before Phase 7, reconcile Event Location Privacy's current migration baseline (`ELP-230C`) even though the spatial history is separate, because lifecycle changes still touch the primary transaction path.
5. Keep the provider-neutral primary model free of spatial/provider-specific database types. Generate optional PostGIS migration only from `PostgisDiscoveryDbContext`, with its own snapshot and `__EFPostgisDiscoveryMigrationsHistory`.
6. Default deployments retain `Database__Capabilities__Postgis=false`, apply no optional spatial migration, and require no database/image change. All five primary providers stay supported for local address governance and database-neutral features.
7. After explicit activation approval, the Phase 7 owner completes Task 7.1 by changing ADR-013 to `Accepted` with actual date and named decider/role; no spatial code begins while Proposed.
8. An opting-in PostgreSQL operator activates PostGIS, backs up, sets the capability, and runs the optional migrator. Deploy readiness, isolated migration, projection approval, then nearby API/UI. No discovery-point backfill occurs.
9. Rollback uses `Discovery__Mode=area_only` and `Database__Capabilities__Postgis=false`; optional schema removal/restoration uses only the dedicated migration/backup procedure.
10. No compatibility aliases, dual write shapes, legacy coordinate inputs, or alternate proximity engines are added.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---:|---:|---|---|---|
| Optional PostGIS leaks into the primary model/migration chain. | Medium | Critical | Dedicated context/history, capability-off tests, unchanged primary snapshots for all providers, architecture guard. | Primary spatial diff or optional table/history created while flag false. | 1.1, 7.2–7.3 |
| Operator declares PostGIS but managed PostgreSQL lacks binaries, activation, or privilege. | Medium | High | Explicit flag, validation, preflight, documented activation, area-only recovery. | Extension/readiness or migration failure category. | 7.2–7.3, 7.5 |
| Opt-in local PostGIS image/volume change damages data. | Low | Critical | Plain PostgreSQL remains default; pin opt-in image, backup/restore rehearsal, distinct dev reset path. | Failed preflight/volume version check. | 7.2, 7.5 |
| Address/coordinate PII leaks through logs, URLs, caches, tokens, local suggestions, or telemetry. | Medium | Critical | POST/no-store, SQL visibility predicates, redaction tests, protected tokens, bounded labels. | Security/isolation tests or telemetry scan. | 2.2, 3.2, 4.2, 5.1, 8.2 |
| Custom locations pollute another organization/tenant's autocomplete. | Medium | Critical | Tenant + visibility predicates before PII projection, conservative initial scope, promotion authorization. | Cross-tenant/org/creator persistence test failure. | 2.2–3.2 |
| Policy or UI grants manual creation to an unauthorized actor. | Medium | Critical | Lockable settings + named server authorization + HAL; no user-scope grant or local role logic. | Missing-relation/denial/API test failure. | 2.1–2.3, 5.2, 6.3 |
| Local custom data is upstreamed or license-contaminates an OSM/provider database. | Low | Critical | No upstream contract/path, separate application tables, architecture/data-flow tests and operator docs. | Provider request contains local source or export/import job targets provider dataset. | 2.2–3.2 |
| Photon endpoint is under-sized or public-demo dependent. | High | High | Benchmark, opt-in heavy profile, production validation, local/manual fallback. | Readiness/latency/error rate. | 4.1–4.3 |
| Stale coordinates survive a manual address edit. | Medium | High | Aggregate methods clear pair and revoke discovery; regression tests. | Domain/Application test failure. | 1.2, 7.5 |
| Cross-context lifecycle writes split across connections or transaction owners. | Medium | Critical | Primary UoW owns transaction; spatial context shares it via `UseTransactionAsync`; integration tests. | Different transaction identity or partial commit. | 7.5 |
| Exact discovery exposes private or cross-tenant venues. | Medium | Critical | Explicit approval, tenant filters, privacy-erasure transaction, no point response. | Persistence/API isolation tests. | 7.3–8.2 |
| Query scans or produces unstable pagination. | Medium | High | GiST/relational indexes, database predicates, stable ordering, EXPLAIN evidence. | Query plan/latency/cardinality metrics. | 8.1 |
| Map wrapper or basemap is immature/non-compliant. | High | Medium | Decision gate, one integration, list-first fallback, no public OSM/demo default. | Compatibility/bundle/provider review. | 9.1 |
| Google terms are implemented from an oversimplified matrix or impermissible persistence. | High if activated | Critical | Keep deferred; enforce GoogleMaps-or-None, adjacent no-map attribution, field-level retention/legal/EEA/session review. | Invalid pairing accepted, Google call made while invalid, or unapproved content stored. | Deferred Google gate |
| Martin auto-publishes exact/application data. | High if adopted naively | Critical | Defer; explicit allowlist/read-only/coarse source/auto_publish false/new approval. | Config architecture test/source inventory. | Deferred |

## 14. Success Metrics And Definition Of Done

- Authorized editors can search visible local results with no provider, optionally search/select Photon results, and use keyboard/screen reader throughout. Manual entry during provider absence/failure exists only when effective policy plus authorization allow it.
- `None/None` is healthy. Provider and map choices are independent except that future Google Places may pair only with Google Maps or no map; invalid pairings fail before any Google request and show an accessible admin warning.
- New local locations start creator-private or organization-scoped, tenant promotion is explicit/HAL-gated, and cross-tenant/cross-organization/private exact addresses never appear.
- Local custom addresses stay solely in application-owned storage and are never added to an upstream provider dataset.
- No location write accepts body tenant ID or raw coordinate fields; no stale/partial coordinate pair can exist.
- Provider credentials, addresses, tokens, origins, and exact coordinates are absent from URLs, shared caches, logs, traces, metrics, public DTOs, federation, and public tile sources.
- Photon is production-validated, bounded, resilient, and observable; its public demo is not a fallback.
- If ADR-013 is activated, its `Accepted` metadata names the actual date and decider/role, and explicit PostgreSQL/PostGIS readiness, isolated migration, geography/indexes, approval/revocation/erasure, occurrence query, and stable ordering are proven against real PostGIS.
- PostgreSQL with the default false capability and every non-PostgreSQL deployment remain fully functional for database-neutral features, honest area-only/disabled for exact proximity, and never apply or query optional spatial schema.
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

Provider optionality, the Google map-pairing rule, local-address governance, PostGIS optionality, and the RC-1 transaction mechanism are resolved in design. Residual risks are correct database predicates for creator/organization/tenant-approved local visibility, provider-specific retention/attribution, and shared-transaction behavior under rollback/retry/cancellation. Operationally, self-hosted Photon and a production map source retain capacity, data-update, licensing, privacy, and cost obligations. Do not begin dependencies from demo endpoints or activate a deferred provider merely by adding its name to configuration.

### Deferred Provider And Infrastructure Work

| Deferred item | Why it is not in the initial implementation | Trigger to add a scoped phase |
|---|---|---|
| Google Places with optional Google Maps | Storage/retention, EEA, billing/session termination, field masks, branding, and implementation cost still need approval. Pairing semantics are resolved: Google Maps or no map only; no-map results require adjacent Google attribution. | Approved legal memo/budget/field-level retention contract. The scoped phase must implement Places, optional Google Maps, server/API and admin-form matrix validation, warnings, `.env.example`, and tests atomically. |
| Pelias | Large deployment/data operations and no measured Photon coverage failure. | Regional benchmark proves Photon insufficient and operations accepts Pelias capacity/update burden. |
| Native GeoNames/SQLite FTS5 | Separate importer, refresh, search-quality, and storage workstream. CC BY 4.0 permits commercial adaptation without application-code copyleft, but public/redistributed use must credit GeoNames, link the license, and indicate modifications. | Documented air-gapped/minimal requirement with accepted coverage limits, result/credits attribution placement, change notice, redistribution metadata, and compliance tests. |
| LeafletForBlazor | No second renderer need and current package maturity is volatile. | Measured WebGL/MapLibre incompatibility on supported clients. |
| Martin | No approved safe tile dataset; exact discovery points cannot be public tiles. | Approved coarse/aggregate tile product contract and tenant/cache policy; then explicit allowlist, read-only role, `auto_publish: false`, pinned image, same-origin route. |
| Generic map-provider/component hierarchy | One map implementation does not justify it. | A second approved renderer produces concrete shared behavior worth extracting. |
