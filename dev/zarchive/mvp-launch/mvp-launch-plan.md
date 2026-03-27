# MVP Launch Plan — ISLAMU Event Platform

Last Updated: 2026-03-21

## Executive Summary

The ISLAMU Event platform is **~85% MVP-ready**. After a thorough audit of 56 API controllers, 24+ routable Blazor pages, and the full domain model, the core user flows — event CRUD, registration, organization management, auth, onboarding, admin — are implemented with high polish.

The remaining work is **public web delivery**: making events shareable, discoverable, and deployable. This plan treats that work not as a simple UI feature but as a **public-platform architecture concern** with explicit rules for canonical routing, anonymous visibility, crawler-visible metadata, tenant-aware branding, and deployment validation.

**Success metric**: A mosque event organizer can create an event → get a shareable link → send it on WhatsApp → attendee sees a rich preview card → clicks → views the event → registers — all within 5 minutes.

---

## Current State Analysis

### ✅ Already Working (Confirmed from codebase)

| Capability | Status | Evidence |
|---|---|---|
| Event CRUD | ✅ | `CreateEvent.razor` (41KB), `EventEdit.razor` (35KB), `EventController.cs` (23KB) |
| Event list with rich filtering | ✅ | `EventList.razor` (81KB), virtualization, image preloading, tri-state tag/category |
| Event detail with sidebar | ✅ | `EventDetail.razor` (577 lines), sessions, agenda, aspects, registration |
| Event sessions & agenda | ✅ | Multi-session support, session editor drawer, agenda timeline |
| Event registration/cancellation | ✅ | Single/multi-session registration, cancel flow, status tracking |
| Organization CRUD & members | ✅ | Create, details, profile, members, reviews, shared contacts |
| Landing pages | ✅ | Authenticated and non-authenticated variants |
| Auth (OIDC/Keycloak BFF) | ✅ | BFF pattern, OIDC, token forwarding, route guards |
| Instance + Tenant onboarding | ✅ | Multi-step wizards |
| Admin settings | ✅ | Instance, tenant, organization, group settings pages |
| Multi-tenancy | ✅ | Runtime mode, tenant resolution, query filters |
| S3 image storage | ✅ | Pre-signed URL upload/download, per-tenant S3 config |
| HATEOAS/HAL API | ✅ | 56 controllers, resource assemblers, link policies |
| Analytics & cookie consent | ✅ | PostHog, consent state machine, relay mode |
| Rate limiting & security | ✅ | 4-tier rate limiting, ETag, security headers |
| Event appearance/branding | ✅ | Background color, image, effects per event |
| Islamic & Tech aspects | ✅ | Layer 2 typed schema, CRUD, display |
| Runtime render policies | ✅ | 5 presets, per-route-group, tenant-delegable, `PublicSeo` group |
| Event status model | ✅ | Draft(1), Published(2), Cancelled(3), Completed(4), Archived(5) |

---

## Proposed Changes — Prioritized

### P0 — Public Event Page Foundation (ARCHITECTURAL)

**This is not a feature — it is the infrastructure all sharing depends on.**

#### P0.1 — Centralized Public URL Builder

Create a service that is the single source of truth for all external-facing URLs.

**Why**: Share buttons, OG tags, calendar links, emails, and future federation all need absolute, tenant-aware, reverse-proxy-aware URLs. Without centralization, every consumer will assemble URLs differently and break in production.

##### [NEW] `IPublicUrlBuilder` interface (Application layer)
- `GetEventUrl(Guid eventId)` → absolute canonical URL
- `GetOrganizationUrl(Guid orgId)` → absolute canonical URL
- `GetBaseUrl()` → tenant/domain-aware site root
- All methods return absolute URLs with correct scheme, host, path base
- Reverse proxy / `X-Forwarded-Host` awareness

##### [NEW] `PublicUrlBuilder` implementation (Infrastructure layer)
- Resolves scheme/host from `HttpContext`, `X-Forwarded-*` headers, or governance settings
- Tenant-aware: uses tenant domain/subdomain when available
- Environment-overridable for Docker/proxy scenarios

#### P0.2 — Public Visibility Rules for Events

Define what anonymous users can see. Without this, a public route may leak draft/private content.

**Event visibility policy:**
- **Published** (status=2) = visible anonymously via public URL
- **Draft** (status=1) = visible **only** to the event creator (authenticated, ownership check)
- **Cancelled** (status=3) = visible publicly with "Cancelled" banner
- **Completed** (status=4) = visible publicly (historical record)
- **Archived** (status=5) = not publicly resolvable (404)
- **Unlisted** (new concept) = not discoverable in EventList, but accessible via direct URL if published

##### [MODIFY] `Event` domain entity
- Add `IsUnlisted` boolean property (default `false`)
- Unlisted events are published but excluded from list/search queries

##### [MODIFY] `EventList.razor.cs` / `GetEventsPagedAsync`
- Exclude Draft, Archived, and Unlisted events from public event list
- Only Published, Cancelled, and Completed events appear in discovery

##### [MODIFY] `EventDetail` page logic
- For Draft events: check ownership, return 404-equivalent for non-owners
- For Archived events: return 404-equivalent
- For Cancelled events: show event with "This event has been cancelled" banner
- For Unlisted + Published events: show normally (direct URL access works)

#### P0.3 — OG Image Proxy for Pre-Signed URLs

**Critical constraint**: Event images are stored in object storage and accessed via time-limited pre-signed URLs. Social crawlers cache OG images at scrape time, and pre-signed URLs expire. OG tags cannot use pre-signed URLs.

##### [NEW] Public image proxy endpoint (API layer)
- `GET /api/storage/{storageObjectId}/public` → serves image directly
- Tenant-scoped, validates the storage object exists and belongs to a published event
- Returns image bytes with appropriate `Content-Type` and cache headers
- This gives a stable, non-expiring URL for OG image tags
- Rate-limited to prevent abuse

**Fallback chain for OG image:**
1. Event featured image → public proxy URL
2. Organization profile image → public proxy URL
3. Tenant/site default share image (governance setting)
4. Global platform default image (static asset)

---

### P1 — Shareable Event Page with Crawler-Visible Metadata (CRITICAL)

**P1 and former P3 (OG meta tags) are merged into one deliverable.** From the user's perspective, "shareable link" and "preview card works" are the same feature.

#### Canonical URL

The canonical public event URL is: `/events/{id}`

This URL must:
- Work across all render modes (InteractiveServer, InteractiveAuto, InteractiveWebAssembly)
- Be handled by Blazouter routing (per user direction: stay with Blazouter always)
- Be in the `PublicSeo` route group (already includes `/event/detail/*`, update to `/events/*`)
- Support prerendering when `SeoBalanced` or `AllPrerendered` preset is active

#### Metadata Strategy

The solution must work with all runtime-configurable render modes per tenant:

**Approach — Server-side metadata injection via Blazor SSR + HeadContent:**
- Blazor's `<HeadContent>` component renders into the `<head>` during server-side rendering
- The `PublicSeo` route group already supports prerendering (`SeoBalanced` preset sets `public_seo.prerender_enabled = true`)
- Event data is loaded in `OnInitializedAsync` and `<HeadContent>` is populated with OG tags
- When prerender is enabled, the initial HTML response includes the metadata before hydration

**Fallback — If `HeadContent` does not reliably populate initial crawler-visible HTML:**
- Introduce a dedicated API endpoint that returns an HTML fragment with OG tags for a given event ID
- BFF middleware detects crawler user-agents on event URLs and serves this minimal HTML
- This is a contingency, not the default plan

##### [MODIFY] EventDetail page (route + meta tags)
- Update Blazouter route from `/event/detail/{eventId}` to `/events/{eventId}`
- Add `<HeadContent>` block with:
  - `<meta property="og:title">`
  - `<meta property="og:description">` (truncated to 200 chars, plain text)
  - `<meta property="og:image">` (public proxy URL, not pre-signed)
  - `<meta property="og:url">` (canonical absolute URL via `IPublicUrlBuilder`)
  - `<meta property="og:type" content="event">`
  - `<meta property="og:site_name">` (tenant name or platform name)
  - `<meta name="twitter:card" content="summary_large_image">`
  - `<meta name="twitter:title">`
  - `<meta name="twitter:description">`
  - `<meta name="twitter:image">`
  - `<meta name="description">`
  - `<link rel="canonical">`

##### [MODIFY] `RuntimeRenderPolicyService.ClassifyRouteGroup`
- Update `PublicSeo` route patterns: add `/events/*` alongside existing `/event/detail/*`

##### Fallback rules
- No image → tenant share image → platform default
- Empty description → truncated title + "Event on {platform name}"
- Title too long → truncate at 70 chars, preserve word boundary

---

### P2 — Share Action (CRITICAL)

##### [MODIFY] EventDetail.razor + EventDetail.razor.cs
- Implement Share button with:
  - **Primary**: Copy canonical URL to clipboard via `navigator.clipboard.writeText`
  - **Secondary**: Native Web Share API (`navigator.share`) when available on mobile
  - No platform-specific share links (no WhatsApp-specific, per user direction)
- Show snackbar confirmation ("Link copied to clipboard!")
- URL generated via `IPublicUrlBuilder.GetEventUrl(eventId)`

##### [MODIFY] EventList sidebar share
- Verify `CopyEventLinkAsync` uses the new canonical URL format
- Uses same `IPublicUrlBuilder`

---

### P3 — Draft Save & Event Visibility Controls (HIGH)

#### P3.1 — Save as Draft from Event Creation

##### [MODIFY] CreateEvent.razor
- Add dropdown arrow next to the "Create Event" submit button
- Dropdown contains "Save as Draft" option
- Default "Create Event" creates with status = Published
- "Save as Draft" creates with status = Draft
- After draft save, navigate to event edit page (not public detail page)

##### [MODIFY] CreateEvent.razor.cs
- Add `SaveAsDraft()` method that sets `EventStatusId = 1` before submission
- Reuse existing create flow, only changing the status parameter

##### [MODIFY] MyEvents.razor
- Show Draft events with a "Draft" badge
- Allow editing and publishing drafts
- Add "Publish" action for draft events

#### P3.2 — Unlisted Events

##### [MODIFY] Event domain entity + DTO
- Add `IsUnlisted` property
- Add to create/update DTOs
- Add toggle in EventEdit page: "Unlisted — only people with the link can see this event"

---

### P4 — URL Refactoring (HIGH)

Establish proper RESTful URL patterns. Current URLs are inconsistent and non-standard.

| Current Route | New Route | Page |
|---|---|---|
| `/myevents` | `/my/events` | My Events |
| `/my-registrations` | `/my/registrations` | My Registrations |
| `/organizations/my` | `/my/organizations` | My Organizations |
| `/user/reviews` | `/my/reviews` | My Reviews |
| `/create-event` | `/events/create` | Create Event |
| `/event/detail/{id}` | `/events/{id}` | Event Detail |
| `/event/edit/{id}` | `/events/{id}/edit` | Event Edit |
| `/organizations/create` | `/organizations/create` | *(keep — already good)* |

##### Implementation approach
- Update Blazouter route registrations
- Update all `Navigation.NavigateTo()` calls that reference old URLs
- Update `RuntimeRenderPolicyService.ClassifyRouteGroup` patterns
- Update `CopyEventLinkAsync` and any URL generation
- Keep old routes temporarily as redirects if needed for bookmarks

##### Files requiring NavigateTo updates
- `EventList.razor.cs` → `/event/detail/{id}` and `/event/edit/{id}`
- `EventDetail.razor.cs` → `/myevents`
- `EventEdit.razor.cs` → `/event/detail/{id}`
- `CreateEvent.razor.cs` → `/event/detail/{id}`, `/organizations/create`, `/groups/create`
- `MyEvents.razor.cs` → `/event/edit/{id}`, `/event/detail/{id}`

---

### P5 — Calendar Integration (HIGH)

##### [MODIFY] EventDetail.razor.cs
- Implement `AddToGoogleCalendar()`:
  - Build Google Calendar URL: `https://calendar.google.com/calendar/r/eventedit?text=...&dates=...&details=...&location=...`
  - Open in new tab
  - Timezone-safe: use session start/end times with UTC conversion
- Implement `DownloadIcsFile()`:
  - Generate RFC 5545 compliant `.ics` content
  - Handle ICS line folding and escaping
  - Support VTIMEZONE
  - Trigger browser download

##### [MODIFY] EventDetail.razor
- Replace stubs with calendar action dropdown/menu
- Two options: "Google Calendar" and "Download .ics"

##### Calendar export semantics for multi-session events
- **Primary session only** exported to calendar by default
- For multi-session events with clear primary: export primary
- Future: "Download full schedule (.ics)" with multiple VEVENT entries (not MVP)

---

### P6 — UI/UX Theme Improvements (HIGH)

**Current theme problem**: Both light and dark palettes use `#00D16F` (a saturated green) as the primary color. This reads as aggressive, unsophisticated, and clashes with the premium aspirations of the platform.

##### [MODIFY] `MainLayout.razor.cs` — Light Palette
Replace the green-heavy palette with a refined, professional color system:

**Proposed light palette:**
- **Primary**: `#3B82F6` (refined blue — professional, trustworthy, universally readable)
- **Secondary**: `#1E293B` (slate — grounding, modern)
- **Background**: `#F8FAFC` (very light slate — subtle warmth)
- **Surface**: `#FFFFFF`
- **Success**: `#10B981` (muted emerald — professional green for confirmations only)
- **Info**: `#3B82F6` (matches primary)
- **Warning**: `#F59E0B` (warm amber)
- **Error**: `#EF4444` (refined red)
- **AppbarBackground**: `rgba(248,250,252,0.85)` (subtle frosted glass)
- Subtle light gradient on background: `linear-gradient(180deg, #F0F4FF 0%, #F8FAFC 100%)`

**Proposed dark palette:**
- **Primary**: `#60A5FA` (lighter blue for dark mode contrast)
- **Secondary**: `#F1F5F9`
- **Background**: `#0F172A` (deep navy-slate)
- **Surface**: `#1E293B` (dark slate card surface)
- **Success**: `#34D399`
- **AppbarBackground**: `rgba(15,23,42,0.85)` (frosted dark glass)

##### [MODIFY] MainLayout.razor
- Add subtle background gradient via CSS custom property or inline style
- Consider a very light `linear-gradient` on the `<MudLayout>` background in light mode

##### Component-level improvements (scoped CSS)
- Review event card styling for consistency with new palette
- Ensure chips, buttons, and accents use the new primary blue
- Verify dark mode contrast ratios meet WCAG AA

---

### P7 — Organization Events Page (MEDIUM)

##### [MODIFY] OrganizationDetails.razor / OrganizationDetails.razor.cs
- Add "Events by this organization" section
- Query events by actor/organization ID
- Show upcoming events first, then past events
- Respect visibility rules (only Published/Cancelled/Completed, not Draft/Archived/Unlisted)

---

### P8 — Remove Non-Functional UI (LOW)

##### [MODIFY] EventDetail.razor
- Remove "Report" button entirely (no backend exists)
- Dead buttons reduce perceived product quality

---

### P9 — Deployment Golden Path Verification (HIGH)

**This is a product feature, not a documentation task.**

The deliverable is a single, fully tested, reproducible install path.

##### Verification checklist
- [ ] Clean machine: `git clone` → `docker compose up` → instance accessible
- [ ] Keycloak import: realm config imports correctly
- [ ] MinIO/S3 storage: upload and serve images work
- [ ] Database migrations: apply cleanly on fresh PostgreSQL
- [ ] First-run onboarding: complete instance + tenant setup
- [ ] Event creation: create one event end-to-end
- [ ] Public URL: event page accessible from outside Docker network
- [ ] Share image: uploaded event images accessible via public proxy for preview cards
- [ ] Reverse proxy: base URL / forwarded host headers correct
- [ ] Tenant resolution: works in deployed environment

##### [MODIFY] docs/OPERATIONS.md
- Update with verified golden path instructions
- Include minimum Keycloak realm configuration
- Include Docker Compose variables and their effects

---

## What Is NOT In MVP Scope

- ❌ EAV custom properties (moved to `dev/pause/`)
- ❌ Federation protocol surface (foundation exists)
- ❌ Localization/i18n (English-only)
- ❌ Email notifications
- ❌ Payment integration
- ❌ Maps integration
- ❌ Template system for recurring events
- ❌ Rich text editor for descriptions
- ❌ Mobile app / PWA
- ❌ Platform-specific share buttons (WhatsApp, Twitter, etc.)
- ❌ Report feature

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| `HeadContent` OG tags not in initial HTML for some render modes | Medium | High | Verify with `SeoBalanced` preset; server-side fallback endpoint as contingency |
| Pre-signed image URLs expire in OG tags if no proxy | Certain | High | Public image proxy endpoint (P0.3) eliminates this |
| Blazouter route migration breaks existing navigation | Medium | Medium | Update all `NavigateTo` calls; keep old patterns temporarily |
| URL refactoring breaks external links/bookmarks | Low | Low | Keep old routes as redirects during transition |
| Theme change disrupts existing component styling | Low | Medium | Test all major pages in both light/dark after palette change |
| Multi-tenant URL generation in Docker/proxy | Medium | High | `IPublicUrlBuilder` with `X-Forwarded-*` support |

---

## Effort Estimates

| Priority | Item | Effort |
|---|---|---|
| P0.1 | Public URL builder service | S |
| P0.2 | Event visibility rules + Unlisted | M |
| P0.3 | OG image proxy endpoint | M |
| P1 | Shareable event page + OG metadata | M |
| P2 | Share button | S |
| P3 | Draft save + visibility controls | M |
| P4 | URL refactoring | M |
| P5 | Calendar integration | S-M |
| P6 | Theme improvements | M |
| P7 | Organization events page | S-M |
| P8 | Remove dead UI | S |
| P9 | Deployment verification | M-L |

**Total estimated effort**: 2–3 weeks for a focused developer. P0+P1+P2 (the critical path) could be done in **3–5 days**. Slightly more if `HeadContent` metadata requires the server-side fallback path.

---

## Verification Plan

### Automated Tests

**Existing test suites** (run via `dotnet test` from solution root):
- `Event.Architecture.Tests` — validates dependency rules and naming
- `Event.Application.UnitTests` — application layer unit tests
- `Event.API.IntegrationTests` — API endpoint integration tests
- `Explore.Blazor.Client.Tests` — Blazor component tests (bUnit)

**New tests to add:**

| Test | Type | What it verifies |
|---|---|---|
| `PublicUrlBuilder` — generates correct absolute URLs | Unit | P0.1 |
| `PublicUrlBuilder` — handles reverse proxy headers | Unit | P0.1 |
| Event visibility — Draft not returned in public list | Unit/Integration | P0.2 |
| Event visibility — Archived returns 404 on detail | Integration | P0.2 |
| Event visibility — Unlisted excluded from list but accessible by ID | Integration | P0.2 |
| Public image proxy — returns image for published event | Integration | P0.3 |
| Public image proxy — rejects request for non-existent storage object | Integration | P0.3 |
| Event page anonymous access — returns 200 for published event | Integration | P1 |
| Event page anonymous access — returns 404 for draft event | Integration | P1 |
| Google Calendar URL builder — correct date format and encoding | Unit | P5 |
| ICS file generation — valid RFC 5545 output | Unit | P5 |

### Manual Verification

1. **P1**: Open incognito browser → navigate to `/events/{guid}` → page loads with event data, no login required
2. **P1**: View page source → `<meta property="og:title">` present with correct event title in raw HTML
3. **P1**: Share event URL in a messaging app → preview card shows title, description, image
4. **P2**: Click Share button → snackbar shows "Link copied!" → paste from clipboard → correct URL
5. **P3**: Create event → click dropdown arrow → "Save as Draft" → event not visible in public list
6. **P4**: Navigate to old URL `/myevents` → should redirect or work (transition period)
7. **P5**: Click "Add to Google Calendar" → Google Calendar opens with correct event title, time, location
8. **P6**: Compare light and dark mode before/after theme change — verify readability and aesthetics
9. **P9**: On clean machine: `docker compose up` → complete onboarding → create event → share link → verify card preview

---

## Compatibility with Future Plans

- ✅ No schema conflicts with paused EAV custom properties
- ✅ `IPublicUrlBuilder` is the same service federation will use for AT Protocol record URLs
- ✅ OG meta tags can later include custom property data
- ✅ Public image proxy will be reused by federation for public media
- ✅ URL refactoring establishes the permanent canonical URL shape
- ✅ Event visibility rules are the foundation for future publication workflows
- ✅ Theme system remains MudBlazor-based, no framework change

---

## Critique: Potential Risks & Unknowns

1. **HeadContent + render mode interaction**: The `PublicSeo` route group supports prerendering, but whether `<HeadContent>` with async-loaded data actually populates the initial HTML in all five render presets needs empirical verification. If it doesn't, the server-side fallback endpoint is the contingency.

2. **Pre-signed URL expiration for OG images**: Certain failure if not addressed. The public image proxy (P0.3) is non-optional. Without it, shared event links will show broken image previews within hours.

3. **Blazouter route migration**: Changing route patterns from `/event/detail/{id}` to `/events/{id}` while staying on Blazouter requires verifying Blazouter's route registration syntax and ensuring the `PublicSeo` route group classification still matches.

4. **URL refactoring scope creep**: Navigation references to old URLs exist across many files. A systematic find-and-replace is needed, not ad-hoc fixes. Missing one reference creates a broken link.

5. **Theme change visual regression**: Changing the primary color from green to blue affects every component that uses `Color.Primary`. Manual review of all major pages in both modes is required.
