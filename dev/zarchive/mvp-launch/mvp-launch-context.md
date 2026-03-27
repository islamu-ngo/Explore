# MVP Launch — Context

Last Updated: 2026-03-22

## SESSION PROGRESS (2026-03-22)

### ✅ COMPLETED (all previously planned phases)
- Phase 0: Public URL builder, visibility rules, OG image proxy — all complete
- Phase 1: OG metadata in EventDetail (HeadContent, all og/twitter tags) — complete
- Phase 2: Share button in EventDetail + CopyEventLinkAsync in EventList — complete
- Phase 3: Draft save, UpdateEventCommand CQRS, visibility controls — complete
- Phase 4: Full URL refactoring — all routes at canonical paths, all NavigateTo refs updated
  - Bug fixed: `EventCreated.razor` `/createevent` → `/events/create`
  - Bug fixed: `MyEvents.razor.cs` `NavigateToCreateEvent()` was pointing to `/my/organizations`
- Phase 5: Calendar integration (Google Calendar + ICS download) — complete
- Phase 6: Theme (blue palette) — already applied in MainLayout.razor.cs
- Phase 8: Report button — was never present, audit done

### ✅ BUILD & TEST FIXES
- Added `UpdateEventFieldsAsync` to `partial interface IEventApiClient` in EventApiClient.cs
  (was missing from interface, causing compile errors on `IEventApiClient` typed references)
- Updated `EventServiceTests.cs` to mock `UpdateEventFieldsAsync` instead of `UpdateEventAsync`

### 🟡 REMAINING
- Phase 9: Deployment golden path verification (manual, not code)

### ⚠️ KEY DECISIONS MADE
- Stay with Blazouter routing always (user direction)
- No WhatsApp-specific sharing — just clipboard + Web Share API
- Solution must work with all runtime render modes per tenant
- OG images need a public proxy endpoint because pre-signed URLs expire
- Replace green theme with blue-based professional palette
- Add Draft save dropdown + Unlisted event concept
- Refactor all non-RESTful URL patterns
- HeadContent is primary approach for OG tags; server-side fallback is contingency

## Architecture Decisions

### Render Mode Compatibility
From `RENDER_POLICIES.md`:
- 5 presets: `AllInteractiveServer`, `SeoBalanced`, `AllPrerendered`, `AllInteractiveAutoNoPrerender`, `CustomAdvanced`
- `PublicSeo` route group includes `/event/detail/*` — must be updated to `/events/*`
- `SeoBalanced` enables prerender for `PublicSeo` group → HTML includes `<HeadContent>` data
- Default `AllInteractiveServer` does NOT prerender → `<HeadContent>` may not be in initial HTML for crawlers
- **Implication**: For OG metadata to work reliably, the tenant should either use `SeoBalanced`/`AllPrerendered`, or a server-side fallback must emit meta tags

### Pre-Signed URL Problem for OG Images
- `ImageStorageService` uses `PresignedUrlAsync` / `PresignedUrlByKeyAsync` to get time-limited URLs
- Social crawlers cache OG images at scrape time — pre-signed URL may expire before cache refresh
- **Solution**: Public image proxy endpoint that serves images directly with stable URLs

### URL Refactoring Scope
Current → New:
- `/myevents` → `/my/events` (MyEvents.razor)
- `/my-registrations` → `/my/registrations` (MyRegistrations.razor)
- `/organizations/my` → `/my/organizations` (MyOrganizations.razor)
- `/user/reviews` → `/my/reviews` (MyReviews.razor)
- `/create-event` → `/events/create` (CreateEvent.razor)
- `/event/detail/{id}` → `/events/{id}` (EventDetail)
- `/event/edit/{id}` → `/events/{id}/edit` (EventEdit)

NavigateTo references to update:
- `EventList.razor.cs`: lines 516, 657
- `EventDetail.razor.cs`: line 646
- `EventEdit.razor.cs`: line 460
- `CreateEvent.razor.cs`: lines 154, 176, 618
- `MyEvents.razor.cs`: lines 94, 104, 162, 163
- `CopyEventLinkAsync` in EventList.razor.cs: line 596

### Theme Colors
Current (problematic):
- Primary: `#00D16F` (saturated green — aggressive, unsophisticated)
- Same green in both light and dark

Proposed:
- Light Primary: `#3B82F6` (professional blue)
- Dark Primary: `#60A5FA` (lighter blue for contrast)
- Success: `#10B981` / `#34D399` (green reserved for confirmations only)

## Key Files

### Render Policy System
- `Explore.Blazor.Client/Services/RuntimeRenderPolicyService` — route classification + mode resolution
- `docs/RENDER_POLICIES.md` — full documentation

### Theme
- `Explore.Blazor.Client/Layout/MainLayout.razor.cs` — `_lightPalette`, `_darkPalette`, `BuildTheme()` (lines 130-219)

### Event Status
- `Explore.Domain/Enums/EventStatusEnum.cs` — Draft=1, Published=2, Cancelled=3, Completed=4, Archived=5

### Image Storage
- `Explore.Blazor.Client/Services/ImageStorageService.cs` — pre-signed URL handling
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs` — `PresignedUrlAsync`, `PresignedUrlByKeyAsync`

## Quick Resume

To continue:
1. Read this file for current state and decisions
2. Review `mvp-launch-plan.md` for the full plan
3. Check `mvp-launch-tasks.md` for task checklist
4. Start with P0: Public URL builder, then visibility rules, then OG image proxy
