<!-- ABOUTME: Operational memory for the planned MangaDex-inspired /home discovery experience. -->
<!-- ABOUTME: Captures verified source anchors, decisions, baseline evidence, risks, and the next implementation action. -->

# MangaDex-Inspired `/home` Discovery Experience — Context

Last Updated: 2026-07-15 Europe/Brussels

## SESSION PROGRESS (2026-07-15 Europe/Brussels)

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

### In Progress

- None. Implementation has not started.

### Next

1. **Task 0.4:** run the fresh build and Domain/Application/API/Blazor/Architecture test baseline; capture warning delta.
2. **Task 1.1:** update the current public-discovery design contract.
3. **Task 1.2:** create the planned PostGIS ADR and canonical architecture/domain/self-hosting summaries.
4. Do not begin UI/API code before 0.4, 1.1, and 1.2 are complete.

### Blockers

- No implementation blocker.
- Phase 6 requires separate future approval and is not part of the current release.

## Quick Resume

1. Read this file, the plan, and tasks.
2. Recheck the shared dirty worktree and do not include unrelated changes.
3. Start canonical Task 0.4.
4. Keep identical task IDs in all three docs.
5. Never use “near you” or distance wording in current-release code/docs.

## MangaDex Reference Evidence

Chrome DevTools observations from 2026-07-15:

- Page order: compact shell → 10-slide hero → ad/content break → dense latest grid → multiple horizontal shelves → footer.
- Hero: image background/gradient/cover/metadata, previous/next/position, responsive detail reduction.
- Latest grid: one column narrow and multiple columns wide.
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
- `LocationPii` owns exact coordinates; `LocationListDto` intentionally omits them.
- `IUserSettingsService` supports authenticated BFF persistence and anonymous localStorage with SSR-safe behavior.
- `LandingPageService` still serves `/welcome` marketing and should not become the home-discovery orchestrator.
- `HomeStart` renders `Home` when startup policy selects PublicLanding, so startup-selected `/` intentionally follows the new `/home` composition.
- `/welcome` independently routes to anonymous marketing and remains unchanged.

## Key Files And Planned Responsibilities

| Path | Status | Planned responsibility |
|---|---|---|
| `src/Explore.Domain/Constants/GovernanceSettingKeys.cs` | existing | Discovery-area config and user preference keys. |
| `src/Explore.Domain/Settings/Definitions/PublicExperienceSettingDefinitions.cs` | existing | Tenant area config plus user area/mode definitions. |
| `src/Explore.Application/Models/PublicExperience/PublicDiscoveryAreasConfig.cs` | new | Versioned stable areas, coarse centroids, default/active state, internal location IDs. |
| `src/Explore.Application/DTOs/PublicExperience/HomeDiscoveryDto.cs` | new | Composite context/sections/statuses and event wrapper contract. |
| `src/Explore.Application/Features/PublicExperience/**/GetHomeDiscovery*` | new | Tenant-safe bounded server composition/dedupe/backfill. |
| `src/Explore.API/Controllers/PublicExperienceController.cs` | existing | Add composite anonymous GET route. |
| `src/Explore.API/Extensions/CachingExtensions.cs` | existing | Tenant/area/mode cache policy. |
| `src/Explore.Blazor.Client/Services/HomeDiscoveryService.cs` | new | One generated-client home read; safe UI result mapping. |
| `src/Explore.Blazor.Client/Services/LandingPageService.cs` | existing | Remains `/welcome` marketing only; do not extend. |
| `src/Explore.Blazor.Client/Pages/Home.*` | existing | Unified discovery composition and persistent state. |
| `src/Explore.Blazor.Client/wwwroot/js/home-location.js` | new | Explicit browser origin read only; no persistence/logging. |
| Production `Pages/Events/Components/EventCard.*` | existing | Single card implementation for all three modes. |
| Existing `HeroCarousel`/`FeaturedEventHero` | existing | Manual, accessible, isolated hero. |
| Existing `EventHorizontalRail` | existing | Native scroll-snap production-card rail. |
| `docs/adr/ADR-*-postgis-proximity-discovery.md` | new | Planned, not implemented, exact-proximity contract. |

## Durable Decisions

1. Current release provides stable area/city discovery only, not event proximity.
2. Trigger copy is “Browsing events in {Area}” or “Browsing online events.”
3. Requested dropdown actions are “Use my current location” and “Browse online events.”
4. URL/settings persist `area_id` and mode, never a localized city display string or origin.
5. Area selection order: valid URL → saved area ID → tenant default active area → first active area → all-area mode.
6. Browser origin is requested only on action and compared with coarse public area centroids.
7. Generic `LocationListDto` and exact `LocationPii` coordinates remain unchanged/private.
8. Current labels are “Upcoming in {Area},” “Most viewed in {Area},” “Most viewed online,” explicit curation, and “Recently added.”
9. “Near you,” closest/radius/distance, trending, recommended, and unsupported community/grassroots labels are forbidden.
10. One composite GET returns all current home discovery sections; server owns tenant validation, dedupe/backfill, cache, ETag, and status.
11. New `HomeDiscoveryService` owns this use case; legacy `LandingPageService` remains marketing-only.
12. Hero is manual by default; active image is prioritized, optional next preloaded, remainder lazy.
13. No ad, CTA substitute, or reserved ad gap.
14. PostGIS is the sole planned exact-proximity engine and measures minimum distance to an eligible future public event-session occurrence.
15. Phase 6 never starts without separate explicit approval.

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

Completed:

```text
dotnet build --configuration Release --verbosity quiet
Result: passed, 26 projects, 0 errors, 4,445 warnings from pre-existing/shared worktree state
```

Task 0.4 must run before implementation:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Current Risks

- Composite handler still performs several bounded internal reads; cache, payload, and latency budgets must be proven.
- Anonymous localStorage preference can require one post-hydration correction when canonical URL/default differ; test and avoid loops.
- Discovery-area JSON needs validation and a safe administration path; do not silently accept cross-tenant location IDs.
- View sorting is not quality; honest “Most viewed” copy is mandatory.
- PostGIS roadmap docs must never imply runtime capability before Phase 6.
- Shared worktree contains unrelated changes.

## Handoff Notes

### Handoff — 2026-07-15 Europe/Brussels

- **Current state:** Planning amended and user-reviewed; no product code changed.
- **Next action:** Task 0.4 fresh baseline.
- **Blockers:** None for current release; Phase 6 separately deferred.
- **Modified files:** only the three planning files under this task directory.
- **Validation:** prior Release build passed; post-amendment headers/dates/whitespace, canonical task-ID parity, required sections, and stale-architecture contradiction checks passed.
- **Do not:** expose `LocationListDto` coordinates, extend `LandingPageService`, compose many browser event calls, use deceptive labels, add autoplay/ad filler, or implement Phase 6.
