<!-- ABOUTME: Repository-grounded plan for private, governed address acquisition. -->
<!-- ABOUTME: Defines Clean Architecture boundaries, test-first phases, provider optionality, and spatial-discovery handoff. -->

# Address Geocoding And Spatial Discovery - Implementation Plan

Last Updated: 2026-08-25 Europe/Brussels

## 0. Planning Metadata

- **Task directory:** `dev/active/address-geocoding-and-spatial-discovery/`
- **Planning status:** Re-baselined for user review; implementation has not started.
- **Senior CTO decision:** **Split before approval.** This ledger now owns address integrity and acquisition only. Exact PostGIS discovery stays under the existing `dev/active/home-discovery-experience/` Phase 6 authority. Maps require a separate future workstream.
- **Delivery shape:** Four phases and three PRs:
  - PR A: location integrity plus governed local-address persistence.
  - PR B: local-only API/HAL/BFF/Blazor vertical slice.
  - PR C: optional Photon adapter and protected provider selections.
- **Matched intents:** `add-write-endpoint`, `add-cqrs-handler`, `add-ef-migration`, `update-repository-query`, `openapi-contract-change`, `blazor-component-affordance`, and `external-infrastructure-bootstrap`.
- **Criticality:** Tier 2 Privacy with Tier 1 Security/Migration boundaries.
- **Primary layers:** Domain, Application, Persistence, Infrastructure, API, generated OpenAPI client, Blazor Client, tests, configuration, operations, and canonical documentation.
- **Complexity:** XL overall; each PR boundary is independently reviewable.
- **I-VSD document:** [I-VSD Address Geocoding And Spatial Discovery](../../../islamic-value-sensitive-design/i-vsd-address-geocoding-and-spatial-discovery.md)
- **Compatibility posture:** Pre-v1 development mode. Remove obsolete request members and generated client shapes directly. Add no aliases, dual contracts, coordinate shims, or compatibility endpoints. Data integrity and operator recovery remain mandatory.
- **Research boundary:** External evidence was reduced to source-free functional constraints. No third-party implementation source, code, SQL, migrations, tests, comments, documentation prose, or assets are implementation inputs.
- **Context7 status:** Requested and attempted on 2026-08-25, but no Context7 MCP tool was available. No Context7 result is claimed. Official Npgsql, Microsoft, PostGIS, and Photon documentation is the recorded substitute.

### 0.1 Composite Contribution Contract

| Concern | Required planning decision |
|---|---|
| Must-read docs | `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/AUTHORIZATION.md`, `docs/SECURITY-MODEL.md`, `docs/MULTI_TENANCY.md`, `docs/CONFIGURATION.md`, `docs/SELF_HOSTING.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/BLAZOR.md`, `docs/ACCESSIBILITY.md`, ADR-013, this workstream, and the Home Discovery Phase 6 state before any spatial change. |
| Skills/rules | `criticality-guardrail`, `i-vsd`, `grill-me`, `ip-clean-room`, `clean-architecture-rules`, `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`, `auth-patterns`, `blazor-bff-patterns`, `blazor-ui-conventions`, `accessibility`, `error-tracking`, and matching path rules. |
| Scope | Every coordinate-bearing write path; location aggregate invariants; provider-neutral address governance and local search; private API/HAL/BFF/UI; one optional Photon adapter; tests/docs/configuration. |
| Verification | One Release build and at most one full project test per phase; targeted TUnit Red/Green slices at the owning seam. |
| Forbidden | Hand-edited generated artifacts; browser-owned tenant/actor authority; UI role checks; raw address/origin/token in telemetry; public demo endpoint as production dependency; provider types in Application; external HTTP inside DB transactions; exact-discovery implementation in this ledger. |

## 1. Executive Summary

Repair the existing location write boundary before adding a provider. `Location` will expose explicit atomic address transitions. Manual changes clear stale coordinates. Geocoded changes require a finite both-or-none pair. Raw latitude/longitude inputs are deleted from every write path, not only Location CRUD: direct location DTOs, nested event creation, and AI event-draft creation all move to governed manual address data or an opaque protected provider selection.

The existing tenant model is already server-authoritative: create DTOs have no tenant field, `LocationController` supplies `ITenantContext`, and handlers persist the context tenant. The implementation must remove or cross-check the remaining in-process `CreateLocationCommand.TenantId` authorization fact so a mismatched internal caller fails closed.

Address acquisition first ships without any external provider. Application owns provider-neutral use-case contracts and hierarchical policy decisions. Persistence owns a bounded local-address query that applies tenant and visibility predicates before exact PII projection. API exposes private POST operations and server-authored HAL affordances through the existing YARP BFF. Blazor consumes only generated contracts and HAL, provides an accessible local-address combobox/manual form, and preserves the separate private-home consent action.

The optional Photon phase plugs into the already-shipped local-only vertical slice. Infrastructure owns concrete provider selection, endpoint configuration, HTTP resilience, Photon transport models, and ASP.NET Core time-limited Data Protection. Application sees only semantic availability and normalized address models, never `Photon`, HTTP, or provider configuration enums. Suggestion responses carry display fields, attribution, and an opaque token; raw provider coordinates never reach the browser.

Exact spatial discovery is not implemented here. Home Discovery already owns the area-only product and ADR-013 activation gate. This plan contributes a decision-complete handoff: discovery eligibility must be `EventLocation`/occurrence-scoped through the existing disclosure authority, not location-wide; installed-but-disabled spatial storage must continue lifecycle cleanup; and the capability state machine must distinguish `Absent`, `InstalledDisabled`, and `Serving`.

### Explicit Non-Goals

- No PostGIS code, package, context, migration, query, endpoint, or positive runtime test in this ledger.
- No map implementation.
- No Google, Pelias, GeoNames, Martin, or second geocoder adapter.
- No generic provider/map/spatial abstraction.
- No direct browser provider request.
- No requirement for a provider, map, PostgreSQL, or PostGIS.
- No raw coordinate inputs after PR A.
- No exact points in generic/public/federated/AI/tile contracts.
- No automatic location or discovery publication.
- No compatibility aliases or dual request shapes.

## 2. Source-Grounded Current State Report

### 2.0 Pre-Flight Structural Context

Code-review graph tools were not exposed in this session. A read-only repository scout plus focused source/test reads established this bounded slice. Rerun graph impact and affected-flow queries before implementation if the tools are available.

```yaml
Target: Location address creation and mutation
Callers:
  - LocationController.Create/Patch
  - CreateEventCommandHandler nested location creation
  - CreateEventDraftAiActionMapper
  - TenantLookupTablesSection and location dialogs
Callees:
  - CreateLocationCommandHandler / UpdateLocationCommandHandler
  - LocationRepository
  - Location / LocationPii
Impacted flows:
  - Tenant location administration
  - Event creation
  - AI-assisted event draft creation
  - Private-home consent and erasure
  - Generated OpenAPI/BFF/Blazor consumers
Tests:
  - Event.Application.UnitTests
  - Event.Persistence.IntegrationTests
  - Event.API.IntegrationTests
  - Explore.Blazor.Client.Tests
  - Explore.Blazor.IntegrationTests
  - Event.Architecture.Tests
```

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Consequence |
|---|---|---:|---|
| Exact address and scalar coordinates live in a dedicated 1:1 PII model. | `Location.cs`; `LocationPii.cs` | High | Keep exact address ownership and irreversible erasure intact. |
| Browser bodies do not carry tenant identity. | `CreateLocationDto.cs`; `UpdateLocationDto.cs`; `LocationController.Create` | High | Correct the stale plan claim; test missing/mismatched internal tenant facts instead. |
| Location CRUD still accepts raw coordinates. | Location create/update DTOs, validators, handlers and dialogs | High | Remove directly and regenerate contracts. |
| Nested event creation also accepts and persists raw coordinates. | `DTOs/Event/CreateEventLocationDto.cs`; `CreateEventCommandHandler.CreateLocationsAsync` | High | PR A must contract all write paths, not only LocationController. |
| AI event-draft creation maps model-supplied coordinates into nested event locations. | `Features/AiAssistant/Actions/CreateEventDraftAiActionPayload.cs`; `CreateEventDraftAiActionMapper.cs` | High | AI draft contracts must use governed manual data or protected selection. |
| Authorized output/disclosure contracts also contain coordinates. | EventLocation disclosure DTOs/services, MCP/federation views | High | Do not delete authorized read fields blindly; contract only untrusted writes. |
| Create behavior relies on uncharacterized flattened AutoMapper mapping. | `CreateLocationCommandHandler.cs`; `LookupMappingProfile.cs`; PII proxy setters | High | Lock current failure/behavior with a real construction test, then replace with explicit aggregate construction. |
| Update applies address and coordinates independently. | `UpdateLocationCommandHandler.cs` | High | Aggregate transition must clear or atomically replace the pair. |
| Private-home consent and anti-resurrection behavior are shipped. | `Location.cs`; `LocationController.cs`; privacy tests | High | Preserve classify/transfer consent and erasure semantics. |
| Location admin mutations are rendered unconditionally. | `TenantLookupTablesSection.razor` | High | Convert to HAL-gated affordances. |
| Existing BFF proxies `/api/*`, strips/enriches trusted context, and antiforgery-protects unsafe methods. | `EventApiProxyExtensions.cs` | High | Reuse and prove this route; add no bespoke BFF endpoint. |
| Home Discovery remains active and area-only. | `dev/active/home-discovery-experience/`; PostGIS absence architecture tests | High | It remains the only spatial execution ledger. |
| ADR-013 remains Proposed. | `docs/adr/ADR-013-postgis-proximity-discovery.md` | High | No agent may self-activate spatial work. |
| Current five-provider migration heads are newer than the location contraction. | As of 2026-08-25 all end at `AddAdmissionIssuancePersistence` | High | Discover the current head immediately before generating a new migration. |
| Current package graph is EF Core 10.0.10/Npgsql 10.0.3; NTS plugin is absent. | `Directory.Packages.props`; Persistence project | High | Spatial dependency selection belongs to Home Discovery after approval. |
| Npgsql NTS defaults to geometry unless geography is explicit. | https://www.npgsql.org/efcore/mapping/nts.html | High | Spatial handoff requires constrained geography mapping. |
| Cross-context EF transactions require the same connection and transaction. | https://learn.microsoft.com/en-us/ef/core/saving/transactions | High | Spatial handoff retains one UoW owner. |
| Data Protection supports time-limited, purpose-isolated payloads. | Microsoft `ITimeLimitedDataProtector` docs | High | Provider selections are short-lived and reject mismatches. |
| One resilience handler should own HTTP resilience. | Microsoft `Microsoft.Extensions.Http.Resilience` docs | High | Do not stack handlers or hedge address queries. |
| Public Photon provides no availability guarantee and may throttle extensive usage. | https://photon.komoot.io/ | High | Production activation requires an operator-owned/contracted endpoint. |
| Context7 could not be queried. | Tool/librarian attempt on 2026-08-25 | High | Record official-doc substitution; retry before dependency adoption if available. |

### 2.2 Current Layer Behavior

#### Domain/Application

- `Location` owns tenant, privacy state, private-home ownership, audit and concurrency.
- `LocationPii` owns address/postcode/nullable coordinates but does not enforce a complete finite pair.
- Create validates, AutoMaps, overwrites tenant from context and persists.
- Update validates, checks concurrency and mutates address/coordinates independently.
- Nested event and AI draft flows construct `LocationPii` directly from untrusted/generated coordinate fields.
- The hierarchical settings system already supports instance/tenant/organization/group/user resolution and locks. User scope must not grant address-creation authority.

#### Persistence

- Repositories return entities and own PII loading/deletion.
- PostgreSQL, SQLite, SQL Server, MariaDB and MySQL use separate generated migration chains.
- Real relational execution evidence is strongest for PostgreSQL and SQLite. Model/snapshot generation for other providers is not proof of query translation.

#### API/BFF/Blazor

- Location CRUD is authenticated; exact management reads are private/no-store.
- Private-home classification/ownership uses explicit consent and `If-Match`.
- Existing YARP `/api/*` forwarding is sufficient.
- Create/edit dialogs expose raw coordinates; Edit also hosts the private-home consent action.
- Location actions are not consistently HAL-gated.

#### Spatial/Product

- Home Discovery is area-only and active.
- Browser origin is currently reduced to coarse areas and is not sent to the server.
- Architecture tests enforce absence of PostGIS/NTS/exact runtime.
- Existing disclosure authority is per `EventLocation` and purpose; a location-wide public point would bypass it.

### 2.3 Current Gaps

1. Raw coordinate inputs exist in Location, Event and AI draft write contracts.
2. Independent mutation permits stale/partial coordinates.
3. Create mapping is unproven and structurally fragile.
4. Internal tenant facts can disagree with context unless explicitly checked.
5. No conservative source/visibility state or SQL-first local reuse query exists.
6. Existing rows cannot be truthfully assigned provider provenance.
7. Private homes need an explicit never-tenant-approved reuse invariant.
8. No local-only address acquisition API/UI exists.
9. No PII-safe optional provider adapter or protected selection exists.
10. The former plan duplicated tasks inside `plan.md`, sequenced tests post hoc, mixed three products, omitted I-VSD, and held stale migration/tenancy facts.

## 3. Proposed Future State

### 3.1 Location Integrity

All location creation routes call the same Application-owned address transition:

- **Manual address:** normalized address fields, no coordinate pair, source `Manual`, conservative visibility.
- **Provider selection:** opaque protected token unprotected by the server before the transaction; complete normalized address plus finite coordinate pair; opaque provider provenance.
- **Existing local reuse:** select an already-authorized Location reference; do not clone exact PII by default.

`Location` exposes explicit methods rather than public independent coordinate mutation. Manual changes clear coordinates and any provider selection state. Existing authorized read/disclosure DTOs keep coordinates where the shipped disclosure authority permits them.

### 3.2 Governed Local Address Acquisition

1. Resolve tenant and actor only from trusted context.
2. Resolve effective address policy from the existing hierarchy plus named authorization.
3. Query eligible local addresses using tenant plus visibility predicates before exact PII projection.
4. Return bounded private/no-store local suggestions.
5. Manual creation is allowed only when policy and authorization both allow it.
6. Promotion changes visibility only, never source.
7. Private Homes are never tenant-wide autocomplete rows. Owner/request-specific reuse still passes the existing EventLocation management/disclosure authority.
8. Legacy rows migrate to `UnknownLegacy` source and `Quarantined` visibility. They are not reusable until explicit moderation supplies truthful scope.

Effective policy:

| Mode | Required authority | Initial visibility |
|---|---|---|
| `Disabled` | None; creation denied | Not applicable |
| `AdminOnly` | Tenant address-management authorization | Creator-private unless the same authorized operation explicitly approves |
| `OrganizationGoverned` | Organization context, organization grant and named authorization | Organization-scoped |
| `OpenWithModeration` | Existing location-create authorization; settings alone never grant | Creator-private or organization-scoped |

Missing/invalid policy fails as `Disabled`; user scope cannot loosen it.

### 3.3 Optional Photon Acquisition

1. `Geocoding:Provider=None` is healthy and the local-only product remains complete.
2. Infrastructure/host composition selects `None` or Photon. Application does not contain a concrete provider enum or endpoint options.
3. Photon receives only the minimal authorized query. Local application rows are never sent upstream.
4. Infrastructure normalizes the provider response into Application semantics.
5. API returns display fields, attribution, source and protected token only; no raw provider coordinates.
6. Token binding includes tenant, actor, organization scope where applicable, command purpose, target Location and concurrency stamp for updates, provider profile/config fingerprint, issued time and expiry.
7. Every mismatch is rejected before persistence and before a database transaction.
8. Provider failure keeps typed input, local suggestions and authorized manual entry available.

### 3.4 Spatial Discovery Handoff

Home Discovery Phase 6 is the sole implementation ledger and must absorb these invariants before ADR acceptance:

- Eligibility is per `EventLocation`/eligible occurrence and must pass the existing disclosure authority for the public-discovery purpose. A location-wide approval is insufficient.
- Exact point storage is a purpose-limited projection, never generic Location state.
- Capability state is:
  - `Absent`: no schema/package/runtime; no dormant rows.
  - `InstalledDisabled`: serving is disabled, but transactional correction/erasure cleanup remains active.
  - `Serving`: readiness is green and exact queries/HAL are available.
- Transition from `InstalledDisabled` to `Absent` requires verified projection cleanup and dedicated schema removal.
- Startup fails closed if dormant rows exist while lifecycle cleanup is not registered.
- Exact origin remains transient private POST data; no point/origin enters public output or telemetry.
- `ST_DWithin` filters indexed geography before distance ordering; no in-memory/client fallback.

## 4. Non-Negotiable Constraints

1. Domain/Application have no EF/Npgsql/NTS/HTTP/provider-configuration dependencies.
2. Repositories return entities; bounded query ports return Application-owned result models, not Persistence rows or `IQueryable`.
3. Validators are manually instantiated and cancellation flows end to end.
4. Tenant/actor facts come from trusted context and mismatches fail closed.
5. HAL is UI affordance authority; Blazor never checks roles/claims for resource actions.
6. Address acquisition uses authenticated private POST and no-store.
7. Address queries, coordinates, tokens, provider payloads, secrets, tenant/location IDs and URIs never reach logs/traces/metric labels/cache keys/errors/health data.
8. `Provider=None` is healthy and registers no outbound provider.
9. Application-owned manual/local data never enters provider requests, feedback, imports, exports or datasets.
10. Private Homes cannot be promoted to tenant-wide autocomplete.
11. Migrations/snapshots/OpenAPI/client are generated, never edited.
12. All write-contract breaks are direct; no compatibility readers or aliases.
13. Every new file starts with two `ABOUTME:` lines.
14. This ledger cannot activate ADR-013 or implement spatial runtime.

## 5. Architecture And Design Decisions

### Decision 1: Contract every coordinate-bearing write path

- Remove raw coordinates from Location CRUD, nested event creation and AI event-draft creation in one contract change.
- Preserve authorized coordinate reads governed by existing disclosure services.
- Add architecture tests that prevent a new untrusted or generated write DTO from reintroducing coordinate authority.

### Decision 2: Explicit aggregate address transitions

- Replace independent proxy setters as the write API with manual/provider methods.
- Manual transition clears coordinates/provenance.
- Provider transition requires a finite complete pair.
- Erasure remains irreversible and authoritative.

### Decision 3: Source and visibility are independent

- Source: `UnknownLegacy`, `Manual`, or opaque provider-selected.
- Visibility: `Quarantined`, `CreatorPrivate`, `OrganizationScoped`, `TenantApproved`.
- Legacy data is quarantined, not guessed or widened.
- Private Home cannot become `TenantApproved`.

### Decision 4: Local-only ships before Photon

- The first complete vertical slice uses local suggestions and governed manual entry.
- Provider selection is optional Infrastructure composition added later.
- No address API/UI depends on Photon readiness.

### Decision 5: Protected selections are least-privilege capabilities

- `ITimeLimitedDataProtector` token holds normalized provider data and exact coordinates.
- Binding includes tenant/actor/scope/purpose/target/concurrency/provider/config/time.
- API DTO does not expose coordinates.
- Token unprotect/validation occurs outside the DB transaction.

### Decision 6: Existing BFF is reused and proven

- No bespoke BFF endpoint/service.
- Targeted BFF integration proves antiforgery, browser privileged-header stripping, token/tenant enrichment and `/api/*` forwarding.

### Decision 7: One provider, one resilience pipeline

- Infrastructure uses one `Microsoft.Extensions.Http.Resilience` handler; no stacking or hedging.
- Five-second total budget; at most two transient retries with 200/500 ms backoff, bounded jitter and bounded `Retry-After`.
- Retry only transport failures, 408, 429 and 5xx when another attempt fits.
- Cancellation stops immediately; tests use exact signals and a bounded timeout, never sleeps.

### Decision 8: Concrete provider policy stays outside Application

- Application models semantic source/attribution/persistence-profile data.
- Host/Infrastructure own `None|Photon`, endpoint, demo rejection, resilience, readiness and adapter registration.
- A future second provider must justify abstraction from concrete shared behavior.

## 6. Implementation Phases

### Phase 1: Location Integrity And Complete Write-Contract Contraction

- **Goal:** Remove browser/model coordinate authority across Location, Event and AI draft writes and make address transitions atomic.
- **Depends on:** User approval of PR A.
- **Relevant files:** Location aggregate/PII; Location/Event/AI write DTOs, validators, mappers and handlers; current API/generated client/dialogs; matching Application/API/architecture tests.
- **Test-first anchor:** Red tests name every coordinate-bearing write member, prove partial/non-finite/stale behavior, real create construction, tenant mismatch, private-home consent/erasure, and AI/nested-event contraction before production edits.
- **Exit criteria:** All writes use manual data or protected selection; no raw coordinate write member survives; explicit aggregate construction replaces fragile AutoMapper create mapping; manual transitions clear coordinates; internal tenant mismatch fails closed; authorized disclosure reads remain unchanged.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- **Rollback:** Forward-fix within PR A; never restore raw coordinate writes or weaken privacy tests.

### Phase 2: Governed Persistence And Local-Only Acquisition

- **Goal:** Add conservative source/visibility state, hierarchical policy, promotion and SQL-first local suggestions.
- **Depends on:** Phase 1.
- **Relevant files:** Location/lookup state; settings definitions; Application semantic contracts/handlers; Persistence configuration/query; generated five-provider migrations/snapshots; Application/Persistence tests.
- **Test-first anchor:** Red migration/isolation tests precede model/query code: `UnknownLegacy+Quarantined`, Private Home non-promotion, cross-tenant/creator/organization denial, missing scope, idempotent promotion, current migration heads and provider model parity.
- **Exit criteria:** Existing rows are quarantined; no heuristic provenance; only explicitly moderated rows become reusable; SQL filters before PII projection; PostgreSQL and SQLite execute real query tests; SQL Server/MariaDB/MySQL model/migration parity is claimed only as such unless real lanes are added.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback:** Fix model/configuration/seeding and regenerate only unapplied migrations. Never widen visibility or hand-edit artifacts.

### Phase 3: Local-Only API, HAL, BFF, Generated Client And Blazor

- **Goal:** Ship a complete provider-free address acquisition/admin experience through existing trust boundaries.
- **Depends on:** Phase 2.
- **Relevant files:** Application local search/policy requests and `Explore.Geocoding` metrics; capability-partitioned API controllers/routes/rate limit/HAL; generated OpenAPI/client; existing BFF tests; new accessible component/CSS; current dialogs/lookup section/services; canonical docs/tests.
- **Test-first anchor:** Red Application/API/BFF/Blazor tests cover pre-Persistence validation, tenant/organization rejection, authorized result shaping, `provider=none` instrument/label allowlists, 401/403, tenant spoofing, short/oversized input, no-store, rate limits, RFC 7807, HAL omission, Private Home isolation, provider absence, antiforgery, trusted header transforms, combobox keyboard/focus/live status, cancellation/latest-request-wins, RTL/localization and consent preservation.
- **Exit criteria:** Local suggestions/manual policy work with no provider; local-only metrics emit only bounded `provider=none` outcomes; captured telemetry proves no sensitive dimensions; controllers only dispatch/map/assemble; generated client is current; BFF route is reused; UI actions depend only on HAL; no raw coordinates reach write contracts; no-provider/no-map flow is complete.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback:** Disable new HAL/API relations and keep existing authorized manual Location behavior without coordinates; do not add a direct provider or local role check.

### Phase 4: Optional Photon Adapter, Protected Selections And Release Closure

- **Goal:** Add one production-capable optional provider without changing the local-only product.
- **Depends on:** Phase 3 plus approved endpoint ownership, data footprint, capacity, update/swap, support, recovery, terms and attribution evidence.
- **Relevant files:** Infrastructure provider/protector/options/composition; Application semantic provider port; API/UI provider result integration; `.env.example`; config/secrets/self-hosting/operations docs; Infrastructure/API/Blazor/architecture tests; clean-room record and release fragment.
- **Test-first anchor:** Red tests cover provider absent, success, malformed payload, cancellation, timeout, transient/permanent statuses, circuit, token binding/tamper/expiry/replay/target-concurrency mismatch, zero-PII telemetry, demo-endpoint rejection and no local-data upstream request.
- **Exit criteria:** `None` registers no client; Photon is opt-in; public demo is rejected in production; one bounded resilience pipeline is used; tokens are least-privilege and coordinate-opaque to browser; local-only behavior remains green; provenance/license evidence and Tier 2 change fragment are complete.
- **Phase-end verification:**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback:** Set provider to `None`; local/manual acquisition remains available and stored locations remain intact.

## 7. Test-First And Verification Strategy

Every behavioral task in `tasks.md` has:

1. a Red task naming exact public-contract assertions and targeted TUnit command;
2. a Green task implementing only enough to satisfy them;
3. the same targeted command passing without timing luck;
4. one phase-end Release build and one selected full project test.

Targeted iteration command:

```bash
dotnet run --project <owning-test-project>.csproj --no-build -- --treenode-filter "/*/*/*<TargetTestClass>/*"
```

High-leverage adversaries:

- partial/non-finite/stale coordinates across every write route;
- forged/missing tenant and organization scope;
- quarantined/Private Home/cross-tenant local result leakage;
- concurrent promotion/update and erasure anti-resurrection;
- API/BFF tenant/header/antiforgery bypass;
- token cross-tenant/cross-actor/replay/target/concurrency mismatch;
- provider cancellation/429/5xx/permanent 4xx/circuit;
- PII injection into captured logs/traces/metrics/ProblemDetails/health.

Async tests subscribe to the exact event/state before triggering work and await it with a bounded timeout. Fixed sleeps and retry-polling are forbidden.

## 8. Documentation, Configuration, Operations, And Release

### Documentation

- `ARCHITECTURE.md`, `DOMAIN.md`, `AUTHORIZATION.md`, `SECURITY-MODEL.md`, `MULTI_TENANCY.md`
- `API.md`, `API_CHANGELOG.md`, `API_CONTRACT_INVENTORY.md`, generated schema/client
- `CONFIGURATION.md`, `SECRETS.md`, `.env.example`
- `SELF_HOSTING.md`, `OPERATIONS.md`, `TROUBLESHOOTING.md`
- `BLAZOR.md`, `ACCESSIBILITY.md`, `TESTING.md`
- Clean-room source register, AFC/SSO decision, dependency/service/data license evidence

### Configuration

- `Geocoding:Provider=None|Photon` (`None` default; owned by host/Infrastructure).
- `Geocoding:Photon:BaseUri`.
- `Geocoding:Photon:TotalTimeoutSeconds=5`.
- `Geocoding:Photon:MaxRetries=2`.
- `Geocoding:Photon:RetryBackoffMilliseconds=200,500`.
- `Geocoding:SelectionLifetimeSeconds` with bounded documented range.
- Secrets originate only from Infisical or `.env`; `.env.example` documents keys only.

### Executable Observability Contract

Meter: `Explore.Geocoding`

| Instrument | Type/unit | Allowed labels |
|---|---|---|
| `geocoding.requests` | Counter `{request}` | `provider=none|photon`, bounded `outcome` |
| `geocoding.duration` | Histogram `ms` | `provider`, bounded `outcome` |
| `geocoding.retries` | Counter `{retry}` | `provider`, `reason=transport|408|429|5xx` |
| `geocoding.selection_rejections` | Counter `{rejection}` | bounded `reason` only |
| `geocoding.rate_limit_rejections` | Counter `{rejection}` | `policy` only |

No tenant, actor, organization, location, address, query, URI, coordinate, token, provider payload or endpoint labels. Provisional acceptance budget is p95 under 2.5 seconds and hard completion under the five-second total timeout at approved representative load; Phase 4 records measured evidence and alert/runbook ownership before production activation.

Readiness states are `DisabledHealthy`, `Configured`, `Degraded`, and `Misconfigured`. Disabled performs no network probe. Readiness exposes categories only; no endpoint or exception details.

### Release

This is Tier 2 due to breaking API, migration, privacy and operator impact.

- Final Phase 4 task reserves the next available `docs/releases/changes/CHG-2026-NNNN.yaml`.
- Validate it with repository release policy.
- Compose (but do not create without user authorization) a Conventional Commit with `Change-Id: CHG-2026-NNNN`.
- Include `BREAKING CHANGE:` for removed coordinate write members and migration/operator action.

## 9. I-VSD And Moral Boundaries

The linked [I-VSD report](../../../islamic-value-sensitive-design/i-vsd-address-geocoding-and-spatial-discovery.md) is a blocking artifact.

- **Amanah/Trust:** exact addresses, private homes and provider disclosures are stewardship obligations.
- **Non-Harm/Rights:** minimize precision, default visibility conservatively, preserve correction/erasure and prohibit resurrection.
- **Avoiding Spying:** no automatic origin, hidden provider disclosure, telemetry capture or cross-context tracking.
- **Truthfulness:** `None` and degraded states are honest; area-only never claims exact proximity.
- **Justice/Ihsan:** accessible keyboard/screen-reader/RTL operation and complete no-provider/no-map paths.
- **Promise-Keeping:** self-hosting, provider limits, attribution, recovery and optionality match actual behavior.

This is design reasoning, not a fatwa, Sharia certification, product certification or proof of ethical outcomes.

## 10. Security, Privacy, Tenancy, And Abuse

- Trusted tenant/actor facts are rechecked at handler/query boundaries.
- Application authorization and policy both gate manual creation/promotion.
- HAL expresses executable actions; UI checks links only.
- Private POST, no-store, bounded bodies/results and dedicated rate limiting protect address input.
- Local exact PII is filtered in SQL before mapping.
- Private Homes are owner/request-scoped and never tenant-approved autocomplete.
- Provider token is opaque, short-lived and least-privilege bound.
- Provider calls happen before transactions; no external side effect is dual-written.
- Address/query/token/provider body never reaches telemetry or health.
- `None` and provider failure do not disable local/manual capability.

## 11. Migration And Compatibility Plan

1. Phase 1 directly removes coordinate write members across Location, Event and AI contracts and regenerates all consumers.
2. Phase 2 discovers the then-current migration head for every provider immediately before generation.
3. Add provider-neutral source/visibility/organization state with generated migrations.
4. Existing rows become `UnknownLegacy` plus `Quarantined`; no heuristic provider/source/approval assignment and no tenant-wide reuse.
5. Explicit moderation may transition a quarantined row after truthful scope/provenance review.
6. PostgreSQL and SQLite run real local-query translation/isolation tests. Other providers claim generated model/migration parity only unless real runtime lanes are added.
7. Never rewrite merged/applied migrations, hand-edit snapshots, or use a compatibility reader.

## 12. Risk Register

| Risk | Impact | Mitigation | Detection/owner |
|---|---|---|---|
| Coordinate write survives in direct Location, nested Event or AI draft route | Critical | Complete structural inventory plus architecture ratchet | Phase 1 |
| Stale/partial coordinates | High | Explicit aggregate transitions | Application invariants |
| Internal tenant mismatch | Critical | Context equality/fail-closed handlers | Phase 1 Red tests |
| Legacy/private address becomes tenant-reusable | Critical | `Quarantined`; Private Home non-promotion | Phase 2 real DB tests |
| Cross-tenant/org/creator suggestion leak | Critical | SQL-first predicates | Phase 2 isolation tests |
| BFF/client trust bypass | Critical | Existing YARP/antiforgery plus targeted integration | Phase 3 |
| Token cross-scope replay | High | Tenant/actor/scope/target/concurrency binding | Phase 4 |
| PII telemetry leak | Critical | Bounded instruments and captured-sink tests | Every phase |
| Public Photon dependency/outage | High | `None` default; production topology gate | Phase 4 |
| Local data upstream/license contamination | Critical | No upstream local-data path; clean-room/license evidence | Phase 4 |
| Five-provider claim exceeds evidence | High | Narrow claim to model parity where no runtime lane | Phase 2 |
| Duplicate spatial authority | Critical | Home Discovery Phase 6 is sole ledger | Planning handoff |

## 13. Success Metrics And Definition Of Done

- No untrusted write contract accepts latitude/longitude.
- Authorized disclosure reads remain governed by the existing per-EventLocation authority.
- Manual address changes cannot retain stale coordinates.
- Existing rows are quarantined rather than guessed or widened.
- Cross-tenant, cross-organization, other-creator and Private Home suggestions are absent.
- Local-only API/BFF/UI works with no provider or map.
- HAL is the only client mutation-affordance source.
- Photon is optional, production-gated, bounded and coordinate-opaque to the browser.
- Token cross-tenant/actor/target/replay attempts fail before persistence.
- No address/query/coordinate/token/provider payload appears in telemetry, URL, cache, ProblemDetails or health.
- Exact spatial implementation remains absent here and is handed to one authoritative Home Discovery ledger.
- Every phase passes its selected build/test gate; docs/generated artifacts/I-VSD/release/provenance agree.

## 14. Implementation Agent Contract

1. On cold resume read context and current task first, then only the referenced plan heading.
2. Keep one task in progress and maintain `tasks.md` immediately.
3. Run each Red target first and prove the expected missing behavior before Green.
4. Never weaken, skip or delete an invariant to reach green.
5. Use graph/LSP for blast radius and symbols when available.
6. Generate migrations/snapshots/OpenAPI/client; never edit them.
7. Preserve unrelated shared-worktree changes.
8. Update context after phase/decision/blocker/failure/discovery/handoff; update plan only for strategy change.
9. Recheck external API/package details through Context7 if it becomes available; otherwise record official-doc substitution.
10. Run phase gates only after phase tasks; no browser/app/Aspire/Docker/manual runtime lane in this plan.
11. Do not implement PostGIS/maps/other providers from this ledger.
12. Do not create a git commit unless the user separately authorizes it.

## 15. Progress Reporting Contract

```text
Implemented: developer teaching summary
Verified: exact command/evidence
Remaining: incomplete or deferred work
Next: recommended task
Docs updated: tasks yes/no; context/plan updated or unchanged with trigger
```

## 16. Potential Risks And Unknowns

The highest risk is ordinary address privacy, not spatial math: a local-only autocomplete can still leak creator-private or Private Home addresses, trust an internal tenant mismatch, or put query text into telemetry. PR A and PR B must prove those boundaries before a provider is enabled.

The provider risk is operational and contractual. Photon is not approved until endpoint ownership, data footprint, capacity, update/swap, support, recovery, terms and attribution are evidenced. `Provider=None` is the release-safe default.

The spatial risk is ownership drift. Home Discovery Phase 6 must remain the sole ledger and replace location-wide approval with per-EventLocation/occurrence disclosure eligibility plus the `Absent`/`InstalledDisabled`/`Serving` lifecycle. This address workstream records that handoff but cannot activate it.

### Deferred Successor Work

| Item | Owner/trigger |
|---|---|
| Exact PostGIS discovery | `dev/active/home-discovery-experience/` Phase 6 after ADR-013 acceptance and updated disclosure/lifecycle design |
| Map experience | Separate workstream after tile/data license, privacy, accessibility, cost and bundle approval |
| Google Places/Maps | Separate legal/budget/retention/EEA/branding/session decision |
| Pelias | Measured Photon coverage failure plus operator capacity approval |
| GeoNames offline search | Approved air-gapped requirement and dataset attribution/update lifecycle |
| Martin/vector tiles | Separate coarse aggregate tile contract; never application-table auto-publication |
