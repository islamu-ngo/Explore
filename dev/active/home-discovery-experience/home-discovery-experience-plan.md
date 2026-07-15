<!-- ABOUTME: Repository-grounded implementation plan for a MangaDex-inspired event discovery experience on /home. -->
<!-- ABOUTME: Defines the page grammar, component reuse, data selection, accessibility, tests, risks, and implementation handoff. -->

# MangaDex-Inspired `/home` Discovery Experience — Implementation Plan

Last Updated: 2026-07-15 Europe/Brussels

## 0. Planning Metadata

- **Request:** Analyze the whole [MangaDex homepage](https://mangadex.org/) with Chrome DevTools and plan an ISLAMU Event `/home` experience with an immersive carousel, dense vertical event sections, horizontal shelves, and the existing three event-card layouts. Do not redesign `/events`.
- **Task directory:** `dev/active/home-discovery-experience/`
- **Planning status:** User-reviewed and architecture-amended after Senior CTO review; implementation not started
- **Matched intents:** No intent covers the whole public-page redesign. `blazor-component-affordance` governs only edit/delete visibility. The composite home endpoint and dedicated discovery-area DTO invoke the secondary `openapi-contract-change` contract, while the broader work uses the fallback contract below and includes a task to consider a reusable `blazor-public-experience` intent.
- **Fallback contract:** `AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/API.md`, `docs/BLAZOR.md`, `docs/DESIGN.md`, `docs/DESIGN_SYSTEM.md`, `docs/ACCESSIBILITY.md`, `docs/RENDER_POLICIES.md`, `docs/SEO.md`, `docs/TESTING.md`, `docs/CODEBASE_STRUCTURE.md`, and `docs/BLAZOR_DEV_WORKFLOW.md`.
- **Relevant skills:** `clean-architecture-rules`, `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `senior-cto-feedback`, and the frontend reference-analysis workflow.
- **Relevant rules:** `.claude/rules/domain.md`, `.claude/rules/application-layer.md`, `.claude/rules/api-controllers.md` for contract governance, `.claude/rules/blazor-client.md`, and `.claude/rules/tests.md`.
- **Primary layers touched:** Domain setting definitions, Application public-experience models/query, API controller/cache contract, generated OpenAPI/client artifacts, Blazor Client, tests, and docs. The area-only release needs no Persistence schema or migration.
- **Estimated complexity:** **L**. The first release changes a public SEO route, establishes one composite cacheable read model, adds a coarse tenant-configured discovery-area contract, consolidates duplicate components, and requires responsive/accessibility/privacy/browser verification. Exact proximity is a separate planned PostGIS phase.
- **Baseline:** `dotnet build --configuration Release --verbosity quiet` passed on 2026-07-15 with 0 errors. The shared worktree already produced 4,445 warnings unrelated to this plan; do not treat those as introduced by this work.

### 0.1 Fallback Contract Details

| Contract concern | Planning decision |
|---|---|
| Must-read docs | The fallback documents named above plus `dev/active/README.md` and `.claude/commands/dev-docs.md`. |
| Skills/rules | Apply the listed Blazor, CSS isolation, design-system, accessibility, and test guidance. |
| Paths in scope | Public-experience setting/config models, composite public-home query/controller/cache, generated contract artifacts, `src/Explore.Blazor.Client/**`, matching Domain/Application/API/Blazor tests, `tests/Explore.Blazor.Client.E2ETests/**`, `docs/DESIGN.md`, a planned-proximity ADR plus architecture/domain/self-hosting summaries, API contract docs, and these dev docs. |
| Minimum tests | `Event.Domain.UnitTests`, `Event.Application.UnitTests`, `Event.API.IntegrationTests`, `Explore.Blazor.Client.Tests`, and `Event.Architecture.Tests`; run E2E manually/nightly after the Aspire host is available. |
| Docs to update | `docs/DESIGN.md`, a new planned-proximity ADR, `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, `docs/SELF_HOSTING.md`, generated API contract/inventory/client artifacts, `docs/API_CHANGELOG.md`, and `docs/BLAZOR.md` for the composite home flow. |
| Unique acceptance | `/home` has the approved MangaDex-inspired page rhythm, reuses the production three-mode `EventCard`, remains tenant-aware, and passes responsive/accessibility browser QA. |
| Forbidden without approval | Exact venue coordinates in generic public DTOs, “near you”/distance claims without event-occurrence geospatial results, IP/third-party geolocation, an in-memory exact-distance fallback, a recommendation/quality-score backend, a new carousel dependency, advertising/ad-shaped filler, or a visual redesign of `/events`. |

## 1. Executive Summary

Build a discovery-focused `/home` that borrows MangaDex's **information rhythm**, not its brand: an image-led hero carousel, a dense time-sensitive section, curated horizontal shelves, restrained section headers, progressive disclosure, and a compact mobile transformation. Event data and ISLAMU's existing design tokens, shell, tenant configuration, footer, and production event cards remain authoritative.

The first release will:

1. keep the existing ISLAMU navigation, announcement, tenant shell, footer, render policy, and `/events` catalog;
2. turn `/home` into the public event-discovery surface for both anonymous and authenticated visitors;
3. leave `/welcome` as the anonymous marketing/registration page;
4. render the three existing `LayoutMode` variants deliberately:
   - `SingleRow` for a short editorial spotlight list;
   - `DetailedList` in a responsive multi-column “Upcoming in {Area}” grid;
   - `CompactGrid` inside horizontal shelves;
5. refactor the existing hero/rail prototypes to use CSS isolation, accessible controls, and the production `Pages/Events/Components/EventCard`;
6. add one cacheable `GET /api/public-experience/home?areaId={id}&mode={mode}` response so initial discovery content requires one API call beyond existing shell/bootstrap reads;
7. persist stable `home_discovery.area_id` and mode values, never a localized city string;
8. place a “Browsing events in {Area}” control directly above the carousel with “Use my current location” and “Browse online events” actions;
9. request browser location only after explicit action, compare it only with coarse public discovery-area centroids, and never expose generic venue coordinates;
10. use honest labels such as “Upcoming in {Area},” “Most viewed in {Area},” “Most viewed online,” and “Recently added”;
11. omit MangaDex's ad/content-break region entirely, without a CTA substitute or reserved gap;
12. document PostGIS as the sole planned exact-proximity engine without implementing it in this release.

### Explicitly Out Of Scope For The First Release

- No redesign of `/events`; only canonical “View all” links may target it.
- No database schema or migration for the area-only release. Discovery areas are a versioned tenant public-experience configuration with stable IDs, coarse centroids, and internal location-ID mappings.
- No changes to generic `LocationListDto` and no generic exposure of exact venue coordinates.
- No opaque “quality score.” `EventListDto` exposes views but not organizer verification or RSVP totals, so the pasted quality-gate idea cannot be implemented honestly yet.
- No “Free” quick filter until the event API supports a server-side price predicate. Filtering a single fetched page in the browser would be incomplete and misleading.
- No exact “near me,” radius filtering, distance ordering/display, IP lookup, automatic permission prompt, third-party geocoding, ad system, personalization model, or tracking expansion.
- No “Recommended,” “Grassroots,” or “Community spotlight” fallback unless explicit tenant curation or actor scope proves the label.
- No copying of MangaDex names, artwork, flags, colors, sidebar, or exact card dimensions.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| `/home` resolves to `Home`; `/` resolves to `HomeStart`. | Verified: `src/Explore.Blazor.Client/Routes.razor` lines 63-66. | High | `HomeStart` may render `<Home />` for `PublicLanding`, so a `/home` change can also affect `/`. |
| `/home` is a public SEO route governed at runtime. | Verified: `docs/RENDER_POLICIES.md`; `docs/SEO.md`. | High | Do not hardcode a component render mode. |
| `Home` branches into organization-centric content or authenticated/anonymous landing components. | Verified: `src/Explore.Blazor.Client/Pages/Home.razor`. | High | Organization remediation and tenant shell content must be preserved. |
| The anonymous marketing page is independently routable at `/welcome`. | Verified: `src/Explore.Blazor.Client/Routes.razor`; `LandingPageForNonUsers.razor`. | High | This enables `/home` to specialize in discovery without deleting the marketing page. |
| Production event cards support exactly three layouts. | Verified: `src/Explore.Blazor.Client/Models/LayoutMode.cs`; `Pages/Events/Components/EventCard.razor`. | High | Modes are `CompactGrid`, `DetailedList`, and `SingleRow`. |
| The three-mode event card has bUnit coverage. | Verified: `tests/Explore.Blazor.Client.Tests/Components/Event/EventCardTests.cs`. | High | Tests cover layout classes, fields, HAL actions, past state, and share behavior. |
| Edit/delete actions are already HAL-gated. | Verified: `EventCard.razor.cs::CanEdit` and `CanDelete`. | High | Homepage reuse must not add role/claim checks. |
| Existing presentation carousel/rail/card components are prototypes and are not wired into current pages. | Verified by search: `HeroCarousel`, `FeaturedEventHero`, and `EventHorizontalRail` have no page call sites; `HeroCarousel` only calls `FeaturedEventHero`, and the rail calls the duplicate presentation card. | High | They contain inline `<style>`, physical directions, and duplicate card behavior. |
| Existing public-experience settings can describe home blocks, CTAs, and filtered event-section presets. | Verified: `PublicExperienceSettingDefinitions.cs`; `PublicEventSectionPresetsConfig.cs`; `PublicExperienceShellDto.cs`. | High | The shell currently projects event presets as label/icon/URL, not hydrated event items. |
| The event service already supports the filters needed for initial shelves. | Verified: `src/Explore.Blazor.Client/Services/EventService.cs::GetEventsPagedAsync`. | High | Supports format, location, date, owner, category/tag, and exact sort strings. |
| Server sort keys are `date`, `title`, `views`, and `createdat`. | Verified: `GetEventListRequestHandler.cs::ResolveSortField`. | High | Do not use UI labels `popularity` or `created`. |
| Exact location coordinates live behind the `LocationPii` boundary and are intentionally absent from `LocationListDto`. | Verified: `LocationPii`, `Location`, `LocationListDto`, generated client. | High | Preserve this boundary; the area-only release exposes coarse discovery-area centroids through a dedicated DTO instead. |
| Public-experience configuration already uses versioned tenant JSON documents. | Verified: `PublicExperienceSettingDefinitions`, `PublicExperienceHomeBlocksConfig`, `PublicEventSectionPresetsConfig`, shell handler. | High | Add one bounded `public_experience.discovery_areas` document rather than a new table/entity for the first release. |
| User-scoped settings already support authenticated API persistence and anonymous localStorage with SSR-safe reads. | Verified: `IUserSettingsService`, `UserSettingsService`, and existing public-experience preference definition. | High | Add stable `home_discovery.area_id`/`mode`; never persist a display name or origin. |
| The API already exposes a cached public-experience controller/shell pattern. | Verified: `PublicExperienceController`, `GetPublicExperienceShellQueryHandler`, `PublicExperienceShell` output-cache policy. | High | Add a separate composite home query/route with query-varying cache and ETag behavior. |
| A free-price server filter was not found. | Not found after searches for price/free filters in `EventService`, event filter bar, and API list query. | High | Defer the `Free` chip rather than client-filtering one page. |
| A recommendation quality score cannot use RSVP or verification signals from the list DTO. | Verified: generated `EventListDto` contains `TotalViews` but no verified-host or registration-count fields. | High | Views support only an honest “Most viewed” label. |
| Current `/home` tests protect auth branching, organization shell content, HTML encoding, remediation, and graceful fallback. | Verified: `tests/Explore.Blazor.Client.Tests/Pages/HomeTests.cs`. | High | Preserve or intentionally replace each protected behavior. |
| Baseline build is green. | Verified by command on 2026-07-15. | High | 0 errors; warning volume is pre-existing/shared-worktree state. |

### 2.2 Whole-Page MangaDex Reference Analysis

Chrome DevTools MCP inspected the live [MangaDex homepage](https://mangadex.org/) at wide desktop and narrow/mobile layouts, including DOM geometry, computed styles, Swiper state, interaction controls, and full-page screenshots.

#### Page Grammar Observed

1. **Shell:** wide screens use a persistent navigation rail and compact top search/profile controls; narrow screens collapse to a small top bar. ISLAMU should keep its own `MainLayout` instead of copying this shell.
2. **Hero:** a full-bleed 10-slide image carousel sits immediately below the shell. Each slide layers a cover/background image, dark gradient, foreground cover, title, tags, description, creator metadata, previous/next buttons, and `No. n`/total position.
3. **Motion:** the hero uses a Swiper slide transition (`speed: 300ms`), touch movement, navigation arrows, and a 10-second autoplay configuration. ISLAMU retains swipe/arrows but chooses manual rotation and reduced transitions.
4. **Content break:** MangaDex inserts a centered ad block between the immersive hero and dense feed. ISLAMU explicitly rejects this region: no ad, CTA substitute, placeholder, or reserved whitespace is added.
5. **Latest Updates:** rows are grouped into responsive columns. Each column holds six compact 80px rows. Chrome observed one column at narrow width and 2/3/4 columns at larger breakpoints.
6. **Horizontal shelves:** `Recommended`, `Self-Published`, `Seasonal`, and `Recently Added` use fixed-width slides with `slidesPerView: auto`, 20px gaps, touch/wheel movement, pagination dots, clipped next-card preview, and a section-level “view all” arrow.
7. **Card density:** wide shelves use approximately 256px slides; narrow shelves use approximately 128px slides and show roughly three cards plus a clipped preview. ISLAMU event cards should remain larger because their metadata and touch targets differ.
8. **Mobile reduction:** the hero shrinks from roughly 440px to 324px, hides the long description, keeps cover/title/creator/tags/counter/arrows, turns Latest Updates into one column, and preserves horizontal shelves.
9. **Footer:** the page ends with compact social, version/legal, and payment marks. ISLAMU keeps its existing tenant-configurable footer.

#### Translation Rules

- Copy the **hierarchy and interaction grammar**, not the brand surface.
- Replace manga language flags with event format/type/date/price indicators.
- Replace “Latest Updates” timestamps with upcoming event dates and organizer/location metadata.
- Omit the MangaDex ad/content break entirely and let the first event section follow the hero at normal section spacing.
- Replace MangaDex card components with the production ISLAMU `EventCard` and its three layout modes.
- Preserve ISLAMU's semantic tokens, white-label appearance, tenant isolation, RTL support, and HAL action gating.

### 2.3 Existing Implementation By Layer

#### Routing And Render Policy

- `Routes.razor` maps `/home` to `Home`, `/welcome` to `LandingPageForNonUsers`, and `/` to `HomeStart`.
- `HomeStart.razor` chooses setup, public landing, event list, tenant redirect, or login state. It renders `Home` when startup policy returns `PublicLanding`.
- `RuntimeRenderPolicyService` owns render decisions; `/`, `/home`, `/welcome`, and `/events` belong to `PublicSeo`.

#### Home Composition

- `Home.razor` loads `IPublicExperienceService`, authentication state, and up to three organization events.
- Organization-centric mode renders shell-driven hero blocks, CTAs, preset links, upcoming events, organization contact, and footer-link projections.
- Discovery-centric mode delegates to `LandingPageForUsers` or `LandingPageForNonUsers`.
- `LandingPageForUsers` contains a marketing hero, three explore tiles, and a three-card `MudCarousel`.
- `LandingPageForNonUsers` contains a separate marketing hero, benefits, another three-card carousel, FAQ, metrics, and final CTA.
- The two landing pages duplicate event-card/carousel markup and fixed inline styling.

#### Event Data

- `LandingPageService` currently fetches the default event page and sorts that page in memory by `TotalViews`; it does not request server sort order and therefore does not prove a global “Most viewed” result.
- `GetEventListRequestHandler` already provides the server-side query semantics needed by the composite home handler.
- `PublicEventSectionPresetsConfig` stores typed owner and event filters, but `PublicExperienceEventSectionDto` projects them into a browse URL only.

#### Reusable UI

- The production `Pages/Events/Components/EventCard` is the only card that should power `/home`; it owns fallbacks, the three layouts, past/moderated states, share action, field visibility, and HAL-gated edit/delete actions.
- `Components/Presentation/EventCard.razor` is a second event card with different logic and no tests; keeping it would create behavioral drift.
- `HeroCarousel`, `FeaturedEventHero`, and `EventHorizontalRail` are useful names/starting points but require refactoring before production use.

### 2.4 Existing Tests And Verification Coverage

| Project/file | Existing protection | Planned action |
|---|---|---|
| `Explore.Blazor.Client.Tests/Pages/HomeTests.cs` | Loading, auth/anonymous branch, organization shell, HTML encoding, remediation, title, error fallback. | Re-baseline discovery expectations while retaining organization safety tests. |
| `Explore.Blazor.Client.Tests/Components/Event/EventCardTests.cs` | Three modes, field visibility, HAL/share behavior, past state. | Add keyboard semantics and homepage-safe action tests. |
| `Explore.Blazor.Client.Tests/Services/LandingPageServiceTests.cs` | Current `/welcome` featured sorting/filtering and error handling. | Preserve for marketing; add separate `HomeDiscoveryServiceTests`. |
| `Event.Architecture.Tests/AccessibilityConventionTests.cs` | Landmarks, h1, target/focus/RTL advisories. | Run unchanged; do not weaken. |
| `Explore.Blazor.Client.E2ETests` | Aspire-backed browser smoke and critical flows. | Add a new public `/home` responsive interaction flow. |

### 2.5 Existing Documentation And Contracts

- `docs/DESIGN.md` is the active design contract. It currently emphasizes operational/admin density and needs a bounded public-discovery extension before implementation.
- `docs/DESIGN_SYSTEM.md` owns tokens, CSS layers, wrappers, and MudBlazor override policy.
- `docs/ACCESSIBILITY.md` requires one `h1`, icon labels, real or keyboard-equivalent interaction semantics, reduced motion, logical CSS, and announcements.
- `docs/RENDER_POLICIES.md` and `docs/SEO.md` define `/home` as a public SEO route.
- `docs/BLAZOR.md` requires service-layer API access, persistent prerender state, tenant-safe public experience, and no component-owned render policy.
- The composite endpoint and discovery DTOs intentionally require regenerated OpenAPI, inventory, NSwag client, and API changelog artifacts.

### 2.6 Current Pain Points / Improvement Areas

1. **Discovery is split across three home variants.** The same route changes identity based on auth and public-experience mode, making the product harder to understand and test.
2. **Duplicate card and carousel markup.** Marketing pages and presentation prototypes bypass the production three-mode card.
3. **Inline styling conflicts with the design-system contract.** Prototype components use inline `<style>`, raw colors, physical `left/right`, and no colocated CSS.
4. **Prototype hero accessibility is incomplete.** Bookmark/share icon buttons have no labels, and auto-rotation has no explicit pause control.
5. **Production cards are click-only containers.** `MudCard @onclick` lacks verified keyboard link semantics; homepage reuse would amplify the issue.
6. **Popularity is currently page-local.** `LandingPageService` sorts only the default returned page in memory and is the wrong use-case boundary for primary discovery.
7. **No honest quality gate exists.** The current list projection cannot rank verified hosts or RSVP strength.
8. **Area context has no contract.** No stable public discovery-area ID, coarse centroid, tenant default, or public-coordinate boundary exists.
9. **Empty/error behavior is fragmented.** Each landing branch handles loading and failures differently.
10. **Preset sections are browse links, not hydrated shelves.** First release should hydrate only explicitly curated/supported sections server-side, not infer “community” from `createdat`.
11. **Client composition would multiply public reads.** Six or seven Blazor/BFF/API calls would fragment caching, deduplication, hydration, and future PostGIS integration.
12. **Area selection is not proximity.** City/area filtering cannot support “near you,” radius, closest-event, or distance wording.

### 2.7 Unknowns After Investigation

| Unknown | What was searched | Resolution task |
|---|---|---|
| Route impact of `/`. | `Routes.razor`, `HomeStart.razor`, onboarding/public-experience docs. | Resolved: startup-selected `PublicLanding` intentionally renders the same `Home`; `/welcome` remains marketing. |
| First-visit area without browser permission. | Public-experience settings, user settings, browser APIs. | Resolve stable area ID in this order: valid URL → saved user area ID → tenant default active area → first active area → all-area mode. Never infer from a localized city string or prompt automatically. |
| Whether a “Free” quick filter is mandatory now. | Event service/API/filter-bar price predicates. | No server predicate found. Deferred unless user explicitly expands scope. |
| Which configured presets can be hydrated honestly. | Preset config, shell DTO, handler URL builder, Home/EventList consumers. | Task 3.2 supports only typed filters/labels that prove their content; unsupported semantic labels remain links or are omitted. |

## 3. Proposed Future State

### 3.1 Page Order

```text
Existing announcement/navigation shell
└─ Discovery context
   ├─ “Browsing events in {Area}” dropdown trigger
   ├─ Dropdown action: “Use my current location”
   ├─ Dropdown action: “Browse online events”
   └─ All | In-person | Online | This weekend quick filters
└─ Featured events hero carousel
   ├─ blurred/covered event image + readable gradient
   ├─ foreground image, type/format badges, title, date, organizer, summary
   ├─ event link, previous/next, position
   └─ compact mobile disclosure
└─ Upcoming in {Area}
   └─ DetailedList cards in a 1/2/3-column responsive grid
└─ Community spotlight (only with explicit tenant curation or primary-actor scope)
   └─ up to 3 SingleRow cards, one per row
└─ Most viewed in {Area}
   └─ CompactGrid horizontal rail
└─ Most viewed online
   └─ CompactGrid horizontal rail
└─ Explicit tenant-curated sections (omit when absent)
   └─ CompactGrid horizontal rail
└─ Recently added
   └─ CompactGrid horizontal rail
└─ Existing tenant-configurable footer
```

### 3.2 Data Selection Rules

One public composite request returns all bounded sections. The Application handler resolves the tenant, selected active discovery area, mode, and explicit curated scope; calls existing event-list query semantics server-side; deduplicates and backfills in priority order; and returns safe per-section status. Area mode applies the configured area's internal location IDs. Online mode removes area location IDs and applies the online format while retaining the saved area ID for return.

| Section | Query | Limit | Notes |
|---|---|---:|---|
| Hero | upcoming, `sortBy=views`, descending | 10 | “Featured events” is presentation copy, not a recommendation claim; prefer images only as a secondary deterministic choice. |
| Upcoming in area | area location IDs, `dateFrom=now`, `dateTo=now+7d`, `sortBy=date`, ascending | 12 | Label with the selected area's display name; never “near you.” |
| Community spotlight | explicit tenant-curated preset or available primary actor only | 3 | Omit when no evidence exists; never fall back to views. |
| Most viewed in area | in-person/hybrid format IDs, area location IDs, `sortBy=views`, descending | 12 | “Most viewed” states the actual signal. |
| Most viewed online | online format ID, upcoming, `sortBy=views`, descending | 12 | No “trending,” trust, or recommendation claim. |
| Curated sections | explicit typed tenant preset only | bounded by config/max 12 | Omit unsupported labels; no `createdat` substitute for “grassroots.” |
| Recently added | `sortBy=createdat`, descending | 12 | Public upcoming events only. |

Area initialization is deterministic: valid `/home?area={guid}` state, then saved `home_discovery.area_id`, then the tenant's default active area, then its first active area, then all-area mode. Selecting “Use my current location” requests browser permission at that moment, compares the temporary origin with coarse `PublicDiscoveryAreaDto` centroids, persists only the selected area ID/mode, updates the canonical URL, and requests one new composite payload. Selecting “Browse online events” changes the label to “Browsing online events,” applies online mode, and preserves the saved area ID. No first-release response contains event distance.

### 3.3 Public Contracts

```text
PublicDiscoveryAreaDto
- Id (stable Guid)
- DisplayName
- City
- CountryCode
- CentroidLatitude / CentroidLongitude (coarse public area point)
- IsActive

HomeDiscoveryDto
- Context (selected/default area, available areas, mode)
- Hero
- UpcomingInArea
- Spotlight (optional)
- MostViewedInArea
- MostViewedOnline
- CuratedSections (explicit only)
- RecentlyAdded
- SectionStatuses
- GeneratedAtUtc

EventDiscoveryItemDto
- Event (normal public EventListDto representation)
- DistanceMeters (null in area-only release)
- NearestSessionId / NearestLocationId / NearestLocationName (null now)
- NearestOccurrenceStartsAtUtc (null now)
```

Tenant configuration uses a versioned `public_experience.discovery_areas` document. Its internal area config additionally maps each area to tenant-local location IDs and identifies one default. The public DTO exposes only coarse area data, never exact venue coordinates or address-derived points.

Inside the composite handler, a simple priority-ordered `HashSet<Guid>` removes duplicates and bounded overfetch backfills each section. A section remains visible if it has data; otherwise it uses a bounded empty state or is omitted. One section failure must not blank the whole home page.

### 3.4 Responsive Behavior

| Width | Hero | Upcoming in area | Single row | Rails |
|---|---|---|---|---|
| 1280+ | 440-520px, full metadata, cover + background | 3 columns | 1 full-width card | 4-5 event cards plus clipped preview |
| 768-1279 | 400-460px, shortened summary | 2 columns | 1 full-width card | 2-3 cards plus preview |
| 375-767 | 320-380px, no long summary, retained badges/date/title/control labels | 1 column | existing card container adaptation | 1-1.5 larger event cards plus preview |

Use CSS container queries for event-card composition and viewport queries only for page-level hero/shell behavior. Use logical properties for RTL.

### 3.5 Interaction And Accessibility

- Hero previous/next controls are real buttons with localized `aria-label`s and minimum target size.
- Rotation is manual by default; swipe/touch and buttons update the counter without an autoplay timer.
- `prefers-reduced-motion` removes nonessential transitions.
- Current slide is announced politely without reading the whole page again.
- Rails use native horizontal overflow and `scroll-snap`; no new carousel library or custom drag engine.
- Rail containers have names and keyboard-scrolling semantics; cards remain independently focusable.
- The production event card gains keyboard activation for its card-body navigation without breaking nested share/HAL actions.
- One page `h1` describes event discovery; all section titles are sequential `h2`s.
- Images use event titles as alt text when informative and empty alt text for duplicated blurred backgrounds.

## 4. Non-Negotiable Constraints

1. Keep tokens and auth credentials behind the BFF; browser code uses existing services.
2. Keep API authorization and tenant filtering authoritative.
3. Gate edit/delete actions through HAL links only; do not inspect roles/claims in the card or home.
4. Do not modify `EventApiClient.g.cs` by hand.
5. Do not add a compatibility shim or a new carousel dependency.
6. Every new file starts with two ABOUTME lines.
7. Every Razor component has colocated isolated CSS; BEM naming and logical properties are required.
8. Use MudBlazor v9 APIs and existing wrappers/tokens.
9. Preserve `/home` PublicSeo render-policy and prerender/hydration safety with `[PersistentState]`.
10. Maintain safe HTML encoding for tenant content; do not render configured block text as raw markup.
11. Do not claim “near you,” “verified,” “recommended for you,” or “free” unless the data selection proves it.
12. Do not change `/events` visual layout in this workstream.

## 5. Architecture And Design Decisions

### Decision 1: `/home` becomes discovery; `/welcome` remains marketing

- **Why:** It removes auth-dependent page identity and aligns the requested route with event discovery.
- **Alternatives considered:** Keep separate authenticated/anonymous home designs; redesign `/events`; merge `/welcome` and `/home`.
- **Consequences:** `LandingPageForUsers` becomes obsolete and can be deleted after tests prove no call sites. `LandingPageForNonUsers` remains for `/welcome`.
- **Files/layers:** `Home.razor`, `Home.razor.css`, `HomeStart.razor` tests, landing components, routes tests.

### Decision 2: Reuse the production three-mode event card

- **Why:** It already owns field visibility, image fallback, past/moderated state, share, and HAL actions.
- **Alternatives considered:** Continue the presentation-only card; create separate home cards.
- **Consequences:** Refactor `EventHorizontalRail` to use `Pages.Events.Components.EventCard`; delete the unused duplicate presentation card.
- **Files/layers:** Blazor components and tests only.

### Decision 3: Native overflow rails, MudBlazor hero

- **Why:** Native `overflow-x` + `scroll-snap` covers shelves; existing `MudCarousel` covers the hero without a dependency.
- **Alternatives considered:** Swiper package; custom JavaScript track; MudCarousel for every shelf.
- **Consequences:** Less JavaScript and fewer state bugs. Pagination dots are optional and should not be added unless they expose meaningful page state.

### Decision 4: Transparent deterministic ranking

- **Why:** Current data proves views, dates, formats, locations, and creation time only.
- **Alternatives considered:** Client heuristic or opaque quality score.
- **Consequences:** Section names and sort behavior remain honest. A future trust/ranking project needs a separate PRD and API contract.

### Decision 5: Stable public discovery areas, not generic venue coordinates

- **Why:** Area selection and exact event proximity are different capabilities; `LocationPii` must not become a generic public coordinate feed.
- **Alternatives considered:** add coordinates to `LocationListDto`; download all venue points; third-party/IP lookup; hardcoded Brussels.
- **Consequences:** A versioned tenant config defines stable area IDs, display data, coarse public centroids, and internal location mappings. URL/settings persist `area_id` and mode only. Browser geolocation is explicit and compares temporary origin only with coarse centroids.

### Decision 6: No ad or ad-shaped content break

- **Why:** The user explicitly rejected MangaDex's ad region.
- **Alternatives considered:** tenant CTA, promotional block, reserved whitespace, or direct feed transition.
- **Consequences:** The hero flows directly into “Upcoming in {Area}” with ordinary design-system section spacing. Existing organization/public-experience content remains available only in its established branch; no new promotional insert is created for discovery home.

### Decision 7: Preserve organization-centric safety branch

- **Why:** Tenant public-experience configuration and remediation are implemented product contracts.
- **Alternatives considered:** Replace all modes with fixed discovery feeds.
- **Consequences:** Organization branding/actor scope informs the hero and spotlight when available; missing primary organization still renders the safe remediation view.

### Decision 8: One composite public-home read

- **Why:** One network request centralizes tenant filtering, caching, deduplication, backfilling, section status, and later PostGIS integration.
- **Alternatives considered:** six or seven Blazor-composed event calls; one oversized generic event query; client-side dedupe.
- **Consequences:** `GET /api/public-experience/home?areaId={id}&mode={mode}` returns `HomeDiscoveryDto`, varies cache by tenant/area/mode, and preserves ETag behavior. The Application handler reuses existing event-list query semantics and performs bounded server-side composition.

### Decision 9: Dedicated `HomeDiscoveryService`

- **Why:** `LandingPageService` remains a marketing-page service for `/welcome`; it should not become the primary discovery orchestrator.
- **Alternatives considered:** extend or rename the legacy service and disturb `/welcome`; call generated client directly from `Home`.
- **Consequences:** Add a small `IHomeDiscoveryService`/`HomeDiscoveryService` wrapper following current Blazor test/DI conventions. Do not extend `LandingPageService`; remove its authenticated-home caller with `LandingPageForUsers`.

### Decision 10: PostGIS is planned, not approximated

- **Why:** Exact proximity belongs to eligible future event-session occurrences and requires governed public points plus indexed geodesic queries.
- **Alternatives considered:** Haversine over downloaded venues; one distance on `Event`; provider abstraction or in-memory fallback.
- **Consequences:** The area-only release forbids “near you” and distances. A durable ADR records PostGIS `geography(Point, 4326)`, GiST indexing, `LocationDiscoveryPoint`, transient first-party POST origin, nearest eligible occurrence semantics, readiness, and self-hosting requirements. Phase 6 remains explicitly deferred.

## 6. Implementation Phases

The task IDs below are canonical and must match the context and checklist exactly.

### Phase 0: Approved Boundaries And Baseline

#### Task 0.1: Analyze The Whole MangaDex Homepage (complete)
- **Result:** Desktop/mobile hierarchy, carousel, dense grid, shelves, footer, and rejected ad region are recorded.
- **Validation:** Chrome DevTools evidence in context.

#### Task 0.2: Trace Current `/home` And Discovery Infrastructure (complete)
- **Result:** Routes, Home branches, production cards, prototypes, public-experience settings/API, services, tests, and render policy are source-grounded.
- **Validation:** Baseline Release build passed with 0 errors.

#### Task 0.3: Approve Geospatial And Location-Privacy Direction (complete)
- **Result:** First release is area-only; exact venue PII remains private; stable area IDs/coarse centroids replace city strings/venue enumeration; “near you” is forbidden; PostGIS is the sole planned exact engine.
- **Validation:** Sections 3, 5, 9, 12, and Phase 6 agree.

#### Task 0.4: Run Fresh Pre-Implementation Baseline
- **Files:** No edits; repository state and test output only.
- **Acceptance:** Build plus Domain, Application, API, Blazor Client, and Architecture projects pass or exact pre-existing failures are recorded; warning count is captured for a no-new-warning delta.
- **Dependencies/Effort:** 0.3 / M
- **Validation:** Commands in Section 7.

### Phase 1: Durable Design And Planned-Architecture Contracts

#### Task 1.1: Extend The Public Discovery Design Contract
- **Files:** `docs/DESIGN.md`
- **Acceptance:** Page order, area selector, manual hero, layout-mode mapping, no-ad rule, honest labels, responsive/RTL/motion/accessibility, and image-loading rules are explicit.
- **Dependencies/Effort:** 0.4 / S
- **Validation:** Docs review and architecture context tests.

#### Task 1.2: Document Planned PostGIS Proximity Discovery
- **Files:** Next available `docs/adr/ADR-*-postgis-proximity-discovery.md`, plus summaries in `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and `docs/SELF_HOSTING.md`
- **Acceptance:** Clearly marked **Planned, not implemented**; defines `LocationPii` vs governed `LocationDiscoveryPoint`, `geography(Point,4326)`, GiST, eligible future session distance, transient POST origin/privacy, cache/readiness, and self-hosting operations.
- **Dependencies/Effort:** 0.3 / M
- **Validation:** Links resolve; no documentation claims the capability currently exists; context tests pass.

### Phase 2: Component Consolidation

#### Task 2.1: Make Production `EventCard` Keyboard-Safe
- **Files:** Production `EventCard.*`, `EventCardTests.cs`
- **Acceptance:** Card-body Enter/Space navigation works once in all three modes; nested share/edit/delete remain isolated and HAL-gated.
- **Dependencies/Effort:** 1.1 / S
- **Validation:** Focused and full Blazor tests.

#### Task 2.2: Refactor The Manual Hero
- **Files:** Existing `HeroCarousel`/`FeaturedEventHero`, isolated CSS, focused tests
- **Acceptance:** Up to 10 slides, previous/next/swipe/counter, no autoplay, safe fallback, active image prioritized, next optionally preloaded, remainder lazy, compact mobile/RTL/reduced-motion behavior.
- **Dependencies/Effort:** 1.1, 2.1 / M
- **Validation:** Component tests and browser keyboard/image-network smoke.

#### Task 2.3: Refactor The Native Horizontal Rail
- **Files:** Existing `EventHorizontalRail`, isolated CSS/tests, duplicate presentation `EventCard` deleted only after reference proof
- **Acceptance:** Production `CompactGrid` cards, native scroll-snap, clipped preview, semantic heading/View all, keyboard/touch/RTL, no new dependency or duplicate card.
- **Dependencies/Effort:** 2.1 / M
- **Validation:** Focused tests, `rg` caller proof, build.

### Phase 3: Area-Only Composite Discovery Contract

#### Task 3.1: Add The Discovery-Area Contract
- **Files:** Governance keys, `PublicExperienceSettingDefinitions`, new `PublicDiscoveryAreasConfig`, public DTOs, serialization context, setting/config tests
- **Acceptance:** Versioned tenant config has stable Guid, display name, city, country code, coarse centroid, active/default flags, and internal tenant location IDs; user preferences are `home_discovery.area_id` and `home_discovery.mode`; no `LocationListDto` or `LocationPii` change.
- **Dependencies/Effort:** 0.4, 1.2 / M
- **Validation:** Domain/Application tests for invalid, duplicate, default, and tenant-boundary cases.

#### Task 3.2: Add The Composite Home-Discovery Query And API
- **Files:** `HomeDiscoveryDto`/`EventDiscoveryItemDto`, query/handler, `PublicExperienceController`, `RouteNames`, cache policy, API/Application tests, regenerated OpenAPI/inventory/NSwag client/changelog
- **Acceptance:** One `GET /api/public-experience/home?areaId={guid}&mode={mode}` returns bounded context/sections/statuses; tenant and published/public rules remain authoritative; server performs deterministic dedupe/backfill; unsupported curated sections are omitted; cache varies by tenant/area/mode and retains ETag behavior.
- **Dependencies/Effort:** 3.1 / L
- **Validation:** Handler/API tests, deterministic contract regeneration, one-call/payload budgets.

#### Task 3.3: Add The Frontend Discovery Context
- **Files:** New `IHomeDiscoveryService`/`HomeDiscoveryService`, DI, `IUserSettingsService` use, `wwwroot/js/home-location.js`, small area-distance helper/model, tests
- **Acceptance:** URL/saved/default resolution uses area ID; current-location action is hidden/disabled when no active centroid areas exist; geolocation occurs only after explicit action and compares with public area centroids; denied/unavailable keeps prior context; online mode preserves area; `LandingPageService` is not extended and remains `/welcome`-only.
- **Dependencies/Effort:** 3.2 / M
- **Validation:** Service/default-order/JS interop tests and permission-denied browser smoke.

#### Task 3.4: Add Explicit Location-Privacy Tests
- **Files:** Setting/config, DTO serialization, API response, JS interop, Home tests
- **Acceptance:** Generic location DTOs expose no coordinates; area centroids are coarse config data; user origin never enters URL, settings, persistent state, logs, analytics, errors, or outbound API payload in area-only mode; Permissions-Policy permits geolocation only for self.
- **Dependencies/Effort:** 3.1, 3.3 / M
- **Validation:** Serialization/API tests, source/log assertions, browser policy inspection.

#### Task 3.5: Compose And Persist Homepage State
- **Files:** `Home.*`, Home tests
- **Acceptance:** One composite payload supplies all sections; `[PersistentState]` avoids hydration duplicates; no origin is persisted; section failures remain bounded; cancellation prevents late writes; anonymous/authenticated discovery composition is identical.
- **Dependencies/Effort:** 2.2, 2.3, 3.3, 3.4 / M
- **Validation:** Home prerender/hydration/partial-failure tests and request-count assertion.

### Phase 4: `/home` Composition

#### Task 4.1: Replace Discovery-Centric Home Branches
- **Files:** `Home.*`, `LandingPageForUsers.*`, Home/route tests
- **Acceptance:** `/home` and startup-selected `/` use discovery for all visitors; `/welcome` remains marketing; organization-centric remediation/encoding stays; obsolete authenticated landing component is removed after proof.
- **Dependencies/Effort:** 3.5 / M
- **Validation:** Home/route tests and build.

#### Task 4.2: Render Area Context Directly Above The Hero
- **Acceptance:** “Browsing events in {Area}”/“Browsing online events,” the two requested dropdown actions, one h1, manual hero, theme contrast, and no ad/CTA/reserved break.
- **Dependencies/Effort:** 4.1 / M
- **Validation:** bUnit, keyboard, contrast, permission states.

#### Task 4.3: Render All Three Honest Event Layouts
- **Acceptance:** `DetailedList` renders “Upcoming in {Area}” at 1/2/3 columns; optional curated/actor `SingleRow` spotlight is omitted without evidence; `CompactGrid` rails use “Most viewed in {Area},” “Most viewed online,” explicit curated labels, and “Recently added.” No “near,” “trending,” “recommended,” or unsupported community/grassroots copy.
- **Dependencies/Effort:** 4.2 / M
- **Validation:** Content/layout tests and responsive screenshots.

#### Task 4.4: Finish Loading, Empty, And Partial-Failure States
- **Acceptance:** Stable skeletons, polite announcements, safe empty/error copy, `/events` fallback, successful sections survive one failed section.
- **Dependencies/Effort:** 4.3 / S
- **Validation:** Failure-path tests.

#### Task 4.5: Complete Localization And RTL
- **Acceptance:** Every new label, action, error, section heading, counter, and accessibility label uses the existing translation path with fallback; area display data remains data; RTL order/focus is verified. Future distance units are documented but not rendered now.
- **Dependencies/Effort:** 4.4 / S
- **Validation:** Localization-key coverage and RTL browser smoke.

### Phase 5: Verification And Handoff

#### Task 5.1: Add Aspire-Backed Public Home Flow
- **Acceptance:** Route, one composite discovery call, area/online actions, granted/denied geolocation, manual hero, keyboard cards, rails, URL state, no coordinate leakage, and console cleanliness.
- **Dependencies/Effort:** 4.5 / M
- **Validation:** E2E project or documented environment blocker plus manual Chrome evidence.

#### Task 5.2: Run Responsive Visual And Accessibility QA
- **Acceptance:** 375/768/1280, light/dark, RTL, reduced motion, long title, no image, empty/partial failure, no overflow, lazy-image behavior, and no ad gap.
- **Dependencies/Effort:** 5.1 / M
- **Validation:** Chrome screenshots/network/console evidence in context.

#### Task 5.3: Run Quality, Contract, And Performance Gates
- **Acceptance:** Required projects pass; generated artifacts are deterministic; no new warnings over Task 0.4; initial discovery uses one API call; response is at most 256 KiB uncompressed/120 KiB compressed; each section times out safely at 1 s and composite execution at 3 s; controlled uncached p95 target is 800 ms, cached p95 200 ms, page LCP target 2.5 s; at most two hero images load eagerly and initial hero bytes stay within 500 KiB.
- **Dependencies/Effort:** 5.2 / M
- **Validation:** Section 7 commands, contract drift, browser network evidence, controlled performance run.

#### Task 5.4: Refresh Canonical And Dev Docs
- **Acceptance:** DESIGN/BLAZOR/API docs, plan/context/tasks, actual files, budgets, tests, and remaining Phase 6 scope agree.
- **Dependencies/Effort:** 5.3 / S
- **Validation:** Cold-resume review and link checks.

#### Task 5.5: Decide Whether To Add A General Public-Blazor Intent
- **Acceptance:** Decision recorded; any intent change is isolated and passes context schema/duplication tests.
- **Dependencies/Effort:** 5.4 / S
- **Validation:** Architecture context tests if changed.

### Phase 6: PostGIS Proximity Discovery (planned later, not current release)

#### Task 6.1: Approve The Geospatial ADR And Privacy Contract
- Define `disabled`, `area_only`, and `postgis` modes, governed public points, occurrence distance, cache/privacy/readiness rules.

#### Task 6.2: Add PostGIS Capability And Spatial Persistence
- Canonical migration, `geography(Point,4326)`, GiST/tenant indexes, approved-point backfill, readiness, self-hosting operations; no generic provider or in-memory fallback.

#### Task 6.3: Add Nearby Event CQRS Query And API
- First-party POST with transient rounded origin/radius/filters; future public sessions only; `ST_DWithin`; minimum event-occurrence distance; stable distance/time/event cursor; private no-store response.

#### Task 6.4: Integrate Distance Metadata Into Discovery UI
- Populate `EventDiscoveryItemDto` distance/nearest-occurrence fields, localize metres/kilometres, and enable “near you” only for valid PostGIS results.

#### Task 6.5: Verify Privacy, Correctness, Performance, And Operations
- Real PostGIS integration tests for tenant isolation; draft/private/unlisted/member visibility; nearest future session across multi-location events; past exclusion; hybrid vs online-only; just-inside/on/outside radius; missing points; stable distance/time/event ordering; cross-border and duplicate-city cases; no origin leakage/shared cache; `EXPLAIN ANALYZE` index evidence; readiness/degraded state; backup/restore/index docs.

## 7. Testing Strategy

| Requirement | Test layer | Coverage |
|---|---|---|
| Setting registration and area-config validation | Domain/Application unit | Stable/duplicate IDs, country/display fields, exactly one default, inactive/foreign location IDs, centroid bounds, user setting scopes. |
| Composite section semantics | Application unit | Exact `date`/`views`/`createdat` keys, area/mode filters, honest labels, optional curation, dedupe/backfill, cancellation, partial status. |
| Public API/cache contract | API integration | Anonymous tenant isolation, query validation, one composite response, cache vary by tenant/area/mode, ETag, generated contract. |
| Location privacy | Domain/API/Blazor/browser | No `LocationListDto` coordinates; no origin in URL, settings, state, logs, analytics, errors, or requests in area-only mode; geolocation self policy. |
| Three card modes and keyboard behavior | bUnit | Existing mode coverage plus Enter/Space and nested HAL/share isolation. |
| Manual hero and image loading | bUnit/browser | Counter/wrap/fallback/labels, no autoplay, active/next/lazy network behavior. |
| Rail semantics and overflow | bUnit/browser | Production `CompactGrid`, empty/skeleton/View all, touch/keyboard/RTL/no page overflow. |
| Home branch and hydration | bUnit | Auth/anonymous parity, organization remediation/encoding, one composite call, persistent state, failure isolation. |
| Honest terminology | bUnit/API | No “near,” distance, “recommended,” “trending,” or unsupported community/grassroots labels. |
| Accessibility/localization | architecture/bUnit/browser | h1/h2, controls, targets, translations/fallbacks, RTL, reduced motion, focus. |
| Real route behavior and budgets | E2E/manual | Aspire home flow, area/online actions, granted/denied location, request/payload/image/LCP evidence. |

Required before implementation and again before completion:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Manual/nightly after Aspire is available:

```bash
dotnet run --project src/Explore.AppHost/Explore.AppHost.csproj
dotnet test --project tests/Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Do not run solution-level `dotnet test`. Task 0.4 records the existing warning count; Task 5.3 fails on new warnings attributable to this work.

## 8. Documentation, Configuration, And Operations Impact

- **Current-release docs:** update `docs/DESIGN.md`, `docs/BLAZOR.md`, `docs/API_CHANGELOG.md`, `docs/API_CONTRACT_INVENTORY.md`, and generated OpenAPI/NSwag artifacts.
- **Planned-capability docs before UI work:** create the PostGIS ADR and summarize its planned status in `docs/ARCHITECTURE.md`, `docs/DOMAIN.md`, and `docs/SELF_HOSTING.md`.
- **Area-only configuration:** add versioned `public_experience.discovery_areas` tenant config and user-scoped `home_discovery.area_id`/`home_discovery.mode`; no environment variable, package, service, or migration.
- **Current operations:** one composite discovery request beyond shell/bootstrap, query-varying output cache, ETag, bounded section statuses, and explicit payload/latency/image budgets.
- **SEO:** retain PublicSeo render policy and canonical `/home?area={guid}&mode={mode}`; origin never appears in a URL.
- **PostGIS operations:** explicitly deferred to Phase 6; migration, extension readiness, backup/restore, index health, missing-point counts, query plans, and managed PostgreSQL operator requirements must ship with that phase.

## 9. Security, Authorization, Privacy, And Abuse

| Coordinate/data type | Area-only release | Planned PostGIS phase |
|---|---|---|
| User origin | Browser memory only after explicit action | Rounded/bounded transient first-party POST body after explicit action |
| Discovery-area centroid | Public coarse tenant config | Public coarse tenant config |
| Exact venue point | Never returned generically | Governed `LocationDiscoveryPoint` only when visibility permits |
| Persisted preference | Area ID and mode only | Area ID, mode, optional radius only |
| URL/query | Area ID and mode; never origin | Never origin |
| Logs/traces/analytics | No origin or exact private point | No origin or exact private point |
| Cache | Shared by tenant/area/mode | Exact-origin nearby response is private, no-store |

Additional invariants:

- API/BFF tenant resolution and published/public visibility remain authoritative; UI never broadens results.
- Edit/delete affordances remain HAL-gated, never role/claim-gated.
- `LocationPii` stays the exact address/private operational boundary.
- `PublicDiscoveryAreaDto` contains coarse configured area centroids, not venue-derived points.
- Geolocation permission is requested only by the explicit action; `Permissions-Policy` permits self only.
- Tenant content stays Razor-encoded; errors expose no provider/payload details.
- View-sorted labels say “Most viewed”; they do not imply quality, trust, recommendation, or abuse resistance.

## 10. Cross-Cutting Product Considerations

| Concern | Plan |
|---|---|
| Multi-tenancy | Composite query validates area and internal location IDs within the resolved tenant; API visibility remains authoritative. |
| Localization | Task 4.5 owns every new string/fallback/accessibility label; area display names are data, not persisted identity. |
| RTL/accessibility | Logical CSS, manual hero, focus order, keyboard rails/cards, target sizes, contrast, announcements, reduced motion. |
| White-label/self-hosting | Semantic tokens; no MangaDex assets, ad provider, external geocoder, or geospatial provider abstraction. |
| Federation | No new federation reads; only tenant-public events already exposed by the API participate. |
| Analytics | No new location tracking; origin and exact private points are forbidden properties. |
| Curated content | Spotlight/community/grassroots require explicit typed curation or actor scope; otherwise omit. |
| Compatibility | `/welcome` remains marketing; `/events` is unchanged visually; startup-selected `/` intentionally follows `Home`. |

## 11. Observability, Budgets, And Image Policy

### Enforceable Current-Release Budgets

| Budget | Gate |
|---|---|
| Initial discovery data | Exactly one composite home API call beyond existing shell/bootstrap reads |
| Response size | At most 256 KiB uncompressed and 120 KiB compressed |
| API latency | Controlled p95 target: 800 ms uncached, 200 ms cached |
| Per-section execution | 1 s hard timeout with a safe section status |
| Composite execution | 3 s hard maximum before a safe endpoint failure |
| Page LCP | 2.5 s target on defined mid-tier mobile/4G profile |
| Hero eager images | Active slide only; optionally next slide; maximum two eager images |
| Initial hero image bytes | At most 500 KiB total |
| Other images | Below-fold hero/rail images lazy |
| Warning delta | No new warnings attributable to the change |

- The composite handler emits safe section/status timing and overall duration, never origins, exact private points, payloads, or raw exceptions.
- One section failure produces a bounded status while successful sections remain.
- Existing cache tags must be invalidated when public-experience area/config or event-list data changes.
- Exact PostGIS nearby responses later use private no-store and cannot share the area-only cache.

## 12. Compatibility And Planned PostGIS Architecture

### Current Release

- No database migration and no `LocationListDto` coordinate change.
- Additive composite public-home and discovery-area contracts are regenerated through the canonical workflow.
- `LandingPageService` remains for `/welcome`; new home code uses `HomeDiscoveryService`.
- `LandingPageForUsers` and duplicate presentation `EventCard` may be removed only after caller proof/build/tests.
- No first-release UI or API claims exact proximity or displays distance.

### Planned: PostGIS-Based Proximity Discovery

**Status: Planned, not implemented in the current `/home` release.**

PostGIS is the sole planned exact-proximity engine. Public discovery points use `geography(Point,4326)` with a GiST index, following [Npgsql NetTopologySuite spatial mapping](https://www.npgsql.org/efcore/mapping/nts.html). `LocationPii` keeps exact private operational data; a separate governed `LocationDiscoveryPoint` carries visibility (`Hidden`, `AreaOnly`, `Approximate`, `ExactPublic`), precision, source, and verification time.

The planned capability states are `disabled` (no current-location action), `area_only` (coarse browser area matching, no distances), and `postgis` (exact occurrence radius/distance). Configured-but-unavailable PostGIS is a readiness failure/degraded capability, never a silent fallback.

Distance belongs to an eligible future public event-session occurrence, never directly to the persistent `Event`. The future query:

1. resolves tenant/publication/visibility/soft-delete rules;
2. selects future physical sessions only;
3. joins governed public discovery points;
4. applies radius with [`ST_DWithin`](https://postgis.net/docs/ST_DWithin.html);
5. derives minimum distance and nearest occurrence per event;
6. orders by distance, occurrence start, then event ID;
7. returns canonical metres through `EventDiscoveryItemDto`.

The future `POST /api/public-experience/home/nearby` accepts a rounded/bounded transient origin in the body, never URL/log/state/settings/analytics, and returns private no-store results. Online-only events have no distance; hybrid events participate only through eligible physical sessions. When configured PostGIS is unavailable, readiness degrades/fails clearly and never falls back to an in-memory dataset scan.

## 13. Risk Register

| Risk | Impact | Mitigation | Owner |
|---|---:|---|---|
| Area wording drifts into proximity claims | High | Forbidden-term tests; only Phase 6 enables “near you”/distance | 3.2, 4.3 |
| Generic venue PII becomes public | High | Dedicated area DTO/config; serialization/API tests; no `LocationListDto` edit | 3.1, 3.4 |
| Composite handler is slow | High | Bounded overfetch/dedupe, cache/ETag, explicit p95/payload gates, timing | 3.2, 5.3 |
| Anonymous saved area causes hydration refetch | Medium | Canonical URL and persistent payload are primary; any post-hydration preference correction is one explicit composite reload and tested | 3.3, 3.5 |
| Invalid/cross-tenant area selection | High | Server validates active area in resolved tenant and falls back safely | 3.1, 3.2 |
| View count is mistaken for quality | Medium | “Most viewed” only; no trending/recommended label | 4.3 |
| Curated label lacks evidence | Medium | Omit section unless explicit typed curation/actor scope exists | 3.2, 4.3 |
| Geolocation denied/inaccurate | Medium | Explicit action, coarse nearest-area selection, keep prior area, safe feedback | 3.3 |
| Hero harms LCP | High | Manual rotation, ≤2 eager images, 500 KiB budget, lazy remainder | 2.2, 5.3 |
| Organization-centric behavior regresses | High | Preserve remediation/encoding tests | 4.1 |
| Shared dirty worktree hides attribution | Medium | Scope diffs, warning delta, never revert unrelated edits | Every task |
| PostGIS roadmap is mistaken as shipped | High | ADR/status language says Planned; readiness/routes absent until Phase 6 | 1.2, 5.4 |

## 14. Definition Of Done And Manual QA

### Functional

- `/home` and startup-selected `/` render discovery for anonymous/authenticated users; `/welcome` remains marketing.
- One composite response supplies area context and all supported sections.
- Dropdown above the hero provides the two requested actions and persists stable area ID/mode.
- Current-location action uses only coarse area centroids and makes no distance claim.
- Manual hero and all three existing card layouts work with honest section labels.
- Unsupported curated sections are omitted; one failed section does not blank the page.
- No ad, CTA substitute, or reserved ad gap exists.

### Quality

- No generic location coordinate exposure, user-origin persistence/leakage, new dependency, raw HTML, role/claim affordance logic, duplicate presentation card, or hand-edited generated artifact.
- Required build/tests, deterministic contract generation, no-new-warning delta, performance/image/request budgets, localization, RTL, accessibility, and browser console/overflow gates pass.
- Planned PostGIS architecture is durable documentation, clearly separated from current behavior.

### Manual QA Gate

1. Inspect `/home` at 375, 768, and 1280 CSS pixels.
2. Confirm one composite discovery request and payload budget.
3. Open the area dropdown; test online, granted, denied, unavailable, and inaccurate/no-match states; inspect URL/storage/network/log surfaces for origin leakage.
4. Drive manual previous/next/swipe, rails, cards, share, HAL actions, and View all using pointer and keyboard.
5. Verify light/dark, RTL, reduced motion, long titles, missing images, empty/failed sections, and no page overflow/ad gap.
6. Inspect image waterfall: active only plus optional next eager, remainder lazy.
7. Capture screenshots, network/console, timing, warning delta, and unresolved evidence in context.

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

1. Read plan, context, and tasks before each slice; use their identical canonical task IDs.
2. Start at the highest-priority incomplete current-release task; Phase 6 never starts without a separate explicit approval.
3. Update plan for scope/architecture, context for evidence/decisions/blockers, and tasks immediately after each slice.
4. Never describe area selection as proximity or roadmap behavior as implemented.
5. Regenerate OpenAPI/inventory/NSwag artifacts through the canonical workflow only.
6. Record exact validation, warning delta, browser evidence, budgets, remaining work, and next task before handoff.
7. Never include unrelated shared-worktree changes.

## 16. Progress Reporting Contract

- **Implemented:** named task ID, concrete files/contracts/components, data/control flow, privacy/accessibility behavior.
- **Verified:** exact commands, generated-contract result, request/payload/latency/image evidence, browser states.
- **Remaining:** incomplete current tasks and separately labeled Phase 6 roadmap.
- **Next:** one canonical task ID.
- **Docs updated:** exact files and why.

## 17. Potential Risks & Unknowns

The approved first release is deliberately area-level. A configured discovery area needs stable identity, coarse centroid, default/active state, and a tenant-local mapping to eligible locations; administrative UX for editing that JSON may be deferred if the existing settings document workflow can safely manage it. The composite handler still performs several bounded internal event queries, so controlled latency/payload evidence is mandatory even though the browser sees one call. Exact venue proximity, radius, distances, and “near you” remain Phase 6 PostGIS work against future public event-session discovery points, not an excuse to expose `LocationPii` or add client-side venue scans.
