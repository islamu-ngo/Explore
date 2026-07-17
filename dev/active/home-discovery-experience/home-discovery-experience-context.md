<!-- ABOUTME: Operational memory for the planned MangaDex-inspired /home discovery experience. -->
<!-- ABOUTME: Captures verified source anchors, decisions, baseline evidence, risks, and the next implementation action. -->

# MangaDex-Inspired `/home` Discovery Experience — Context

Last Updated: 2026-07-17 Europe/Brussels

## SESSION PROGRESS (2026-07-16 Europe/Brussels)

### Completed

- **0.1:** Analyzed the full live [MangaDex homepage](https://mangadex.org/) with Chrome DevTools at desktop and mobile widths.
- **0.2:** Traced routes, Home branches, production/prototype cards, public-experience configuration/API, services, query filters, tests, render/SEO/accessibility/design contracts.
- **0.3:** Incorporated user and Senior CTO feedback:
  - no ad, substitute, or reserved gap;
  - area control directly above the hero;
  - explicit current-location and online actions;
  - stable discovery area IDs and coarse centroids;
  - no generic venue coordinate exposure;
  - one composite public-home endpoint;
  - honest area-level labels;
  - dedicated `HomeDiscoveryService`;
  - PostGIS recorded as planned exact proximity.
- Created and structurally validated plan/context/tasks.
- **0.4:** Established a fresh pre-edit baseline: Release build passed with 26 projects, 0 errors, and 0 warnings; Domain, Application, API Integration, Blazor Client, and Architecture test projects all passed with 0 warnings.
- **1.1:** Extended `docs/DESIGN.md` with the public-home page order, area context/privacy, reusable hero/card/rail states, three layout mappings, honest-copy and no-ad constraints, responsive/RTL/reduced-motion/accessibility behavior, hydration states, and payload/image/latency budgets. Architecture tests passed with 0 warnings.
- **1.2:** Added proposed ADR-013 and `ARCHITECTURE.md`/`DOMAIN.md`/`SELF_HOSTING.md` summaries for the separately approved PostGIS phase: governed public points, occurrence-level geography queries, transient private/no-store origin handling, GiST/readiness/backups, and area-only fallback. No schema/runtime/deployment change was made. Architecture tests passed with 0 warnings.
- **2.1:** Made the production three-layout `EventCard` a labeled keyboard target with visible tokenized focus, Enter/Space activation, and keyboard propagation guards around nested share/management/organizer actions. Every image now has meaningful alt text, lazy loading, async decoding, and intrinsic dimensions. Focused Blazor Client and Architecture suites pass.
- **2.2:** Replaced the autoplay/unbounded hero prototypes with one manual `HeroCarousel`: at most 10 slides, wrapping previous/next controls, counter, LTR/RTL pointer swipes, public slug links, local image fallback, and one high-priority active image URL rendered as backdrop and poster. A 2026-07-17 Chrome DevTools comparison tightened the hero to the poster's lower edge and aligned plain `NO. n` plus transparent arrow controls in the poster's bottom lane. Styling is isolated, logical-direction, responsive, and reduced-motion safe; the unused `FeaturedEventHero` was deleted after caller proof.
- **2.3:** Rebuilt `EventHorizontalRail` as a semantic, focusable native scroll-snap list using the production `CompactGrid` event card. Loading and empty states are explicit; heading/View-all, touch, keyboard scrolling, RTL, responsive sizing, reduced motion, and click forwarding are covered. The duplicate presentation card was deleted after its two legacy prototype callers moved to the canonical card. Four focused tests plus full Blazor Client and Architecture suites passed.
- **3.1:** Added `public_experience.discovery_areas`, exact `home_discovery.area_id`/`home_discovery.mode` user preferences, a bounded versioned Application config, and a public coarse-area DTO that excludes location IDs and PII. Pure validation enforces schema/size, stable unique IDs, one active default, paired two-decimal centroids, tenant-owned location references, and one-area-per-location mapping. Focused tests plus full Domain/Application and Architecture suites passed; generic location contracts/entities are unchanged.
- **3.2:** Added one bounded composite CQRS query over the existing public event-list handler. It resolves tenant-owned area mappings, supports area/online/all context, isolates section failures, evaluates each semantic section independently, omits unsupported curation, and exposes reserved proximity fields as null. `GET /api/public-experience/home` is anonymous, output-cached by tenant/host/area/mode, and represented by the clean `GetHomeDiscovery` operation ID in regenerated OpenAPI, API inventory, NSwag client, and API changelog. Handler and API contract tests cover the composition.
- **3.3:** Added `HomeDiscoveryService` plus a one-shot browser geolocation adapter. URL context precedes saved context, the server retains authority over default/first/all fallback, location is reduced in memory to the closest coarse configured centroid, and only area ID/mode are persisted. Online mode preserves the selected area. Six service and two interop tests passed.
- **3.4:** Added Application, Blazor, JS-source, generated-client, hydration, and BFF-header privacy guards. Home Discovery never consumes generic location DTOs or exposes addresses/internal mappings/venue coordinates; origin has no URL, setting, persistent-state, API, log, console, analytics, or network sink. The browser BFF permits geolocation to self only and the API remains disabled.
- **3.5:** Added the cancellation-aware `HomeDiscoveryExperience` around one `[PersistentState]` composite DTO. Hydration restores before service access, authenticated and anonymous visitors share the same discovery branch, and top-level/section failures are bounded without erasing successful sections.
- **4.1-4.5:** Replaced the legacy authenticated/anonymous Home split with the shared discovery surface while preserving organization-centric remediation/content. The page renders area context, a manual hero, a dedicated MangaDex-inspired `UpcomingEventList`, production cards for spotlight/rails, factual server-backed labels, loading/empty/partial states, translation fallbacks, logical RTL CSS, reduced motion, forced colors, and no ad gap. The update list uses direct event links, vertical groups of six compact 7:10 rows, and local image fallbacks instead of adding a fourth `EventCard` mode. Both obsolete landing components and the standalone marketing route were removed after caller proof.
- **5.3 partial:** Added one-second section and three-second composite timeouts, a compact card/hero projection, 10-item standard sections, and at most two explicit curated rails. The maximum-count exact source-generated JSON test passes the 256 KiB uncompressed and 120 KiB Brotli/gzip ceilings; the focused handler suite now contains eight passing tests.
- **5.1-5.2 partial:** Started Docker Desktop and ran the browser flow through hydration, responsive captures, mobile overlay close, manual hero, keyboard cards/rail scrolling, granted Brussels geolocation, and online mode. Inspected 375 light LTR, 768 dark LTR, and 1280 light RTL/reduced-motion captures; the composition, shell spacing, no-image fallback, themes, and direction were visually sound. The denied branch is now deterministic at the product boundary, screenshot capture resets focus/scroll, and the browser check enforces header/heading separation.
- **5.3 partial runtime fixes:** Live logs exposed overlong HybridCache keys for compound discovery filters and triple composite reads after each context action. The event-list handler now SHA-256 hashes only its canonical specification suffix, with a regression proving the Home-style key stays within 512 characters. Home context actions retain the already-loaded DTO and update the URL with `history.replaceState`, with component tests proving one reload and coordinate-free URL state. A trace also exposed two 502,894-byte fallback-image transfers because XSRF issuance marked static assets `no-store`; static assets now bypass token issuance, caching and protected-mutation integration regressions pass, and manual browser QA checks one encoded hero transfer within 500 KiB.
- **5.4-5.5:** Canonical design/Blazor/API/security and workstream docs now match the implementation and blockers. No broad public-Blazor intent was added because it would overlap existing intent routes.

### In Progress

- **5.1-5.3:** The retired browser flow proved context actions do not reload `/home`, enforced the cache/transfer limits, ran 20-sample uncached/cached API p95 gates, measured fresh-cache mobile LCP under the documented 375×844/4× CPU/4G profile, and wrote a JSON performance artifact. Current automated coverage is held at the API, BFF, and component seams; refreshed screenshots remain manual QA.

### Next

1. After the shared Event Location Privacy migrations align the database with the current location model, rerun the focused `HomeDiscoveryFlowTests` lane from a clean database.
2. Confirm one hero transfer, one composite reload per context action, deterministic denied-location behavior, clean console/network, and refreshed 375/768/1280 captures.
3. Complete controlled uncached/cached p95 and LCP sampling, then rerun the canonical project gates while keeping unrelated failures separate.

### Blockers

- Docker Desktop and Aspire worked during the earlier partial run, but the latest retry could not start the engine: the QEMU VM terminated on both service start and restart, after which Docker Desktop was stopped cleanly. Separately, the current shared location model remains ahead of its persistence migrations: the latest completed Aspire run fails with PostgreSQL `42703` because `locations.location_kind_id` does not exist, while an earlier run failed on missing `event_sessions.event_location_id`. Home Discovery does not own those schema changes and will not add or alter their migrations.
- The current repository-level Release build is also blocked by nine unrelated test compilation errors: seven unresolved `EventLocationDisclosureStates` references in Event Location Privacy tests and two unresolved `IMudDialogInstance` references in Event Detail sidebar tests. Home Discovery does not own those changes.
- The shared Application suite currently has six unrelated behavioral failures in agenda/program/calendar location privacy and registration-mode handling. The latest full Blazor Client suite has six unrelated failures: three AI-assistant reference-search timing failures and three Event Location Privacy failures. This task does not own or modify those implementations.
- Independent visual-review subagents required by the visual-QA workflow remain disabled by the user's no-subagent instruction; do not claim that independent review occurred.
- Phase 6 requires separate future approval and is not part of the current release.

## Quick Resume

1. Read this file, the plan, and tasks.
2. Recheck the shared dirty worktree and do not include unrelated changes.
3. Continue canonical Task 5.1 after the shared Event Location Privacy migration blocker is resolved.
4. Keep identical task IDs in all three docs.
5. Never use “near you” or distance wording in current-release code/docs.

## MangaDex Reference Evidence

Chrome DevTools observations from 2026-07-15 and the focused 2026-07-17 desktop comparison:

- Page order: compact shell → 10-slide hero → ad/content break → dense latest grid → multiple horizontal shelves → footer.
- Hero: a 440px desktop composition whose 7:10 poster ends near the banner edge; the counter is plain `No. n` text and transparent 40px arrow targets share the poster's bottom lane.
- Latest grid: six stacked 80px rows per column, 56×80 thumbnails, one column narrow and 2/3/4 columns wide.
- Shelves: fixed-width auto slides, native-feeling touch/wheel motion, clipped preview.
- Mobile: compact header/hero, one-column dense list, narrow shelves.
- ISLAMU translation: hierarchy, density, manual hero controls, and progressive disclosure only.
- Rejected: MangaDex shell/brand/assets/colors, ad region, exact card proportions, autoplay requirement.

Local evidence (untracked):

- `/tmp/mangadex-home-desktop.png`
- `/tmp/mangadex-home-mobile.png`
- `/tmp/mangadex-snapshot.txt`

## Current Architecture Evidence

- `PublicExperienceController` already exposes cached anonymous settings/shell reads.
- Public-experience tenant configuration already uses versioned JSON documents.
- `GetEventListRequestHandler` already enforces published/public discovery and supports area location IDs, format, date, actor, `date`, `views`, and `createdat`.
- `LocationPii` owns exact coordinates, but generic `LocationListDto` is not privacy-safe: it exposes `Address` plus identifying venue/city/country fields. Home Discovery must not consume or browser-enumerate that contract.
- `IUserSettingsService` supports authenticated BFF persistence and anonymous localStorage with SSR-safe behavior.
- `HomeDiscoveryService` is the sole home-discovery orchestrator; the obsolete marketing-only service has been removed.
- `HomeStart` renders `Home` when startup policy selects PublicLanding, so startup-selected `/` intentionally follows the new `/home` composition.
- The standalone anonymous marketing route has been removed; `/home` owns public discovery.

## Key Files And Planned Responsibilities

| Path | Status | Planned responsibility |
|---|---|---|
| `src/Explore.Domain/Constants/GovernanceSettingKeys.cs` | existing | Discovery-area config and user preference keys. |
| `src/Explore.Domain/Settings/Definitions/PublicExperienceSettingDefinitions.cs` | existing | Tenant area config plus user area/mode definitions. |
| `src/Explore.Application/Models/PublicExperience/PublicDiscoveryAreasConfig.cs` | new | Versioned stable areas, coarse centroids, default/active state, internal location IDs. |
| `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs` | new | Composite context/sections/statuses and event wrapper contract. |
| `src/Explore.Application/Models/PublicExperience/HomeDiscoveryEnums.cs` | new | String-enum selection/status contracts registered in OpenAPI outside the DTO namespace. |
| `src/Explore.Application/Features/PublicExperience/**/GetHomeDiscovery*` | new | Tenant-safe bounded server composition with independent semantic sections. |
| `src/Explore.API/Controllers/PublicExperienceController.cs` | existing | Add composite anonymous GET route. |
| `src/Explore.API/Extensions/CachingExtensions.cs` | existing | Tenant/area/mode cache policy. |
| `src/Explore.Blazor.Client/Services/HomeDiscoveryService.cs` | new | One generated-client home read; safe UI result mapping. |
| `src/Explore.Blazor.Client/Services/Interop/HomeDiscoveryGeolocation.cs` | new | Explicit JS interop boundary for transient low-accuracy origin reads. |
| `src/Explore.Blazor.Client/Pages/Home.razor` | implemented | Unified discovery composition while preserving organization-centric branches. |
| `src/Explore.Blazor.Client/Components/Discovery/HomeDiscoveryExperience.*` | implemented | Persisted composite rendering, area actions, three layouts, and bounded states. |
| `src/Explore.Blazor.Client/wwwroot/js/home-discovery.js` | implemented | Explicit one-shot browser origin read only; no persistence/network/logging. |
| Production `Pages/Events/Components/EventCard.*` | existing | Single card implementation for all three modes. |
| `src/Explore.Blazor.Client/Components/Presentation/HeroCarousel.*` | implemented | Manual, accessible, isolated hero; at most 10 bounded slides and one eager active image. |
| `src/Explore.Blazor.Client/Components/Discovery/UpcomingEventList.*` | implemented | Direct-link compact update rows, six vertically ordered items per column, responsive 1/2/3-column layout, and local image fallback. |
| `src/Explore.Blazor.Client/Components/Collection/EventHorizontalRail.*` | implemented | Native scroll-snap production-card rail with explicit loading/empty state. |
| `docs/adr/ADR-013-postgis-proximity-discovery.md` | implemented documentation | Planned, not implemented, exact-proximity contract. |

## Durable Decisions

1. Current release provides stable area/city discovery only, not event proximity.
2. Trigger copy is “Browsing events in {Area}” or “Browsing online events.”
3. Requested dropdown actions are “Use my current location” and “Browse online events.”
4. URL/settings persist `area_id` and mode, never a localized city display string or origin.
5. Area selection order: valid URL → saved area ID → tenant default active area → first active area → all-area mode.
6. Browser origin is requested only on action and compared with coarse public area centroids.
7. Home Discovery uses only its dedicated coarse `PublicDiscoveryAreaDto`; it never consumes generic `LocationListDto`, whose existing `Address`/identifying fields are unsafe for public discovery. Retirement/minimization of the generic contract belongs to Event Location Privacy.
8. Current labels are “Upcoming in {Area},” “Most viewed in {Area},” “Most viewed online,” explicit curation, and “Recently added.”
9. “Near you,” closest/radius/distance, trending, recommended, and unsupported community/grassroots labels are forbidden.
10. One composite GET returns all current home discovery sections; server owns tenant validation, independent section membership, cache, ETag, and status.
11. `HomeDiscoveryService` owns this use case; the legacy marketing-only service is deleted.
12. Hero is manual by default; the active backdrop/poster URL is prioritized and all inactive slide images are lazy.
13. No ad, CTA substitute, or reserved ad gap.
14. PostGIS is the sole planned exact-proximity engine and measures minimum distance to an eligible future public event-session occurrence.
15. Phase 6 never starts without separate explicit approval.
16. Do not add a broad public-Blazor intent for this one workstream; existing intent routes plus the explicit fallback contract are sufficient, and overlap risk outweighs speculative reuse.
17. The endpoint route name and generated client method are `GetHomeDiscovery` / `GetHomeDiscoveryAsync`; controller-prefixed operation IDs violate the client naming contract.
18. Upcoming discovery is a semantic update list, not a fourth `EventCard` mode: compact rows flow top-to-bottom in groups of six and link directly to public event routes.

## Location Privacy By Capability

| Data | Area-only now | Planned PostGIS |
|---|---|---|
| User origin | Browser memory only | Rounded/bounded transient first-party POST |
| Area centroid | Public coarse config | Public coarse config |
| Exact venue point | Never generic | Governed public discovery point only |
| Stored preference | Area ID/mode | Area ID/mode/optional radius |
| URL/log/trace/analytics | Never origin | Never origin |
| Cache | Shared tenant/area/mode | Nearby response private, no-store |

## Validation Baseline

Completed on 2026-07-16 before home-discovery edits:

```text
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
Result: all passed; build reported 26 projects, 0 errors, 0 warnings; every test command reported 0 warnings.
```

## Current Risks

- Composite handler performs several bounded internal reads; payload and timeout budgets are proven, while hosted uncached/cached p95 remains unmeasured.
- Anonymous localStorage preference and persisted hydration are covered by focused tests; the latest live browser rerun is blocked by the unrelated missing location migration.
- Discovery-area JSON needs validation and a safe administration path; do not silently accept cross-tenant location IDs.
- View sorting is not quality; honest “Most viewed” copy is mandatory.
- PostGIS roadmap docs must never imply runtime capability before Phase 6.
- Shared worktree contains unrelated changes.

## Post-Implementation Verification Snapshot

- Last full Release build before the latest shared-tree changes: 26 projects, 0 errors, 0 warnings. The retired Home browser flow built with no errors and passed targeted formatting/analyzer verification; the repository-level build then failed on nine unrelated test compilation errors.
- Green no-infrastructure suites: Domain, Secrets, Architecture, and all focused Home Discovery Application/API/Blazor tests, including API client naming and generated-contract inventory.
- Full Application: 2,280/2,286 passed; six unrelated agenda/program/calendar/registration failures.
- Full Blazor Client: 1,688/1,695 passed, one skipped; six unrelated AI-assistant/Event Location Privacy failures.
- Focused BFF static-cache, API-proxy antiforgery, and preference-antiforgery suites pass; static images no longer receive `XSRF-TOKEN` or `no-store`.
- Latest independent verification: Domain, focused Home API contract, focused BFF cache/antiforgery, and the accessibility/Blazor/docs/code-hygiene Architecture subset pass. The full Architecture suite has three unrelated failures: a missing backend-health authorization matrix, an unregistered Event Location Privacy enum, and an Event Location Privacy request outside a `Queries` namespace.
- Historical Home Discovery browser QA reached every path through granted geolocation and online mode before the earlier denied-permission ambiguity. Current manual reruns stop during event seed on shared location-schema columns absent from migrations.
- Visual QA: 375 light LTR, 768 dark LTR, and 1280 light RTL/reduced-motion captures were generated and inspected. One initial composite sample was 644.83 ms, within the 800 ms target but insufficient for p95; LCP and cached p95 remain unmeasured. Independent visual subagents were not used under the user's explicit constraint.

## Handoff Notes

### Handoff — 2026-07-16 Europe/Brussels

- **Current state:** Phases 0-4 and Tasks 5.4-5.5 are complete; Tasks 5.1-5.3 have substantive live evidence but remain open.
- **Next action:** Rerun `HomeDiscoveryFlowTests` after Docker Desktop is healthy and the shared location migration lands, then collect p95/LCP and final verification evidence.
- **Blocker:** Docker Desktop currently terminates its QEMU VM, and the last healthy-Docker run reached PostgreSQL `42703` on shared location-model columns absent from migrations (`locations.location_kind_id` currently; `event_sessions.event_location_id` previously); do not solve either by weakening the database contract inside this workstream.
- **Verification blocker:** the current Release build also fails on unrelated Event Location Privacy and Event Detail sidebar test compilation errors; do not modify that concurrent work from Home Discovery.
- **Home fixes verified:** bounded hashed HybridCache keys, in-place history updates without navigation reloads, cache-safe static assets, deterministic denied geolocation, and browser hero-transfer budget assertion.
- **Do not:** resume webhook work, edit the unrelated location schema/migration, claim a p95 from one 644.83 ms sample, or start Phase 6.

### Handoff — 2026-07-15 Europe/Brussels

- **Current state:** Planning amended and user-reviewed; no product code changed.
- **Next action:** Task 0.4 fresh baseline.
- **Blockers:** None for current release; Phase 6 separately deferred.
- **Modified files:** only the three planning files under this task directory.
- **Validation:** prior Release build passed; post-amendment headers/dates/whitespace, canonical task-ID parity, required sections, and stale-architecture contradiction checks passed.
- **Do not:** consume or expose generic `LocationListDto`, enumerate address/coordinate catalogs in the browser, compose many browser event calls, use deceptive labels, add autoplay/ad filler, or implement Phase 6.
