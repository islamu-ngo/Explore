# MVP Launch — Task Checklist

Last Updated: 2026-03-22

## Phase 0: Public Event Page Foundation ✅ COMPLETE (was already implemented)

### P0.1 — Centralized Public URL Builder ✅
- [x] `IPublicUrlBuilder` interface — `Explore.Application/Contracts/Infrastructure/IPublicUrlBuilder.cs`
- [x] `PublicUrlBuilder` impl — `Explore.Infrastructure/Services/PublicUrlBuilder.cs`
- [x] X-Forwarded-Host/Proto support, tenant-aware
- [x] Registered as scoped in `InfrastructureServicesRegistration.cs`

### P0.2 — Event Visibility Rules ✅ (via VisibilityTypeEnum, not IsUnlisted boolean)
- [x] Visibility handled via `VisibilityTypeEnum` (Public=1, Private=2, Unlisted=3, MembersOnly=4)
- [x] `PubliclyDiscoverable()` filter excludes Draft, Archived, non-Public visibility
- [x] EventDetail: Draft → ownership check (GetEventDetailsRequestHandler:87-96)
- [x] EventDetail: Archived → returns null/404 (GetEventDetailsRequestHandler:83-84)
- [x] Unlisted events: VisibilityTypeId=3 excluded from list, accessible by direct ID
- [x] EventDetail: Cancelled → "Cancelled" banner via MudAlert (EventDetail.razor)

### P0.3 — OG Image Public Proxy ✅
- [x] `GET /api/storageobject/{id}/public` — StorageObjectController
- [x] Returns image bytes with Content-Type and 7-day cache headers
- [x] Handler returns null→404 for non-existent storage objects

---

## Phase 1: Shareable Event Page + OG Metadata ✅ COMPLETE (was already implemented)
- [x] `<HeadContent>` with OG meta tags already in EventDetail.razor
  - [x] `og:title`, `og:description`, `og:image`, `og:url`, `og:type`, `og:site_name`
  - [x] `twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`
  - [x] `<meta name="description">`, `<link rel="canonical">`
- [x] Fallback rules for missing image/description (GetMetaDescription helper)
- [x] Unauthenticated users: "Login to register" button + redirect to `/login?returnUrl=...`
- [x] Cancelled event banner in EventDetail

---

## Phase 2: Share Action ✅ COMPLETE
- [x] Implement Share button OnClick in EventDetail (`ShareEventAsync` in EventDetail.razor.cs)
- [x] Copy canonical URL via `navigator.clipboard.writeText`
- [x] Show snackbar "Link copied to clipboard!"
- [x] Web Share API fallback for mobile (tries `navigator.share` first)
- [x] EventList sidebar `CopyEventLinkAsync` uses `Navigation.ToAbsoluteUri("/events/{id}")`

---

## Phase 3: Draft Save & Visibility Controls ✅ COMPLETE

### P3.1 — Save as Draft ✅
- [x] CreateEvent.razor `SaveAsDraftAsync()` sets status = Draft (1) and navigates to edit
- [x] MyEvents.razor shows Draft badge (Yellow/Warning chip for EventStatusId==1)
- [x] "Publish" action for draft events in MyEvents menu (PublishEvent method + PUT endpoint)

### P3.2 — UpdateEventCommand CQRS Pattern (null-check DTOs) ✅ (NEW)
- [x] `UpdateEventStatusDto` — DTO with just `EventStatusId`
- [x] `UpdateEventStatusDtoValidator` — validates against EventStatus lookup table
- [x] `UpdateEventCommand` — extended with `Id` + nullable `EventDto?` / `EventStatusDto?`
- [x] `UpdateEventCommandHandler` — null-check pattern: validates & applies whichever DTO is non-null
- [x] `PUT /api/event/{id}` accepts `UpdateEventCommand` (replaces old `UpdateEventDto` body)
- [x] `UpdateEventFieldsAsync` on partial EventApiClient (until NSwag regeneration)
- [x] `UpdateEventStatusAsync` on IEventService / EventService

### P3.3 — Visibility Controls
- [x] Visibility selector already in EventEdit.razor (VisibilityTypeId dropdown)
- [x] Status selector already in EventEdit.razor (EventStatusId dropdown)
- [x] Unlisted events (VisibilityTypeId=3) excluded from EventList, accessible by direct ID

---

## Phase 4: URL Refactoring ✅ COMPLETE
- [x] `/myevents` → `/my/events` (Routes.razor + MyEvents.razor @page)
- [x] `/my-registrations` → `/my/registrations` (Routes.razor + MyRegistrations.razor @page)
- [x] `/organizations/my` → `/my/organizations` (Routes.razor + MyOrganizations.razor @page)
- [x] `/user/reviews` → `/my/reviews` (Routes.razor + MyReviews.razor @page)
- [x] `/create-event` → `/events/create` (Routes.razor + CreateEvent.razor @page)
- [x] `/event/detail/{id}` → `/events/{id}` (Routes.razor line 54)
- [x] `/event/edit/{id}` → `/events/{id}/edit` (Routes.razor line 99)
- [x] All NavigateTo references updated (EventDetail, EventEdit, EventList, MyEvents, CreateEvent)
- [x] Bug fix: `EventCreated.razor` "Create Another Event" button: `/createevent` → `/events/create`
- [x] Bug fix: `MyEvents.razor.cs` `NavigateToCreateEvent()`: `/my/organizations` → `/events/create`
- [x] RuntimeRenderPolicyService: `/events/` pattern already covers all event routes
- [x] No old route redirects needed (all callers updated)

---

## Phase 5: Calendar Integration ✅ COMPLETE
- [x] `AddToGoogleCalendarAsync()` implemented (EventDetail.razor.cs)
- [x] `DownloadIcsFileAsync()` implemented (EventDetail.razor.cs)
- [x] `GenerateIcsContent()` with UTC times (EventDetail.razor.cs)
- [x] ICS line folding (75 octets per RFC 5545 §3.1) via `IcsFoldLine()`
- [x] `IcsEscape()` handles backslash, semicolon, comma, newlines
- [ ] Add unit tests for calendar/ICS generation

---

## Phase 6: UI/UX Theme Improvements ✅ COMPLETE
- [x] Light palette: Primary `#3B82F6`, AppbarBackground frosted, Background `#F8FAFC` (MainLayout.razor.cs)
- [x] Dark palette: Primary `#60A5FA`, Background `#0F172A`, Surface `#1E293B` (MainLayout.razor.cs)
- [x] Success green reserved for confirmations: `#10B981` (light) / `#34D399` (dark)
- [x] Setup.razor.css already uses blue gradient (#1E40AF → #3B82F6 → #60A5FA)

---

## Phase 7: Organization Events Page ✅ COMPLETE
- [x] `GetPublicEventsByActorAsync(Guid actorId)` added to IEventService + EventService
  - Loads public statuses (Published=2, Cancelled=3, Completed=4), filters by ActorId client-side
- [x] `OrganizationDetails.razor.cs`: inject IEventService, load events in parallel with permissions
  - `UpcomingEvents` / `PastEvents` computed properties for sort
- [x] `OrganizationDetails.razor`: Events section with card grid (upcoming + up to 6 past)

---

## Phase 8: Remove Non-Functional UI ✅ COMPLETE
- [x] "Report" button not present in EventDetail.razor (already removed/never added)
- [x] Audit complete: Share, Calendar, Register, Edit, Delete all have working handlers

---

## Phase 9: Deployment Golden Path Verification 🟡 HIGH
- [ ] Clean machine golden path test
- [ ] Update docs/OPERATIONS.md

---

## Post-MVP: NSwag Regeneration ✅ COMPLETE
- [x] swagger.json and EventApiClient.g.cs regenerated — `UpdateEventAsync(Guid, UpdateEventCommand)` now in interface
- [x] Removed manual `UpdateEventFieldsAsync` from EventApiClient.cs (partial class now only has PrepareRequest)
- [x] EventService.UpdateEventAsync uses `UpdateEventCommand { Id, EventDto }`
- [x] EventService.UpdateEventStatusAsync uses `UpdateEventCommand { Id, EventStatusDto }`
- [x] EventServiceTests updated to mock `UpdateEventAsync(Guid, UpdateEventCommand)`
