ABOUTME: Checklist for all MVP launch work packages with acceptance criteria and release gates.
ABOUTME: Organized by tier for incremental delivery. Revised 2026-03-29 with architect review feedback.

# MVP Launch — Task Checklist

> **Last Updated:** 2026-03-29 (architect review incorporated)

---

## Release Gates

### Gate A — Deployability
- [ ] Both services build in containers (.NET 10)
- [ ] `docker compose up` starts successfully
- [ ] Redis healthy OR in-memory fallback active with startup log
- [ ] DB migrations apply cleanly (including DataProtection)
- [ ] `/health` and `/alive` return 200

### Gate B — Session Integrity
- [ ] Login survives app restart
- [ ] Auth cookie valid after container recycle
- [ ] No unexpected logout during navigation
- [ ] DataProtection keys in database

### Gate C — Registration Truthfulness
- [ ] User registers successfully
- [ ] UI shows accurate message
- [ ] Outbox row created atomically
- [ ] Processor dispatches message
- [ ] Email sent or gracefully retried
- [ ] No duplicate email on outbox replay

### Gate D — Public Event Completion Loop
- [ ] Event detail loads anonymously
- [ ] User can share event
- [ ] User can download .ics calendar
- [ ] User can view registrations after registering
- [ ] Draft hidden from anonymous; Archived returns 404

---

## Tier 1A — Hard Launch Blockers

### WP-1: Infrastructure Fixes ⏳ NOT STARTED (1-2 days)

#### WP-1.4: Fix Broken Email Promise (DO THIS FIRST)
- [ ] Open `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`
- [ ] Line 87: Remove "You will receive a confirmation email shortly."
- [ ] Replace with: "Your registration has been confirmed."
- [ ] Run Blazor client tests — no regressions
- Acceptance: No false promises in UI

#### WP-1.1: Fix Blazor Dockerfile .NET Version
- [ ] Open `Explore.Blazor/Dockerfile`
- [ ] Line 6: `FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base` → `10.0`
- [ ] Line 12: `FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build` → `10.0`
- [ ] Verify `Explore.API/Dockerfile` already uses 10.0 ✅
- Acceptance: `docker build` succeeds for both Dockerfiles

#### WP-1.2: Configure DataProtection Key Persistence
- [ ] Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` v10.0.5 to `Explore.Persistence` and `Explore.Blazor`
- [ ] **Do NOT add to Explore.API** (API is bearer-only, never needs BFF key ring)
- [ ] Create `Explore.Persistence/DataProtectionKeyContext.cs`:
  - Separate DbContext implementing `IDataProtectionKeyContext`
  - NOT on `ExploreDbContext` (keys are global, not tenant-scoped)
  - `DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();`
  - ABOUTME header, file-scoped namespace
- [ ] Register `DataProtectionKeyContext` in DI with `UseNpgsql(DefaultConnection)`
- [ ] Create EF migration: `--context DataProtectionKeyContext --output-dir Migrations/DataProtection`
- [ ] Register in `Explore.Blazor/Program.cs` ONLY:
  ```csharp
  builder.Services.AddDataProtection()
      .SetApplicationName("islamu-event")
      .PersistKeysToDbContext<DataProtectionKeyContext>();
  ```
- [ ] Verify migration auto-applies via `Event.MigrationService`
- [ ] Run build — no errors
- [ ] Run all test projects — no regressions
- Acceptance: Gate B passes — sessions survive restart

#### WP-1.3: Add Redis to docker-compose.yml (with graceful degradation)
- [ ] Add Redis service:
  ```yaml
  redis:
    image: redis:7-alpine
    restart: unless-stopped
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      retries: 5
  ```
- [ ] Add `redis_data` to volumes section
- [ ] Wire Redis connection for Blazor service
- [ ] **Verify in-memory fallback works:** remove Redis → app still starts and works
- [ ] **Verify startup log:** app logs effective cache backend (Redis vs in-memory)
- [ ] **Verify degradation warning:** if Redis configured but unavailable, log warning (not crash)
- Acceptance: Gate A passes — works with or without Redis

#### Post-WP-1: Deploy Smoke Test
- [ ] Run `docker compose up` → all services healthy
- [ ] Login → navigate → restart → login persists (Gate B)
- [ ] Redis down → app still works (Gate A degradation)

---

## Tier 1B — Pre-Launch Hardening

### WP-2: Navbar Customization Phase 7 ⏳ NOT STARTED (2 days)
- [ ] Read `dev/active/navbar-customization/navbar-customization-tasks.md`
- [ ] Read `dev/active/navbar-customization/navbar-customization-context.md`
- [ ] Complete all Phase 7 soft-delete compliance tasks
- [ ] Complete URL validator tasks
- [ ] Complete cache invalidation tasks
- [ ] Run all tests — no regressions
- Acceptance: Phase 7 complete; tests pass

---

## Tier 2 — Must-Do Before First Real Users

### WP-6: Calendar Integration — iCal/.ics ⏳ NOT STARTED (1-2 days)

#### WP-6.1: Add Ical.Net NuGet Package
- [ ] `dotnet add Explore.API package Ical.Net`
- Acceptance: Package restores and builds

#### WP-6.2: API — Calendar Endpoint
- [ ] Add `GET /api/event/{id}/calendar` to `EventController`
- [ ] `[AllowAnonymous]`
- [ ] `Content-Type: text/calendar; charset=utf-8`
- [ ] `Content-Disposition: attachment; filename="{sanitized-slug}.ics"`
- [ ] VEVENT: SUMMARY, DTSTART/DTEND (UTC), LOCATION, DESCRIPTION, URL (canonical), UID (event GUID)
- [ ] Non-negotiable checks:
  - [ ] UID = event GUID (stable, not random)
  - [ ] All timestamps UTC normalized
  - [ ] Filename sanitized (strip special chars)
  - [ ] 404 for Draft, Archived, non-existent
  - [ ] Canonical URL matches `GetCanonicalUrl()` pattern
- [ ] Route name: `RouteNames.GetEventCalendar`
- Acceptance: .ics opens correctly in Calendar apps

#### WP-6.3: Blazor — Calendar Buttons
- [ ] `EventDetail.razor`: "Add to Calendar" button → `/api/event/{id}/calendar`
- [ ] `MyRegistrations.razor`: per-event calendar icon button
- [ ] Post-registration success (WP-5): calendar button
- [ ] Optional: Google Calendar URL link
- Acceptance: Calendar buttons work from 3 touchpoints

#### WP-6.4: NSwag Client Regeneration
- [ ] Export OpenAPI (run API)
- [ ] Rebuild Blazor client (regenerates `EventApiClient.g.cs`)
- [ ] Fix consuming code
- [ ] Run client tests
- Acceptance: Client builds cleanly

#### WP-6.5: Tests
- [ ] Integration: valid .ics with correct Content-Type
- [ ] 404 for Draft/Archived events
- [ ] UTC timestamps correct
- Acceptance: Tests pass

### WP-7: Event Sharing — Verification ⏳ NOT STARTED (0.5 day)

> **EXISTS:** `ShareEventAsync()` + `GetCanonicalUrl()` + OG meta tags

- [ ] Verify share button visible on EventDetail
- [ ] Check EventCard (list view) — add share icon if missing
- [ ] Verify canonical URL tenant-aware in multi-tenant
- [ ] If URL generation duplicated → centralize into shared helper
- Acceptance: Gate D "user can share" passes

### WP-4: My Registrations — Enhancement ⏳ NOT STARTED (0.5 day)

> **EXISTS:** `Pages/User/MyRegistrations.razor` at `/my/registrations`

- [ ] Verify NavMenu/user menu link — add if missing
- [ ] Verify discoverability from nav AND post-registration flow
- [ ] Add "Add to Calendar" icon per registration card (after WP-6)
- [ ] Verify empty state UX
- Acceptance: Gate D "user can view registration later" passes

### WP-5: Post-Registration Confirmation UX ⏳ NOT STARTED (1 day)

> Enhance `EventRegistration.razor` success state (lines 77-96)

- [ ] Add "Add to Calendar" button (WP-6 dependency)
- [ ] Add "Share this Event" button (reuse `ShareEventAsync` pattern)
- [ ] Add "View My Registrations" link → `/my/registrations`
- [ ] Keep it lightweight — 3 actions only, no workflow engine
- [ ] bUnit test for enhanced success state
- Acceptance: Post-registration has 3 actionable next steps

### WP-3: Registration Confirmation Email ⏳ NOT STARTED (3 days)

#### WP-3.1: Email Template Builder
- [ ] Create `Explore.Infrastructure/Mail/Templates/RegistrationConfirmedEmailBuilder.cs`
- [ ] ABOUTME header, file-scoped namespace
- [ ] String interpolation (no template engine for MVP)
- [ ] Inputs: event name, date/time, location, organizer, event URL, calendar URL
- [ ] Output: HTML email body
- [ ] Unit test for builder
- Acceptance: Clean HTML rendered from event data

#### WP-3.2: IOutboxMessageHandler Interface
- [ ] Create `Explore.Application/Contracts/Outbox/IOutboxMessageHandler.cs`
  ```csharp
  public interface IOutboxMessageHandler
  {
      string EventType { get; }
      Task HandleAsync(OutboxMessage message, CancellationToken ct);
  }
  ```
- [ ] ABOUTME header, file-scoped namespace
- Acceptance: Interface defined

#### WP-3.3: Routing Outbox Message Dispatcher
- [ ] Create `Explore.Infrastructure/Outbox/RoutingOutboxMessageDispatcher.cs`
- [ ] Resolves `IEnumerable<IOutboxMessageHandler>` from DI
- [ ] Routes by `OutboxMessage.EventType` → matching handler
- [ ] Unhandled types → log warning (no failure, preserves current behavior)
- [ ] Register as `IOutboxMessageDispatcher` (replaces `LoggingOutboxMessageDispatcher`)
- [ ] Unit test for routing + fallback behavior
- Acceptance: Messages route correctly

#### WP-3.4: Wire Registration Handler to Outbox (Reference Payload)
- [ ] Open `CreateEventRegistrationCommandHandler.cs`
- [ ] After successful save, create `OutboxMessage`:
  - `AggregateType = "EventRegistration"`
  - `AggregateId = registration.Id`
  - `EventType = "RegistrationConfirmed"`
  - `Payload = JSON { registrationId, eventId, eventSessionId, userId, tenantId, correlationId }`
- [ ] **Reference payload only — no presentation data**
- [ ] Persist via `IOutboxRepository.Create()` in same UoW transaction
- [ ] Unit test verifying outbox message creation
- Acceptance: OutboxMessage created atomically with registration

#### WP-3.5: RegistrationConfirmed Handler (Idempotent)
- [ ] Create `Explore.Infrastructure/Outbox/Handlers/RegistrationConfirmedOutboxHandler.cs`
- [ ] `EventType = "RegistrationConfirmed"`
- [ ] **Idempotency:** check if email already sent for this registration before dispatching
- [ ] Deserialize reference payload → fetch fresh data from repos → build email → send via `IEmailService`
- [ ] Structured logging: `RegistrationId`, `EventId`, `UserId`, `TenantId`, `OutboxMessageId`
- [ ] Unit test with mocked repos and IEmailService
- Acceptance: Email sent; no duplicates on replay

#### WP-3.6: Observability
- [ ] Counter: `outbox.messages.processed` with `{event_type, outcome, tenant_id}`
- [ ] Counter: `outbox.messages.failed` with `{event_type, tenant_id}`
- [ ] Dead-letter visibility via `GetFailedEntries`
- Acceptance: Operators can monitor outbox health

#### WP-3.7: Restore Email Promise + Integration Test
- [ ] Restore line 87: "You will receive a confirmation email shortly."
- [ ] Integration test: register → outbox message created → handler processes → email sent
- [ ] Run all tests
- Acceptance: Gate C fully passes

### WP-8: HATEOAS Client Alignment ⏳ NOT STARTED (1.5 days)

- [ ] Read `dev/active/hateoas-client-alignment/hateoas-client-alignment-tasks.md`
- [ ] Phase 2.1: Extract `HasHalLinkInAdditionalProperties` private helper
- [ ] Phase 2.2: Add `HasHalLink` for `OrganizationDto` and `OrganizationListDto`
- [ ] Phase 3.1: Remove `CheckEditPermissions()`, delete `currentUserRole`, use `HasHalLink("edit")`
- [ ] Phase 3.2: Verify `_links` preservation in service
- [ ] **Grep for `RoleHelper.CanManage` in adjacent pages** — fix any cousins
- [ ] Phase 4.3: bUnit tests for OrganizationDetails HATEOAS consumption
- [ ] Run all tests — no regressions
- Acceptance: No RoleHelper for action gating; HAL links are source of truth

### WP-9: Save Draft vs Publish UX ⏳ NOT STARTED (1-1.5 days)

> **Scope warning:** MVP minimum only. Defer beforeunload and advanced transitions.

#### WP-9.1: MVP — Explicit Buttons
- [ ] Event create form: "Save as Draft" (secondary) + "Publish" (primary) buttons
- [ ] Event edit form for Draft: "Save" + "Publish"; for Published: "Save" only
- [ ] Verify Draft → Published transition works in API handler
- [ ] bUnit tests for button rendering based on status
- Acceptance: Organizers choose visibility via explicit button

#### Deferred (post-MVP)
- `beforeunload` guard for unsaved changes
- Advanced status transition validation
- Undo publish

---

## Tier 3 — Polish (Before Public Announcement)

### WP-10: User Onboarding — Gap Analysis ⏳ NOT STARTED (1 day)

> **EXISTS:** Admin onboarding (instance + tenant). User-level may be missing.

- [ ] Audit existing onboarding for completeness
- [ ] Decision: if admin onboarding satisfies need → close fast
- [ ] If gap: lightweight first-login detection + welcome modal
- [ ] Do NOT invent big first-run system late in MVP
- Acceptance: Guided path exists or gap documented as post-MVP

### WP-11: Targeted Test Coverage ⏳ NOT STARTED
- [ ] Registration flow (approval, waitlist, capacity)
- [ ] Visibility rules (Draft hidden, Archived 404)
- [ ] HATEOAS action gating (OrganizationDetails)
- [ ] Calendar endpoint (.ics valid, 404 non-public)
- [ ] Session persistence regression (DataProtection)
- Acceptance: Critical paths guarded; no test sprawl

### WP-12: Production Docker & External API Key ⏳ NOT STARTED
- [ ] `docker-compose.prod.yml` with pre-built images
- [ ] Starter `prometheus.yml` scrape config
- [ ] **Disable external API key endpoints** — config flag or remove from routing
- Acceptance: API key surface not exposed; prod deploy path exists

### WP-13: Cleanup ⏳ NOT STARTED
- [ ] Verify Price/CurrencyCode not in any UI form
- [ ] Final docs update
- Acceptance: No misleading features visible

---

## Sprint Order (Recommended)

| Sprint | Days | Work Packages | Gate |
|--------|------|---------------|------|
| 1 | 1-2 | WP-1 (all infra fixes) | Gate A + B smoke |
| 2 | 3-5 | WP-6 (iCal) + WP-7 (share verify) + WP-4 (enhance) + WP-5 (post-reg UX) | Gate D partial |
| 3 | 6-9 | WP-3 (email) + WP-8 (HATEOAS) | Gate C + D full |
| 4 | 10-12 | WP-2 + WP-9 + WP-10/11/12/13 | All gates |

## Quick Resume

1. Read `mvp-launch-context.md` for key decisions
2. Check this file for current progress
3. Start with WP-1.4 (broken promise) → then WP-1.1/1.2/1.3 → smoke test Gates A+B
4. Follow NSwag checklist (in plan) after any API changes
