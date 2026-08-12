<!-- ABOUTME: Resume context for the address geocoding and governed spatial discovery workstream. -->
<!-- ABOUTME: Captures current evidence, decisions, blockers, validation, and the next executable implementation slice. -->

# Address Geocoding And Spatial Discovery — Context

Last Updated: 2026-08-11 Europe/Brussels

## SESSION PROGRESS (2026-08-11 Europe/Brussels)

### ✅ COMPLETED

- Planning created from `dev/report/address_geocoding_analysis.md` and current repository reality.
- Contribution Contract, matched rules, required skills, canonical docs, current location/privacy/discovery flows, database provider composition, AppHost/Compose, existing tests, and overlapping workstreams were investigated.
- Code-review graph was used before filesystem fallback; current graph matched repository HEAD.
- Baseline Release build passed: 37 projects, 0 errors, 0 warnings.
- Tavily primary-source extraction verified Npgsql spatial mapping, Google Places policy/session constraints, Martin publication/configuration behavior, and OSM tile policy.
- Context7 availability was checked; no Context7 connector/tool/resource/template exists in this session, so no Context7 evidence is claimed.
- Plan and 25-task execution ledger were synchronized.
- Final planning QA passed: required plan Sections 0–17 are present, all 25 task IDs/names match the ledger, every phase has one Release build plus one selected test, no unresolved placeholder remains, and all three untracked files pass `git diff --no-index --check` with no whitespace findings.

### 🟡 IN PROGRESS

- Awaiting user review and Task 1.1 architecture decisions.

### ⏭️ NEXT

1. User reviews the plan, especially the recommended ADR-013 amendment making PostGIS mandatory for PostgreSQL deployments only after activation.
2. Decide whether Photon is the first provider and who operates/contracts its production endpoint.
3. Start Phase 1 at Task 1.1, then complete the location integrity/breaking contract slice before external provider work.
4. Do not start Phase 6 or Phase 8 until their explicit gates are approved.

### ⚠️ BLOCKERS

- **ADR activation:** ADR-013 remains Proposed and explicitly says a planning request is not activation approval.
- **PostgreSQL policy:** Choose mandatory PostGIS for all PostgreSQL deployments after activation (recommended) or re-plan a separate spatial DbContext/store and cross-context erasure transaction.
- **Photon production topology:** Public Photon is unsuitable as a production dependency; endpoint ownership, data footprint, update cadence, and capacity are not approved.
- **Map source:** No licensed production basemap/style provider is selected; public OSM/demo tiles are forbidden defaults.
- **Context7:** Requested documentation connector is unavailable. Revalidate dependencies there if it becomes available, otherwise continue with primary official documentation and record the substitution.

## Quick Resume

1. Read this context and `address-geocoding-and-spatial-discovery-tasks.md`.
2. Read only the current phase, constraints, or changed decisions from `address-geocoding-and-spatial-discovery-plan.md`; do not reread the full unchanged plan every resume.
3. Start from Task 1.1 unless the user overrides it.
4. Keep `tasks.md` current. Update context/plan only at their defined triggers.
5. Preserve unrelated shared-worktree changes and never edit generated migrations/client files by hand.

## Plan In One Paragraph

First repair the existing `Location` write boundary: tenant identity comes only from `ITenantContext`, manual address changes clear coordinates, geocoded changes require an atomic finite pair, and obsolete raw coordinate write fields are removed without shims. Add one Application `IAddressGeocoder` port, one Photon Infrastructure adapter, a short-lived Data Protection selection token, an authenticated/rate-limited/private POST API, and an accessible HAL-gated Blazor combobox. Only after ADR-013 activation add PostgreSQL/PostGIS: exact PII stays in `LocationPii`, an explicitly approved Persistence projection owns the NTS geography point, erasure/revocation is transactional, and exact discovery uses occurrence-level `ST_DWithin`/`ST_Distance` without exposing either point. One optional MapLibre component follows a tile-source/compatibility gate. Google, Pelias, native GeoNames, Leaflet, Martin, and generic map abstractions remain deferred.

## Key Files And Responsibilities

| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `dev/report/address_geocoding_analysis.md` | Existing | Research | Original provider/map analysis. | Input, not authority over current ADRs/rules. |
| `docs/adr/ADR-013-postgis-proximity-discovery.md` | Existing | Architecture | Governs exact proximity, public-point privacy, query shape, readiness, and activation. | Proposed; Task 1.1 owns decision. |
| `src/Explore.Domain/Location.cs` | Existing | Domain | Tenant aggregate, PII proxies, privacy lifecycle, concurrency. | Task 1.2 adds atomic manual/geocoded address methods. |
| `src/Explore.Domain/LocationPii.cs` | Existing | Domain | Hard-deleteable exact address/postcode/coordinates. | Remains scalar/private; no NTS `Point`. |
| `src/Explore.Application/DTOs/Location/CreateLocationDto.cs` | Existing | Application | Current location create contract. | Remove body tenant and raw coordinate inputs. |
| `src/Explore.Application/DTOs/Location/UpdateLocationDto.cs` | Existing | Application | Current grouped PATCH contract. | Remove raw coordinate groups; add protected selection group. |
| `src/Explore.Application/Features/Locations/Handlers/Commands/*LocationCommandHandler.cs` | Existing | Application | Validate and mutate location aggregate. | Explicit construction replaces fragile flattened mapping. |
| `src/Explore.Application/Profiles/LookupMappingProfile.cs` | Existing | Application | Current flattened Location maps. | Characterize then remove unsafe create mapping if no longer needed. |
| `src/Explore.Application/Contracts/Geocoding/` | New | Application | Provider-neutral port, models, and selection-protection abstraction. | No HTTP/NTS/provider types. |
| `src/Explore.Application/Features/Geocoding/` | New | Application | CQRS autocomplete orchestration and validation. | Manual validators; cancellation and bounded results. |
| `src/Explore.Infrastructure/Geocoding/` | New | Infrastructure | Photon HTTP and Data Protection adapters. | `HttpClientFactory`, resilience, PII-safe telemetry. |
| `src/Explore.API/Controllers/GeocodingController.cs` | New | API | Authenticated private autocomplete POST. | Named route, rate limit, RFC 7807, no-store. |
| `src/Event.Web.BffHosting/Proxy/EventApiProxyExtensions.cs` | Existing | BFF | Existing API/YARP proxy, token and tenant boundary. | Reuse; no bespoke geocoding BFF endpoint. |
| `src/Explore.Blazor.Client/Components/Locations/AddressAutocomplete.*` | New | Blazor | Accessible address selection. | Combobox/listbox, debounce/cancel, attribution, RTL. |
| `src/Explore.Blazor.Client/Pages/Admin/Dialogs/*LocationDialog*` | Existing | Blazor | Manual location create/edit experience. | Remove coordinate inputs; integrate protected selection. |
| `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | Existing | Persistence | Closed five-provider EF configuration. | Add NTS only to Application Npgsql branch after approval. |
| `src/Explore.Persistence/Spatial/` | New, Phase 6 | Persistence | Governed point row/store and PostGIS occurrence query. | Persistence owns NTS; never Domain/Application. |
| `src/Explore.AppHost/AppHost.cs` | Existing | DevOps | Local topology and PostgreSQL 18 resource. | Switch primary DB only after approval; keep heavy Photon optional. |
| `docker-compose.yml` | Existing | DevOps | Self-hosted topology. | Primary PostGIS PG18 volume target requires explicit migration/reset. |
| `schemas/openapi_islamu-event.json` | Generated | Contract | Canonical API schema. | Regenerate, never hand-edit. |
| `src/Explore.Blazor.Client/Clients/EventApiClient.g.cs` | Generated | Blazor | Generated API client. | Regenerate, never hand-edit. |
| `tests/Event.Architecture.Tests/DiscoveryPostgisSeparationArchitectureTests.cs` | Existing | Tests | Current negative proof that PostGIS runtime is absent. | Rebaseline only after activation to positive privacy/layer guards. |
| `dev/active/event-location-privacy/` | Existing | Dev docs | Authoritative PII disclosure/erasure/remediation workstream. | `ELP-515/520/530/730` are dependencies/evidence targets. |
| `dev/active/home-discovery-experience/` | Existing | Dev docs | Authoritative area-only Home and deferred PostGIS phase. | Preserve area-only behavior; cross-link instead of duplicating. |

## Key Decisions

1. **Autocomplete is independent of PostGIS/maps.** It is the first useful vertical slice.
2. **One port and one adapter.** Application owns `IAddressGeocoder`; Infrastructure implements Photon. No new project or four adapters.
3. **Protected stateless selections.** Data Protection prevents browser coordinate/result tampering without server session storage.
4. **Atomic address bundle.** Manual edits clear coordinates; geocoded selection sets all address/coordinate fields together.
5. **Trusted tenancy only.** Remove request-body tenant ID and raw coordinate writes; no compatibility shims.
6. **Existing API/YARP path.** No custom BFF endpoint or browser provider credentials.
7. **Recommended PostgreSQL policy.** After explicit activation, all PostgreSQL deployments use pinned PostGIS; other providers remain area-only/disabled. Rejecting this requires a new spatial-store design.
8. **Separate governed projection.** No spatial property/index on `LocationPii`; no automatic publication/backfill.
9. **Occurrence-level proximity.** PostGIS is the sole exact engine; server query uses eligible future occurrences and stable ordering.
10. **No exact public points.** API returns rounded distance/safe occurrence metadata only; origin is transient and private.
11. **One map integration.** MapLibre only after wrapper/tile-source approval; map is supplementary and list/form remains complete.
12. **Martin/providers deferred.** No auto-publication or provider/map abstraction until a concrete approved need exists.

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
- No address, token, origin, coordinate, secret, tenant/location ID, SQL, or upstream payload in logs/traces/metrics/readiness/errors.
- Only primary application PostgreSQL may switch image; do not alter Keycloak, Cerbos, Formbricks, or privacy-authority databases.

## External Research Record

| Topic | Primary source | Decision impact |
|---|---|---|
| Npgsql spatial mapping | https://www.npgsql.org/efcore/mapping/nts.html | Add matching NTS plugin, `UseNetTopologySuite()` only on Npgsql, explicit geography mapping. |
| Google Places policies | https://developers.google.com/maps/documentation/places/web-service/policies | Google remains deferred pending EEA/legal/storage/attribution review. |
| Google session pricing | https://developers.google.com/maps/documentation/places/web-service/session-pricing | Future adapter must own session token/termination/field-mask semantics. |
| Photon | https://github.com/komoot/photon | Public demo is not production; self-hosting has material data/RAM/disk/update cost. |
| Pelias docs | https://github.com/pelias/documentation | Deferred until measured coverage need and operations approval. |
| Martin configuration | https://maplibre.org/martin/config-file | If ever adopted: explicit allowlist and `auto_publish: false`. |
| Martin PostgreSQL tables | https://maplibre.org/martin/sources-pg-tables | Connection-only auto-discovery can publish all spatial tables/columns; never use on application schema. |
| OSM tile policy | https://operations.osmfoundation.org/policies/tiles/ | Public OSM tiles are not a default production basemap/SLA. |
| PostGIS image | https://hub.docker.com/r/postgis/postgis | Pin PG18/PostGIS image/tag/digest and follow PG18 volume rules. |

Tavily direct extraction supplied the verified vendor text. Tavily's broader research endpoint returned usage-limit status 432, so the plan relies on successful direct extractions and repository sources. Context7 was unavailable.

## Validation Baseline

For every implementation phase, run one Release build and at most one selected non-browser project test, once after phase tasks:

| Phase | Selected test project |
|---|---|
| 1 | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 2 | `tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj` |
| 3 | `tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj` |
| 4 | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` |
| 5 | `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` |
| 6 | `tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj` |
| 7 | `tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj` |
| 8 | `tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj` |

Do not run live app/browser/Aspire/Docker as an automated phase gate. Real PostGIS behavior is proven inside the Persistence integration project using its container fixture after that fixture is intentionally updated.

## Current Known Risks / Unknowns

- **1.1:** PostGIS image/extension policy across PostgreSQL deployments.
- **1.2:** Real flattened AutoMapper create behavior is not characterized by current mocked handler tests.
- **3.1:** Photon production sizing, data region, update swap, and ownership.
- **6.1:** PG18/PostGIS named-volume mount migration and rollback.
- **6.4:** Transactional integration with current privacy erasure/correction tasks.
- **7.1:** Representative data volume/query plan and cursor semantics.
- **8.1:** MapLibre wrapper maturity and production tile/style provider.
- **Deferred:** Google EEA/legal/storage semantics, Pelias coverage benefit, native dataset quality, Martin safe dataset.

## Unrelated Worktree Guidance

- The repository is shared and may contain user/other-agent changes. Do not revert, reformat, or include unrelated files.
- This planning session intentionally changed only the three files under `dev/active/address-geocoding-and-spatial-discovery/`.
- Existing active workstreams remain authoritative and were not rebaselined.

## Handoff Notes

### Handoff — 2026-08-11 Europe/Brussels

- **Current state:** Draft implementation plan is complete; 0/25 implementation tasks started.
- **Next action:** Review/decide Task 1.1, then implement Phase 1 only.
- **Blockers:** ADR activation/PostgreSQL policy, Photon topology, map tile source, Context7 unavailable.
- **Modified files:** The three workstream planning artifacts only.
- **Validation:** Pre-planning Release build passed 37 projects with 0 errors/warnings. Planning QA verified Sections 0–17, 25/25 task parity, one-test-per-phase limits, concrete paths, and clean no-index diffs for all three new files.
- **Documentation impact:** Planning docs created; canonical docs remain unchanged until implementation decisions/actions occur.
- **Risks:** Privacy leakage, optional PostGIS/multi-provider conflict, Photon operational load, PG18 volume migration, package/tile maturity.
- **Notes for next contributor/agent:** Do not copy the report's sample hierarchy literally. Reuse existing API/YARP/Location privacy paths, keep NTS out Domain/Application, and do not implement deferred providers/maps/Martin without their trigger.
