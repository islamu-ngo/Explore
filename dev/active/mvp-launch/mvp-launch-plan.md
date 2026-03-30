ABOUTME: Strategic implementation plan for closing all MVP gaps before production launch.
ABOUTME: Prioritized into tiered gates with phased work, acceptance criteria, and risk assessment.

# MVP Launch — Implementation Plan

> **Created:** 2026-03-28 | **Revised:** 2026-03-29 (architect review feedback incorporated)
> **Branch:** `develop`
> **Goal:** Close all gaps identified in `dev/active/mvp-report.md` to reach a shippable MVP for real organizers and event seekers.

---

## Executive Summary

The platform is ~65-70% MVP-complete. The architecture is production-grade (Clean Architecture, CQRS, BFF, multi-tenancy, HATEOAS, rate limiting, observability). What's missing falls into three categories:

1. **Infrastructure blockers** — Dockerfile .NET version mismatch, DataProtection key persistence, missing Redis in docker-compose
2. **Broken user promises** — Registration confirmation email never sent, yet UI says it will be
3. **Missing user-facing features** — No calendar integration, no post-registration flow, incomplete publish UX

This plan organizes work into 4 tiers across 13 work packages with 4 explicit go/no-go release gates.

### Corrections Applied
- **Email verification** is handled by **Keycloak** (not the application). AT Proto handle → PDS's responsibility.
- **MyRegistrations page already exists** at `/my/registrations` with cancel, search, virtualized cards.
- **Share functionality already exists** in EventDetail via Web Share API + clipboard fallback.
- **Admin onboarding already exists** (InstanceOnboarding, TenantOnboarding, StartupGate).

### Key Architecture Decisions (from review)
- **DataProtection**: Blazor BFF only. Do NOT register in API project (API is bearer-only, never needs the same key ring).
- **Outbox payload**: Reference payload (IDs only). Handler fetches fresh data at dispatch time. Smaller payload, fresher data.
- **Redis**: In-memory fallback when unavailable. The app must work optimally without Redis. Context: self-hostable platform where minimal infra (Blazor + API + DB) must be sufficient, with Redis/Cerbos/Keycloak etc. as optional enhancements.

---

## Release Gates (Go/No-Go)

Before declaring MVP-ready, all four gates must pass.

### Gate A — Deployability
- [ ] Both services build in containers (.NET 10)
- [ ] `docker compose up` starts successfully
- [ ] Redis healthy (if present) OR in-memory fallback active with startup log confirming effective cache backend
- [ ] DB migrations apply cleanly (including DataProtection)
- [ ] App reaches ready state (`/health`, `/alive` return 200)

### Gate B — Session Integrity
- [ ] Login survives app restart
- [ ] Auth cookie remains valid after container recycle
- [ ] No unexpected logout during normal navigation
- [ ] DataProtection keys persist in database

### Gate C — Registration Truthfulness
- [ ] User registers successfully
- [ ] UI shows accurate message (no false promises)
- [ ] Outbox row created atomically with registration
- [ ] Background processor dispatches the message
- [ ] Email sent or gracefully retried (no silent failure)
- [ ] No duplicate confirmation email on outbox replay (idempotent handler)

### Gate D — Public Event Completion Loop
- [ ] Event detail loads for anonymous users
- [ ] User can share event (Web Share API / clipboard)
- [ ] User can download .ics calendar file
- [ ] User can view registration list after registering
- [ ] Draft events hidden from anonymous; Archived return 404

---

## Current State Analysis

### What Works (Production-Ready)
- Event CRUD with multi-session, agenda, speakers
- Event discovery with advanced filtering (date, category, tag tri-state, format, audience, language, madhab, skill level, gender mode)
- OIDC/BFF authentication (Keycloak)
- Multi-tenancy (runtime mode switching, query filters, tenant resolution)
- HAL/HATEOAS API with 58 controllers, pagination, authorization-aware links
- 4-tier rate limiting, 3-tier caching, ETags
- Observability (OpenTelemetry, Serilog, Prometheus metrics)
- SMTP infrastructure (MailKit, Polly resilience, per-tenant config)
- Outbox pattern infrastructure (transactional, retry, dead-letter)
- Organization/Group management with governance hierarchy
- My Registrations page (search, cancel, approval status)
- Event sharing (Web Share API + clipboard + OG meta tags)
- Admin onboarding (instance + tenant setup wizards)

### What's Broken or Missing
| # | Gap | Severity | Effort |
|---|-----|----------|--------|
| 1 | Blazor Dockerfile uses .NET 9, project targets net10.0 | BLOCKER | 1 hour |
| 2 | DataProtection keys not persisted (random logouts) | BLOCKER | 0.5 day |
| 3 | Redis missing from docker-compose.yml (must degrade gracefully) | BLOCKER | 1 hour |
| 4 | Registration UI promises email that never sends | BLOCKER | 1 hour (remove) / 3 days (implement) |
| 5 | No post-registration "What Next" flow | HIGH | 1 day |
| 6 | No iCal/.ics calendar integration | HIGH | 1-2 days |
| 7 | HATEOAS client violation in OrganizationDetails | RISK | 1.5 days |
| 8 | No explicit "Save Draft" vs "Publish" UX | MEDIUM | 1-2 days |
| 9 | My Registrations needs calendar button + discoverability check | LOW | 0.5 day |
| 10 | Share on EventCard (list view) — verify/add | LOW | 0.5 day |
| 11 | External API Key Phase 5 incomplete | RISK | disable for MVP |
| 12 | Navbar Customization Phase 7 incomplete | RISK | 2 days |

### In-Flight Work (Active Tracks)
1. **HATEOAS Client Alignment** (`dev/active/hateoas-client-alignment/`) — 5 phases, all not started. Phase 3 is MVP-critical.
2. **External API Access** (`dev/active/external-api-access/`) — Phases 0-4 complete. **Decision: disable endpoints for MVP** (ship without unlimited API key access).
3. **Navbar Customization** (`dev/active/navbar-customization/`) — Phases 1-6 complete, Phase 7 has open tasks.

---

## Implementation Tiers

### Tier 1A — Hard Launch Blockers
*Estimated: 1-2 days. Must be complete before ANY traffic.*

These are platform survivability issues. If the sprint gets compressed, these cannot be deferred.

#### WP-1: Infrastructure Fixes (1 day)

**WP-1.1: Fix Blazor Dockerfile .NET Version**
- File: `Explore.Blazor/Dockerfile`
- Change: `mcr.microsoft.com/dotnet/aspnet:9.0` → `10.0` (both base and SDK)
- API Dockerfile already uses 10.0 (verified)
- Acceptance: `docker build` succeeds for both Dockerfiles

**WP-1.2: Configure DataProtection Key Persistence**
- **Blazor BFF only.** Do NOT register in API project (API is bearer-only, never decrypts BFF cookies).
- Approach: Separate `DataProtectionKeyContext` (not on `ExploreDbContext` — keys are global, not tenant-scoped)
- Requires:
  1. Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` v10.0.5 to `Explore.Persistence` and `Explore.Blazor`
  2. Create `Explore.Persistence/DataProtectionKeyContext.cs` implementing `IDataProtectionKeyContext`
  3. Create EF migration: `--context DataProtectionKeyContext --output-dir Migrations/DataProtection`
  4. Register in `Explore.Blazor/Program.cs` only: `.AddDataProtection().SetApplicationName("islamu-event").PersistKeysToDbContext<DataProtectionKeyContext>()`
- Migration strategy: committed to source; auto-applied at startup by `Event.MigrationService` (same as all other migrations)
- Acceptance: BFF sessions survive container restart; Gate B passes

**WP-1.3: Add Redis to docker-compose.yml (with graceful degradation)**
- Add Redis 7 Alpine service with volume persistence and health check
- Wire connection strings for Blazor service
- **Critical requirement:** App must work without Redis. In-memory fallback is mandatory.
- App must log the **effective cache backend** at startup (Redis vs in-memory)
- Production profile must NOT silently fall back — log a warning if Redis is configured but unavailable
- Acceptance: `docker compose up` starts Redis; if Redis removed, app still works with in-memory cache and logs the degradation

**WP-1.4: Fix Broken Email Promise (do this FIRST if email won't ship immediately)**
- File: `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` line 87
- Remove: "You will receive a confirmation email shortly."
- Replace with: "Your registration has been confirmed."
- Product-truthfulness rule: no UI text promises functionality that doesn't exist
- Acceptance: Gate C "UI shows accurate message" passes

### Tier 1B — Pre-Launch Hardening
*Estimated: 2 days. Important but not same class as survivability.*

#### WP-2: Navbar Customization Phase 7 Completion (2 days)
- Existing track: `dev/active/navbar-customization/`
- Focus: soft-delete compliance, URL validators, cache invalidation
- Must read existing task docs before starting
- Acceptance: All Phase 7 tasks complete; tests pass

---

### Tier 2 — Must-Do Before First Real Users
*Estimated: 6-8 days*

#### WP-3: Registration Confirmation Email (3 days)

**WP-3.1: Create Email Template Infrastructure**
- Simple HTML email builder (string interpolation for MVP; no heavy template engine)
- Create `RegistrationConfirmedEmailBuilder.cs` in `Explore.Infrastructure/Mail/Templates/`
- Template fetches fresh data at render time (reference payload — see WP-3.2)
- Template content: event name, date/time, location, organizer, "View Event" link, "Add to Calendar" link
- Acceptance: Builder renders clean HTML given event + registration data

**WP-3.2: Wire Registration Handler to Outbox (Reference Payload)**
- File: `CreateEventRegistrationCommandHandler.cs`
- After successful registration, create `OutboxMessage` with **reference payload**:
  ```json
  {
    "registrationId": "guid",
    "eventId": "guid",
    "eventSessionId": "guid",
    "userId": "guid",
    "tenantId": "guid",
    "correlationId": "trace-id-snapshot"
  }
  ```
- **No presentation-ready data in payload.** Handler fetches fresh event/user data at dispatch time.
- Why reference: smaller payload, fresher data, no stale snapshot if event is edited after registration
- Persist via `IOutboxRepository.Create()` in same UoW transaction
- Acceptance: OutboxMessage row created atomically with EventRegistration

**WP-3.3: Implement Routing Outbox Message Dispatcher**
- Replace `LoggingOutboxMessageDispatcher` with `RoutingOutboxMessageDispatcher`
- Strategy pattern: `IOutboxMessageHandler` per event type, dispatcher routes by `OutboxMessage.EventType`
- Falls back to logging for unhandled types (no failure — preserves current behavior)
- Register as `IOutboxMessageDispatcher` in DI
- Acceptance: Dispatcher routes messages to correct handler

**WP-3.4: Create RegistrationConfirmed Handler (Idempotent)**
- Create `RegistrationConfirmedOutboxHandler.cs` in `Explore.Infrastructure/Outbox/Handlers/`
- **Idempotency contract:** handler is keyed by `(EventType="RegistrationConfirmed", registrationId)`. Must check prior send state before dispatching email. Options:
  - Store send marker on registration entity (e.g., `ConfirmationEmailSentAt`)
  - Or: accept rare duplicates as tolerable for MVP but log them
- Handler: deserialize reference payload → fetch registration + event + user from repos → build email via template → send via `IEmailService`
- Structured logging: `RegistrationId`, `EventId`, `UserId`, `TenantId`, `OutboxMessageId`
- Acceptance: Gate C passes (email sent, no duplicates on replay, dead-letter observable)

**WP-3.5: Observability**
- Metric counter: `outbox.messages.processed` with dimensions `{event_type, outcome, tenant_id}`
- Metric counter: `outbox.messages.failed` with dimensions `{event_type, tenant_id}`
- Dead-letter messages visible via `GetFailedEntries`
- Acceptance: Operators can monitor outbox health

**WP-3.6: Restore Email Promise in UI**
- After email is wired and tested, restore: "You will receive a confirmation email shortly."
- Acceptance: UI text matches actual behavior; Gate C fully passes

#### WP-6: Calendar Integration — iCal/.ics (1-2 days)

**WP-6.1: API — iCal Endpoint**
- Add `GET /api/event/{id}/calendar` to `EventController`
- Use `Ical.Net` NuGet package (v5.2.1, netstandard2.0)
- `[AllowAnonymous]` — public events only; return 404 for Draft/Archived
- `Content-Type: text/calendar; charset=utf-8`
- `Content-Disposition: attachment; filename="{sanitized-slug}.ics"`
- VEVENT fields: SUMMARY, DTSTART/DTEND (UTC), LOCATION, DESCRIPTION, URL (canonical), UID (event GUID — stable across requests)
- **Non-negotiables:**
  - Stable UID = event GUID (not random — allows calendar app updates)
  - UTC normalization for all timestamps
  - Filename sanitization (strip special chars)
  - 404 for Draft, Archived, non-existent events
  - Canonical URL must match `GetCanonicalUrl()` pattern from EventDetail
- Route name: `RouteNames.GetEventCalendar`
- Acceptance: .ics downloads correctly; opens in macOS Calendar, Google Calendar, Outlook

**WP-6.2: Blazor — Calendar Buttons**
- On `EventDetail.razor`: "Add to Calendar" button → `/api/event/{id}/calendar`
- On `MyRegistrations.razor`: per-event calendar icon button
- On post-registration success (WP-5): calendar button
- Optional: Google Calendar URL link (`https://calendar.google.com/calendar/render?action=TEMPLATE&text=...&dates=...`)
- Acceptance: Calendar buttons work from all three touchpoints

**WP-6.3: Tests**
- Integration test: valid .ics content returned with correct Content-Type
- Test: Draft/Archived events return 404
- Test: UTC timestamps correct (especially DST boundary if applicable)
- Test: all-day events handled correctly (if relevant to domain)
- Acceptance: Tests pass

**WP-6.4: NSwag Client Regeneration**
- Export OpenAPI → regenerate `EventApiClient.g.cs` → fix consuming code → build client
- Acceptance: Blazor client builds cleanly with new endpoint

#### WP-7: Event Sharing — Verification (0.5 day)

> **ALREADY EXISTS:** `EventDetail.razor` has `ShareEventAsync()` + `GetCanonicalUrl()` + OG meta tags.

- Verify share button is visible and discoverable on EventDetail page
- Check if EventCard (list view) has share affordance — add share icon button if missing
- Verify canonical URL is tenant-aware in multi-tenant mode
- If canonical URL generation logic needs duplication → centralize into shared helper now
- Acceptance: Gate D "user can share" passes

#### WP-4: My Registrations — Enhancement (0.5 day)

> **ALREADY EXISTS:** `Pages/User/MyRegistrations.razor` at `/my/registrations`

- Add "Add to Calendar" icon button per registration card (after WP-6)
- Verify NavMenu/user menu link to `/my/registrations` — add if missing
- Verify discoverability from BOTH nav/menu AND post-registration flow
- Verify empty state UX
- Acceptance: Gate D "user can view registration later" passes

#### WP-5: Post-Registration Confirmation UX (1 day)

> Enhance the existing `isRegistered` success state in `EventRegistration.razor` (lines 77-96)

- Add "Add to Calendar" button (iCal download from WP-6)
- Add "Share this Event" button (reuse `ShareEventAsync` pattern from EventDetail)
- Add "View My Registrations" link → `/my/registrations`
- Keep it lightweight: calendar + share + my registrations. Do not build a mini workflow engine.
- Acceptance: Post-registration success state has 3 actionable next steps

#### WP-8: HATEOAS Client Alignment — OrganizationDetails (1.5 days)

- Existing track: `dev/active/hateoas-client-alignment/`
- MVP scope: Phase 2 (HalResourceExtensions) + Phase 3 (OrganizationDetails fix) + Phase 4 bUnit tests
- Phase 1 (API collection link policies) and Phase 5 (docs) follow post-MVP
- Core fix: Replace `RoleHelper.CanManage(currentUserRole)` with `organization?.HasHalLink("edit") ?? false`
- **Also grep for nearby cousins** of `RoleHelper.CanManage(...)` in adjacent pages — these patterns cluster
- Acceptance: OrganizationDetails derives action affordance from HAL links; no RoleHelper for action gating

#### WP-9: Save Draft vs Publish UX (1-1.5 days)

> **Schedule risk warning:** This touches UI action semantics, domain transitions, visibility rules, validation, and permissions. Scope carefully.

**MVP minimum (do this):**
- Expose current Draft/Published status clearly in create/edit forms
- On create form: two distinct buttons — "Save as Draft" (secondary) and "Publish" (primary)
- On edit form for Draft: "Save" + "Publish" buttons; for Published: "Save" only
- Verify Draft → Published transition works in API handler

**Post-MVP (defer this):**
- `beforeunload` guard for unsaved changes (easy to underestimate, often annoying if done poorly)
- Advanced status transition validation
- Undo publish

- Acceptance: Organizers have clear control over event visibility via explicit button choice

---

### Tier 3 — Polish (Before Public Announcement)
*Estimated: 2-4 days*

#### WP-10: User Welcome/Onboarding — Gap Analysis (1 day)

> **PARTIALLY EXISTS:** Admin onboarding covers instance/tenant setup. User-level onboarding may be missing.

- Decision rule: if admin onboarding satisfies launch need, close fast. Do not invent a big first-run system late in MVP.
- If user-level gap exists: lightweight first-login detection + welcome modal
- Acceptance: First-time users have a guided path or the gap is documented as post-MVP

#### WP-11: Targeted Test Coverage

Focus on high-value tests only. Avoid test-coverage ambition trap.

- Registration flow (approval, waitlist, capacity)
- Visibility rules (Draft hidden, Archived 404)
- HATEOAS action gating (OrganizationDetails)
- Calendar endpoint (valid .ics, 404 for non-public)
- Session persistence regression (DataProtection)
- Acceptance: Critical paths guarded; no test sprawl

#### WP-12: Production Docker & External API Key

**WP-12.1: docker-compose.prod.yml Override**
- Pre-built image references instead of `build:` directives
- Starter `prometheus.yml` scrape config

**WP-12.2: Disable External API Key Endpoints**
- **Decision: disable for MVP.** Do not ship with unlimited API key access.
- Add config flag or remove endpoints from routing until Phase 5 rate limiting is complete
- Acceptance: External API key surface is not exposed to production traffic

#### WP-13: Cleanup
- Verify `Event.Price`/`Event.CurrencyCode` not exposed in any UI form
- Final docs update pass
- Acceptance: No misleading or incomplete features visible to users

---

## NSwag Client Regeneration Checklist

Apply this whenever API surface changes (WP-3, WP-6, any visibility changes):

1. Update API (controllers, DTOs, handlers)
2. Run API to export OpenAPI: `dotnet run --project Explore.API` → `swagger.json` refreshed
3. Rebuild Blazor client: `dotnet build Explore.Blazor.Client` → `EventApiClient.g.cs` regenerated
4. Fix any consuming code broken by contract changes
5. Run client tests: `dotnet test --project Explore.Blazor.Client.Tests`

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Duplicate confirmation email due to outbox replay | Medium | Idempotent handler keyed by `(RegistrationConfirmed, registrationId)` |
| Redis silently not used in runtime | Medium | Startup log effective cache backend; warn if configured but unavailable |
| DataProtection migration not applied in self-hosted install | High | Explicit migration path; auto-migrate at startup via MigrationService |
| File ownership confusion across Blazor/Client/API | Medium | Pre-flight file map per WP in context doc |
| WP-9 scope expansion (draft/publish UX) | Medium | Split: MVP = explicit buttons only; defer beforeunload + advanced transitions |
| iCal timezone bugs | Medium | UTC normalization; explicit test cases for timed events |
| NSwag client drift after API changes | Medium | Formal regeneration checklist (above) |
| Outbox dispatcher wiring complexity | Low | LoggingOutboxMessageDispatcher already registered; routing dispatcher is additive |
| DataProtection migration on existing data | Low | New table, no data migration; one-time session invalidation acceptable |
| iCal library compatibility with .NET 10 | Low | Ical.Net targets netstandard2.0; verified compatible |

---

## Dependency Graph

```
Tier 1A (hard blockers):
  WP-1.4 (broken promise) ─── do FIRST
  WP-1.1 (Dockerfile) ─────── parallel
  WP-1.2 (DataProtection) ─── parallel
  WP-1.3 (Redis) ──────────── parallel
  → Gate A + Gate B smoke test immediately after

Tier 1B (hardening):
  WP-2 (Navbar Ph7) ──── parallel with Tier 2 start

Tier 2 (user-facing):
  WP-6 (iCal) ────────── no deps, start early
  WP-7 (Share verify) ── no deps
  WP-4 (My Regs enhance) ── after WP-6
  WP-5 (Post-Reg UX) ──── after WP-6 + WP-7
  WP-3 (Email) ──────── after WP-1.4
  WP-8 (HATEOAS) ────── no deps
  WP-9 (Draft/Publish) ── no deps
  → Gate C + Gate D after Tier 2
```

### Recommended Sprint Order (revised per review)

**Sprint 1 (Days 1-2):** WP-1 (all sub-tasks) → deploy smoke test (Gates A+B)
**Sprint 2 (Days 3-5):** WP-6 (calendar) + WP-7 (share verify) + WP-4 (enhance) + WP-5 (post-reg UX)
  → This closes the user loop quickly and visibly
**Sprint 3 (Days 6-9):** WP-3 (registration email) + WP-8 (HATEOAS fix)
  → Email is higher operational complexity; infra is stable by now
**Sprint 4 (Days 10-12):** WP-2 (navbar) + WP-9 (draft/publish) + WP-10/11/12/13 (remaining runway)
  → Gate C + Gate D verification

---

## Success Metrics

1. **All four gates pass** — Deployability, Session Integrity, Registration Truthfulness, Event Loop
2. **Zero broken promises** — Every UI text matches actual behavior
3. **Complete user loop** — Browse → Register → Confirm → Calendar → Share → Return
4. **Self-hosted deploy works** — `docker compose up` with or without Redis
5. **No random logouts** — DataProtection keys persist across restarts
6. **Test baseline holds** — All existing + new tests pass
7. **Operators can observe** — Cache backend logged, outbox metrics emitted, dead-letters visible

---

## Related Documents

- `dev/active/mvp-report.md` — Source readiness assessment
- `dev/active/hateoas-client-alignment/` — HATEOAS fix track
- `dev/active/external-api-access/` — API key track (disable for MVP)
- `dev/active/navbar-customization/` — Navbar track
- `docs/ARCHITECTURE.md` — System architecture
- `docs/OUTBOX_PATTERN.md` — Outbox implementation reference
- `docs/BLAZOR.md` — Blazor frontend patterns
- `docs/API.md` — API patterns and contracts
