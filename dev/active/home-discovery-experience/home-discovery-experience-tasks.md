<!-- ABOUTME: Tactical checklist for implementing the MangaDex-inspired /home discovery experience. -->
<!-- ABOUTME: Tracks approval, design, components, data composition, page integration, verification, and deferred scope. -->

# MangaDex-Inspired `/home` Discovery Experience — Task Checklist

Last Updated: 2026-07-15 Europe/Brussels

## Status Summary

- **Overall status:** User-reviewed and Senior-CTO-amended; implementation not started
- **Completed:** 0.1 MangaDex analysis, 0.2 repository trace, 0.3 area/PostGIS privacy decision
- **Current priority:** 0.4 fresh baseline, then 1.1 and 1.2 durable docs
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
  - Release build baseline: 0 errors, 4,445 pre-existing/shared warnings.

- [x] **0.3 Approve geospatial and location-privacy direction**
  - Current release is area-only with stable IDs/coarse centroids.
  - No generic venue coordinates, “near you,” distance, IP lookup, automatic prompt, or client venue scan.
  - PostGIS is the sole planned exact-proximity engine.

- [ ] **0.4 Run fresh pre-implementation baseline**
  - Run build plus Domain, Application, API, Blazor Client, and Architecture tests.
  - Record exact failures and warning count before edits.
  - **Dependency:** 0.3

## Phase 1: Durable Design And Planned Architecture

- [ ] **1.1 Extend the public discovery design contract**
  - Update `docs/DESIGN.md` for area context, manual hero, three layouts, honest labels, no ad, image loading, responsive/RTL/motion/accessibility.
  - **Dependency:** 0.4

- [ ] **1.2 Document planned PostGIS proximity discovery**
  - Create next available PostGIS ADR.
  - Summarize planned status in `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and `docs/SELF_HOSTING.md`.
  - Cover governed public points, eligible future sessions, transient POST origin, `geography(Point,4326)`, GiST, readiness/cache/self-hosting.
  - **Dependency:** 0.3

## Phase 2: Component Consolidation

- [ ] **2.1 Make production `EventCard` keyboard-safe**
  - Add focused tests, then Enter/Space card-body navigation without breaking nested share/edit/delete or HAL gating.
  - **Dependency:** 1.1

- [ ] **2.2 Refactor the manual hero**
  - Reuse existing hero prototypes with isolated CSS.
  - Up to 10 slides; previous/next/swipe/counter; no autoplay; active image prioritized, optional next preload, remainder lazy.
  - Cover fallback, mobile, RTL, reduced motion, labels, and navigation.
  - **Dependencies:** 1.1, 2.1

- [ ] **2.3 Refactor the native horizontal rail**
  - Render production `CompactGrid` cards with native scroll-snap, semantic heading/View all, touch/keyboard/RTL.
  - Delete duplicate presentation card only after caller proof and build.
  - **Dependency:** 2.1

## Phase 3: Area-Only Composite Discovery Contract

- [ ] **3.1 Add the discovery-area contract**
  - Add governance keys/settings and versioned `PublicDiscoveryAreasConfig`.
  - Stable Guid, display/city/country, coarse centroid, active/default state, internal tenant location IDs.
  - Add user preferences `home_discovery.area_id` and `home_discovery.mode`.
  - Do not modify `LocationListDto` or `LocationPii`.
  - Test duplicate/default/coordinate/tenant-location validation.
  - **Dependencies:** 0.4, 1.2

- [ ] **3.2 Add the composite home-discovery query and API**
  - Add `HomeDiscoveryDto`, `EventDiscoveryItemDto`, query/handler, public-experience route/cache, tests, and regenerated contract artifacts.
  - Exactly one `GET /api/public-experience/home?areaId={guid}&mode={mode}`.
  - Server owns tenant validation, bounded queries, honest sections, deterministic dedupe/backfill, statuses, query-varying cache, ETag.
  - Omit unsupported curated sections.
  - **Dependency:** 3.1

- [ ] **3.3 Add the frontend discovery context**
  - Add `IHomeDiscoveryService`/`HomeDiscoveryService`; do not extend `LandingPageService`.
  - Resolve URL → saved area ID → tenant default → first active → all-area.
  - Add explicit browser geolocation action against coarse area centroids; hide/disable it when no active centroid areas exist; online mode preserves area.
  - Keep denied/unavailable state safe.
  - **Dependency:** 3.2

- [ ] **3.4 Add explicit location-privacy tests**
  - Prove generic location DTOs expose no coordinates.
  - Prove origin never enters URL, settings, persistent state, logs, analytics, errors, or outbound API requests in area-only mode.
  - Verify geolocation Permissions-Policy is self-only.
  - **Dependencies:** 3.1, 3.3

- [ ] **3.5 Compose and persist homepage state**
  - One composite payload, `[PersistentState]`, no hydration duplicate, no origin persistence, bounded partial failures, cancellation safety, auth/anonymous parity.
  - **Dependencies:** 2.2, 2.3, 3.3, 3.4

## Phase 4: `/home` Composition

- [ ] **4.1 Replace discovery-centric Home branches**
  - `/home` and startup-selected `/` use discovery; `/welcome` stays marketing.
  - Preserve organization remediation/encoding.
  - Remove `LandingPageForUsers` only after caller proof.
  - **Dependency:** 3.5

- [ ] **4.2 Render area context directly above the hero**
  - “Browsing events in {Area}”/“Browsing online events.”
  - Actions: “Use my current location” and “Browse online events.”
  - One h1, manual hero, contrast, no ad/CTA/reserved gap.
  - **Dependency:** 4.1

- [ ] **4.3 Render all three honest event layouts**
  - `DetailedList`: “Upcoming in {Area},” 1/2/3 columns.
  - `SingleRow`: explicit curated/primary-actor spotlight only; omit otherwise.
  - `CompactGrid`: “Most viewed in {Area},” “Most viewed online,” explicit curated labels, “Recently added.”
  - No near/trending/recommended/unsupported community or grassroots labels.
  - **Dependency:** 4.2

- [ ] **4.4 Finish loading, empty, and partial-failure states**
  - Stable skeletons, polite announcements, safe copy, `/events` fallback, successful sections survive one failure.
  - **Dependency:** 4.3

- [ ] **4.5 Complete localization and RTL**
  - Translation keys/fallbacks for all copy and accessible labels.
  - Area names remain data; future distance units are documented but not rendered.
  - Verify RTL layout/focus.
  - **Dependency:** 4.4

## Phase 5: Verification And Handoff

- [ ] **5.1 Add Aspire-backed public home flow**
  - Route, one composite call, area/online actions, granted/denied location, manual hero, keyboard cards/rails, URL state, no origin leakage, clean console.
  - **Dependency:** 4.5

- [ ] **5.2 Run responsive visual and accessibility QA**
  - 375/768/1280, light/dark, RTL, reduced motion, long/no-image/empty/failure states, lazy images, no overflow/ad gap.
  - Record Chrome screenshots/network/console evidence.
  - **Dependency:** 5.1

- [ ] **5.3 Run quality, contract, and performance gates**
  - Required build/tests and deterministic generated artifacts.
  - No new warnings over 0.4.
  - One initial discovery call.
  - ≤256 KiB uncompressed / ≤120 KiB compressed.
  - Controlled p95 ≤800 ms uncached / ≤200 ms cached; LCP target 2.5 s.
  - Per-section hard timeout 1 s; composite hard maximum 3 s with safe failure behavior.
  - ≤2 eager hero images and ≤500 KiB initial hero bytes.
  - **Dependency:** 5.2

- [ ] **5.4 Refresh canonical and dev docs**
  - DESIGN/BLAZOR/API docs plus plan/context/tasks match implementation/evidence and keep Phase 6 clearly deferred.
  - **Dependency:** 5.3

- [ ] **5.5 Decide whether to add a general public-Blazor intent**
  - Record decision; isolate any context-governance edit and run schema/duplication tests.
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

- [ ] Changed diagnostics clean and no new warnings versus Task 0.4.
- [ ] Release build passes.
- [ ] Domain, Application, API, Blazor Client, and Architecture test projects pass.
- [ ] OpenAPI/inventory/NSwag artifacts are regenerated deterministically, never hand-edited.
- [ ] E2E passes or environment blocker has equivalent manual evidence.
- [ ] One composite discovery call and payload/latency/image budgets pass.
- [ ] Keyboard, localization, RTL, reduced motion, light/dark, empty/error/no-image/long-title states pass.
- [ ] No generic location coordinates, origin leakage, deceptive labels, ad gap, raw HTML, role/claim action logic, or unrelated edits.
- [ ] Plan/context/tasks and canonical docs match actual behavior.

## Deferred Outside Phase 6

- Quality/recommendation scoring.
- Server-side Free filter.
- Personalized recommendations/consent model.
- Hydrated arbitrary tenant presets without typed curation.
- IP or third-party geolocation.
