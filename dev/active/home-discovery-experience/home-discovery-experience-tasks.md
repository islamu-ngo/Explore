<!-- ABOUTME: Tactical checklist for implementing the MangaDex-inspired /home discovery experience. -->
<!-- ABOUTME: Tracks approval, design, components, data composition, page integration, verification, and deferred scope. -->

# MangaDex-Inspired `/home` Discovery Experience — Task Checklist

Last Updated: 2026-07-16 Europe/Brussels

## Status Summary

- **Overall status:** In implementation
- **Completed:** Phases 0-4 plus Tasks 5.4-5.5
- **Current priority:** Tasks 5.1-5.3 final runtime evidence; Aspire ran partially, and reruns are blocked by Docker Desktop QEMU failure plus unrelated Event Location Privacy model/migration drift
- **Current release:** Phases 0-5
- **Deferred:** Phase 6 PostGIS proximity, separate approval required

## Maintenance Rules

- [ ] Read plan/context/tasks and recheck shared `git status` before each task.
- [ ] Use the same canonical task ID in all three docs, commits, and handoffs.
- [ ] Update docs immediately after each completed task or changed decision.
- [ ] Write the smallest behavioral check for each protected boundary.
- [ ] Never hand-edit generated API artifacts or include unrelated worktree changes.
- [ ] Never use “near you”/distance wording until Phase 6 is implemented and proven.

## Phase 0: Approved Boundaries And Baseline

- [x] **0.1 Analyze the whole MangaDex homepage**
  - Whole desktop/mobile page, interaction grammar, responsive density, shelves, footer, and rejected ad region recorded through Chrome DevTools.

- [x] **0.2 Trace current `/home` and discovery infrastructure**
  - Routes, Home branches, cards, prototypes, public-experience config/API, services, tests, and render policy traced.
  - Planning-time Release build baseline: 0 errors, 4,445 pre-existing/shared warnings; superseded by the clean Task 0.4 baseline.

- [x] **0.3 Approve geospatial and location-privacy direction**
  - Current release is area-only with stable IDs/coarse centroids.
  - No generic venue coordinates, “near you,” distance, IP lookup, automatic prompt, or client venue scan.
  - PostGIS is the sole planned exact-proximity engine.

- [x] **0.4 Run fresh pre-implementation baseline**
  - Run build plus Domain, Application, API, Blazor Client, and Architecture tests.
  - 2026-07-16: build passed (26 projects, 0 errors, 0 warnings); all five named test projects passed with 0 warnings.
  - **Dependency:** 0.3

## Phase 1: Durable Design And Planned Architecture

- [x] **1.1 Extend the public discovery design contract**
  - Update `docs/DESIGN.md` for area context, manual hero, three layouts, honest labels, no ad, image loading, responsive/RTL/motion/accessibility.
  - Added reusable area-control, hero, event-card, and rail states plus page order, loading/failure/hydration, and measurable performance budgets; Architecture tests passed with 0 warnings.
  - **Dependency:** 0.4

- [x] **1.2 Document planned PostGIS proximity discovery**
  - Create next available PostGIS ADR.
  - Summarize planned status in `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and `docs/SELF_HOSTING.md`.
  - Cover governed public points, eligible future sessions, transient POST origin, `geography(Point,4326)`, GiST, readiness/cache/self-hosting.
  - Added proposed ADR-013 and canonical summaries; no runtime/deployment/schema claim was introduced; Architecture tests passed with 0 warnings.
  - **Dependency:** 0.3

## Phase 2: Component Consolidation

- [x] **2.1 Make production `EventCard` keyboard-safe**
  - Add focused tests, then Enter/Space card-body navigation without breaking nested share/edit/delete or HAL gating.
  - Added labeled keyboard semantics, Enter/Space handling, tokenized focus-visible outline, keyboard propagation guards around nested actions, and meaningful lazy-decoded images with intrinsic dimensions in every layout; focused Blazor Client and Architecture tests passed.
  - **Dependency:** 1.1

- [x] **2.2 Refactor the manual hero**
  - Reuse existing hero prototypes with isolated CSS.
  - Up to 10 slides; previous/next/swipe/counter; no autoplay; active image prioritized, optional next preload, remainder lazy.
  - Cover fallback, mobile, RTL, reduced motion, labels, and navigation.
  - Consolidated the prototypes into one isolated-CSS component, removed unsupported labels/autoplay and the unreferenced child prototype, used clean public event URLs, capped rendering at 10, and added manual controls, pointer swipe, RTL direction, fallback images, and one eager active image. Four focused tests plus full Blazor Client and Architecture suites passed; responsive browser evidence remains in Phase 5.
  - **Dependencies:** 1.1, 2.1

- [x] **2.3 Refactor the native horizontal rail**
  - Render production `CompactGrid` cards with native scroll-snap, semantic heading/View all, touch/keyboard/RTL.
  - Delete duplicate presentation card only after caller proof and build.
  - Added explicit loading/empty states, a semantic heading/link, focusable native logical-axis scroll snap, responsive/RTL/reduced-motion styling, and production-card click forwarding. The duplicate presentation card was removed after its two legacy prototype callers were switched to the canonical card. Four focused tests plus full Blazor Client and Architecture suites passed.
  - **Dependency:** 2.1

## Phase 3: Area-Only Composite Discovery Contract

- [x] **3.1 Add the discovery-area contract**
  - Add governance keys/settings and versioned `PublicDiscoveryAreasConfig`.
  - Stable Guid, display/city/country, coarse centroid, active/default state, internal tenant location IDs.
  - Add user preferences `home_discovery.area_id` and `home_discovery.mode`.
  - Do not consume generic `LocationListDto`; it exposes `Address` plus identifying fields and is not privacy-safe for Home Discovery. Use only the dedicated coarse area DTO; Event Location Privacy owns generic-contract minimization.
  - Test duplicate/default/coordinate/tenant-location validation.
  - Added the versioned tenant JSON setting, exact user preference keys/modes, bounded Application config, coarse public DTO without internal IDs/PII, source-generation registrations, and pure tenant-location-set validation. Focused validation, full Domain/Application, and Architecture suites passed; Task 3.1 did not consume the unsafe generic `LocationListDto`, whose Address exposure is tracked by Event Location Privacy.
  - **Dependencies:** 0.4, 1.2

- [x] **3.2 Add the composite home-discovery query and API**
  - Add `HomeDiscoveryDto`, `EventDiscoveryItemDto`, query/handler, public-experience route/cache, tests, and regenerated contract artifacts.
  - Exactly one `GET /api/public-experience/home?areaId={guid}&mode={mode}`.
  - Server owns tenant validation, bounded queries, honest sections, deterministic dedupe/backfill, statuses, query-varying cache, ETag.
  - Omit unsupported curated sections.
  - Added the bounded CQRS orchestrator over existing public event queries, fail-closed area resolution, priority dedupe/backfill, isolated section statuses, anonymous route and query-varying cache. OpenAPI uses the clean `GetHomeDiscovery` operation ID; API inventory, NSwag client, and changelog were regenerated. Eight focused handler and two controller-contract tests pass.
  - **Dependency:** 3.1

- [x] **3.3 Add the frontend discovery context**
  - Add `IHomeDiscoveryService`/`HomeDiscoveryService` as the dedicated home orchestrator.
  - Resolve URL → saved area ID → tenant default → first active → all-area.
  - Add explicit browser geolocation action against coarse area centroids; hide/disable it when no active centroid areas exist; online mode preserves area.
  - Keep denied/unavailable state safe.
  - Added a dedicated service with URL → saved → server-default precedence, authenticated/anonymous coarse preference persistence, closest-centroid reduction, and online-area preservation. The one-shot low-accuracy JS adapter has no storage/network/logging sink; six service and two interop tests passed.
  - **Dependency:** 3.2

- [x] **3.4 Add explicit location-privacy tests**
  - Prove Home Discovery never serializes generic location DTOs, addresses, or venue coordinates.
  - Prove origin never enters URL, settings, persistent state, logs, analytics, errors, or outbound API requests in area-only mode.
  - Verify geolocation Permissions-Policy is self-only.
  - Added Application/browser reflection and source guards proving no generic location DTO, address, venue coordinate, origin parameter, persistent origin, browser sink, or hydration service read. The BFF policy is exactly `geolocation=(self)` while the API remains disabled; focused privacy and BFF tests passed.
  - **Dependencies:** 3.1, 3.3

- [x] **3.5 Compose and persist homepage state**
  - One composite payload, `[PersistentState]`, no hydration duplicate, no origin persistence, bounded partial failures, cancellation safety, auth/anonymous parity.
  - Added `HomeDiscoveryExperience` with one persisted DTO, cancellation-aware reloads, safe retry/partial states, and identical discovery composition for anonymous/authenticated visitors. Focused component, hydration, privacy, and Home branch tests passed.
  - **Dependencies:** 2.2, 2.3, 3.3, 3.4

## Phase 4: `/home` Composition

- [x] **4.1 Replace discovery-centric Home branches**
  - `/home` and startup-selected `/` use discovery; remove the obsolete standalone marketing route.
  - Preserve organization remediation/encoding.
  - Remove `LandingPageForUsers` only after caller proof.
  - Replaced both legacy discovery branches with the shared component, preserved organization-centric remediation/content, and deleted both obsolete landing components plus the standalone marketing route and service after repository-wide caller proof. Seven focused Home tests passed.
  - **Dependency:** 3.5

- [x] **4.2 Render area context directly above the hero**
  - “Browsing events in {Area}”/“Browsing online events.”
  - Actions: “Use my current location” and “Browse online events.”
  - One h1, manual hero, contrast, no ad/CTA/reserved gap.
  - Added the native labeled area selector, explicit location/online actions, one h1, manual hero, visible status text, and normal section spacing with no ad-shaped region.
  - **Dependency:** 4.1

- [x] **4.3 Render all three honest event layouts**
  - `DetailedList`: “Upcoming in {Area},” 1/2/3 columns.
  - `SingleRow`: explicit curated/primary-actor spotlight only; omit otherwise.
  - `CompactGrid`: “Most viewed in {Area},” “Most viewed online,” explicit curated labels, “Recently added.”
  - No near/trending/recommended/unsupported community or grassroots labels.
  - Wired `DetailedList`, evidence-backed `SingleRow`, and `CompactGrid` rails to server section truth. Most-viewed/recent/explicit curated labels are factual; unsupported and deceptive labels are absent. The server caps standard sections at 10 and explicit curated rails at two.
  - **Dependency:** 4.2

- [x] **4.4 Finish loading, empty, and partial-failure states**
  - Stable skeletons, polite announcements, safe copy, `/events` fallback, successful sections survive one failure.
  - Added geometry-preserving context/hero/card skeletons, top-level retry, per-section empty/failure copy, polite announcements, and `/events` fallbacks. Focused tests prove one failed section does not erase successful sections.
  - **Dependency:** 4.3

- [x] **4.5 Complete localization and RTL**
  - Translation keys/fallbacks for all copy and accessible labels.
  - Area names remain data; future distance units are documented but not rendered.
  - Verify RTL layout/focus.
  - Routed new static copy and accessible labels through translation keys with English fallback, kept area labels as server data, and used logical isolated CSS plus RTL/reduced-motion/forced-colors behavior. The partial Task 5.2 browser run covered RTL and reduced motion; the final rerun remains open.
  - **Dependency:** 4.4

## Phase 5: Verification And Handoff

- [ ] **5.1 Add Aspire-backed public home flow**
  - Route, one composite call, area/online actions, granted/denied location, manual hero, keyboard cards/rails, URL state, no origin leakage, clean console.
  - Added and compiled `HomeDiscoveryFlowTests` with real tenant settings/event seeding, route/hero/action/geolocation/privacy/console assertions, responsive screenshots, deterministic denied geolocation, header-overlap protection, one-transfer/500-KiB hero assertions, and a guard proving context actions do not reload the `/home` document. Aspire execution reached online mode. The latest retry could not start Docker Desktop because its QEMU VM terminated; the last healthy-Docker reruns stopped during event seed because shared location-model columns are absent from migrations (`locations.location_kind_id` currently; `event_sessions.event_location_id` previously).
  - **Dependency:** 4.5

- [ ] **5.2 Run responsive visual and accessibility QA**
  - 375/768/1280, light/dark, RTL, reduced motion, long/no-image/empty/failure states, lazy images, no overflow/ad gap.
  - Record Chrome screenshots/network/console evidence.
  - Generated and inspected 375 light LTR, 768 dark LTR, and 1280 light RTL/reduced-motion captures. Hero, grids/rails, shell spacing, dark theme, RTL, mobile overlay, keyboard card focus, rail scrolling, and no page overflow passed the observed run. The test now resets scroll/focus before capture; refreshed evidence waits on the shared migration. Independent visual subagents remain prohibited by the user.
  - **Dependency:** 5.1

- [ ] **5.3 Run quality, contract, and performance gates**
  - Required build/tests and deterministic generated artifacts.
  - No new warnings over 0.4.
  - One initial discovery call.
  - ≤256 KiB uncompressed / ≤120 KiB compressed.
  - Controlled p95 ≤800 ms uncached / ≤200 ms cached; LCP target 2.5 s.
  - Per-section hard timeout 1 s; composite hard maximum 3 s with safe failure behavior.
  - ≤2 eager hero images and ≤500 KiB initial hero bytes.
  - Partial evidence: focused handler suite passes one-second section/three-second composite cancellation; maximum-count source-generated JSON passes 256 KiB raw and 120 KiB Brotli/gzip limits; hero tests prove one eager image; EventCard layouts prove lazy loading, async decoding, alt text, and intrinsic dimensions. One live initial composite sample was 644.83 ms. Runtime diagnostics drove bounded SHA-256 cache keys, one in-place reload per area/online action, and cache-safe static assets; focused regressions pass. The browser now enforces one 500-KiB hero transfer and contains 20-sample uncached/cached p95 plus fresh-cache 375×844/4× CPU/4G LCP gates that write `performance.json`; executing those measurements remains blocked by the shared migration.
  - **Dependency:** 5.2

- [x] **5.4 Refresh canonical and dev docs**
  - DESIGN/BLAZOR/API docs plus plan/context/tasks match implementation/evidence and keep Phase 6 clearly deferred.
  - Refreshed design, Blazor, API changelog/inventory, security model, proposed PostGIS architecture summaries, and all three workstream docs. Remaining Docker and shared-worktree verification gaps are recorded without claiming live evidence.
  - **Dependency:** 5.3

- [x] **5.5 Decide whether to add a general public-Blazor intent**
  - Record decision; isolate any context-governance edit and run schema/duplication tests.
  - Decision: do not add one yet. The cross-layer fallback was explicit and a broad intent would overlap existing component/OpenAPI/setting intents; reconsider after a second repeated public-Blazor workstream. No intent file was changed for this feature.
  - **Dependency:** 5.4

## Phase 6: PostGIS Proximity Discovery — PLANNED FOR LATER

- [ ] **6.1 Approve geospatial ADR and privacy contract**
  - Define disabled/area-only/PostGIS modes, public point vs `LocationPii`, occurrence distance, caching, privacy, readiness.

- [ ] **6.2 Add PostGIS capability and spatial persistence**
  - Canonical migration, `geography(Point,4326)`, GiST/tenant indexes, approved-point backfill, readiness and self-hosting docs.
  - No generic provider or in-memory exact-distance fallback.

- [ ] **6.3 Add nearby event CQRS query and API**
  - Transient rounded origin/radius/date/format body.
  - Tenant/public/future-session predicates, `ST_DWithin`, minimum occurrence distance, stable cursor, private no-store response.

- [ ] **6.4 Integrate distance metadata into discovery UI**
  - Populate `EventDiscoveryItemDto` distance/nearest-occurrence fields.
  - Localize metres/kilometres and enable “near you” only for valid PostGIS results.

- [ ] **6.5 Verify privacy, correctness, performance, and operations**
  - Real PostGIS tenant isolation; draft/private/unlisted/member visibility; nearest future multi-location session; past exclusion; hybrid/online-only; just-inside/on/outside radius; missing point; stable ordering; cross-border/duplicate-city tests.
  - No origin leakage/shared cache; `EXPLAIN ANALYZE` index evidence; readiness/degraded state; backup/restore/index docs.

## Current-Release Verification Checklist

- [ ] Changed diagnostics clean and no new warnings versus Task 0.4 (the Home E2E file passes targeted format/analyzer verification; the shared tree currently has unrelated test compilation errors and warning churn; Razor LSP remains unavailable by prior user choice).
- [ ] Release build passes (last clean run: 26 projects, 0 errors, 0 warnings; current run fails on seven missing Event Location Privacy test symbols and two unrelated MudBlazor dialog-test type errors).
- [ ] Domain, Application, API, Blazor Client, and Architecture test projects pass.
- [x] OpenAPI/inventory/NSwag artifacts are regenerated deterministically, never hand-edited.
- [ ] E2E passes or environment blocker has equivalent manual evidence.
- [ ] One composite discovery call and payload/latency/image budgets pass.
- [ ] Keyboard, localization, RTL, reduced motion, light/dark, empty/error/no-image/long-title states pass.
- [ ] No generic `LocationListDto`, address/coordinate catalog, origin leakage, deceptive labels, ad gap, raw HTML, role/claim action logic, or unrelated edits.
- [x] Plan/context/tasks and canonical docs match actual behavior and explicitly record partial live evidence and the shared migration blocker.

## Deferred Outside Phase 6

- Quality/recommendation scoring.
- Server-side Free filter.
- Personalized recommendations/consent model.
- Hydrated arbitrary tenant presets without typed curation.
- IP or third-party geolocation.
