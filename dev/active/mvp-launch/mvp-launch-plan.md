ABOUTME: Strategic implementation plan for closing all MVP gaps before production launch.
ABOUTME: Prioritized into tiered gates with phased work, acceptance criteria, and risk assessment.

# MVP Launch — Implementation Plan

> **Created:** 2026-03-28 | **Revised:** 2026-03-29 (architect review) | **Extended:** 2026-04-24 (codebase audit synthesis)
> **Branch:** `develop`
> **Goal:** Close all gaps identified in `dev/active/mvp-report.md` AND the 2026-04-24 parallel codebase audit to reach a shippable MVP for real organizers and event seekers.

---

## Executive Summary

The platform is ~65-70% MVP-complete. The architecture is production-grade (Clean Architecture, CQRS, BFF, multi-tenancy, HATEOAS, rate limiting, observability). What's missing falls into six categories:

1. **Infrastructure blockers** — Dockerfile .NET version mismatch, DataProtection key persistence, missing Redis in docker-compose
2. **Broken user promises** — Registration confirmation email never sent, yet UI says it will be
3. **Missing user-facing features** — No calendar integration, no post-registration flow, incomplete publish UX
4. **Public-surface polish gaps** — No sitemap.xml/robots.txt, no branded 404/500 error pages, no JSON-LD structured data, no PWA manifest, no email unsubscribe mechanism (GDPR risk), limited OG/Twitter tags outside EventDetail
5. **Registration-flow correctness gaps** — No capacity enforcement (over-registration possible), no automatic waitlist on full, no duplicate-registration guard, no organizer notifications
6. **Security/observability audit gaps** — PII access not audited, setup-secret endpoint not rate-limited, admin actions not audited, no BFF CSP header, no external-dependency health checks (Redis/Keycloak/SMTP)

This plan organizes work into **5 tiers across 25 work packages** with **7 explicit go/no-go release gates**.

### Corrections Applied (from 2026-03-29 review)
- **Email verification** is handled by **Keycloak** (not the application). AT Proto handle → PDS's responsibility.
- **MyRegistrations page already exists** at `/my/registrations` with cancel, search, virtualized cards.
- **Share functionality already exists** in EventDetail via Web Share API + clipboard fallback.
- **Admin onboarding already exists** (InstanceOnboarding, TenantOnboarding, StartupGate).

### Additional Findings (2026-04-24 codebase audit)
- **Legal pages exist** (Privacy, Terms, Community Guidelines) — good baseline, no License/Accessibility statement yet.
- **Cookie consent banner exists** — GDPR-compliant, non-blocking, equal Accept/Decline buttons.
- **Analytics bridge exists** — privacy-first (PostHog/Plausible/Umami support), consent-driven.
- **Audit log entity exists** — schema is there but admin-action/PII-access logging is missing.
- **Notification entity exists** — in-app notifications already implemented (MarkAsRead, Archive, Snooze handlers).
- **Registration intent aggregate exists** — parent `EventRegistrationIntent` + child `EventRegistration` rows with policy snapshot.
- **`Ical.Net`-compatible infrastructure not yet added** — library is green-lit via D7 but not installed.
- **No dedicated error pages** — only generic `Error.razor` (development-focused, not branded).
- **No sitemap.xml / robots.txt** — SEO baseline broken.
- **No JSON-LD on any page** — structured data missing.
- **All UI strings are hardcoded English** — no `IStringLocalizer` usage; `LanguagePicker.razor` exists but non-functional.
- **Obsolete HAL legacy fallback** — `MapMethodToAction()` still active for policies missing explicit `PermissionAction`.
- **Capacity fields exist** (`EventSession.MaxAudienceAttendees`, `CurrentAudienceAttendees`) but **not enforced** in registration handler.

### Key Architecture Decisions (from review)
- **DataProtection**: Blazor BFF only. Do NOT register in API project (API is bearer-only, never needs the same key ring).
- **Outbox payload**: Reference payload (IDs only). Handler fetches fresh data at dispatch time. Smaller payload, fresher data.
- **Redis**: In-memory fallback when unavailable. The app must work optimally without Redis. Context: self-hostable platform where minimal infra (Blazor + API + DB) must be sufficient, with Redis/Cerbos/Keycloak etc. as optional enhancements.

---

## Release Gates (Go/No-Go)

Before declaring MVP-ready, **all seven gates must pass**.

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
- [ ] Email contains working one-click unsubscribe link (RFC 8058 compliant)

### Gate D — Public Event Completion Loop
- [ ] Event detail loads for anonymous users
- [ ] User can share event (Web Share API / clipboard)
- [ ] User can download .ics calendar file
- [ ] User can view registration list after registering
- [ ] Draft events hidden from anonymous; Archived return 404
- [ ] Capacity-full sessions show clear waitlist state; over-registration is prevented
- [ ] Users cannot double-register for the same session

### Gate E — Legal & Compliance (NEW)
- [ ] Privacy Policy, Terms of Service, Community Guidelines reachable from footer + direct routes
- [ ] Cookie consent banner functional (Accept/Decline; preference persisted 180 days)
- [ ] All transactional emails include working one-click unsubscribe (RFC 8058 `List-Unsubscribe-Post`)
- [ ] User-preference-based email opt-outs respected at dispatch (blocked if user unsubscribed from category)
- [ ] Accessibility statement and License pages reachable (or explicitly waived in plan)

### Gate F — SEO & Discoverability (NEW)
- [ ] `/sitemap.xml` returns valid sitemap covering all Published events + static pages (respects tenant visibility)
- [ ] `/robots.txt` returns valid directives (allow indexing in prod, disallow in dev)
- [ ] EventDetail renders valid JSON-LD `schema.org/Event` (validates via Google Rich Results tool)
- [ ] OrganizationProfile renders valid JSON-LD `schema.org/Organization`
- [ ] OG tags present on Home, Landing, EventDetail, OrganizationProfile, OrganizationDetail
- [ ] Canonical URLs present on all public pages
- [ ] Branded error pages (404, 403, 500) replace generic Error.razor

### Gate G — Security Audit Trail (NEW)
- [ ] Setup-secret validation endpoint rate-limited (`setup_secret` policy: 5 attempts / 60s / IP)
- [ ] PII reads on UserPii/ActorPii write an audit log entry (who accessed whose PII, with correlation ID)
- [ ] Admin/instance/tenant setting changes write audit log entries
- [ ] Authorization denial events logged with principal + resource + action
- [ ] BFF responses carry a CSP header (`default-src 'self'; ...`)
- [ ] Audit log queries restricted: instance admin = full; tenant admin = their tenant only; users = own actions

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
- Event sharing (Web Share API + clipboard + OG meta tags on EventDetail)
- Admin onboarding (instance + tenant setup wizards)
- Legal pages (Privacy, Terms, Community Guidelines)
- Cookie consent banner (GDPR-compliant, 180-day lifetime)
- Privacy-first analytics bridge (PostHog/Plausible/Umami + relay endpoint)
- Notification entity + read/archive/snooze handlers
- Audit log entity + repository
- Multi-tenancy isolation (48 entities with named query filters; API-authoritative resolution)

### What's Broken or Missing
| # | Gap | Severity | Effort | WP |
|---|-----|----------|--------|----|
| 1 | Blazor Dockerfile uses .NET 9, project targets net10.0 | BLOCKER | 1 hour | WP-1.1 |
| 2 | DataProtection keys not persisted (random logouts) | BLOCKER | 0.5 day | WP-1.2 |
| 3 | Redis missing from docker-compose.yml (must degrade gracefully) | BLOCKER | 1 hour | WP-1.3 |
| 4 | Registration UI promises email that never sends | BLOCKER | 1 hour (remove) / 3 days (implement) | WP-1.4 / WP-3 |
| 5 | No post-registration "What Next" flow | HIGH | 1 day | WP-5 |
| 6 | No iCal/.ics calendar integration | HIGH | 1-2 days | WP-6 |
| 7 | HATEOAS client violation in OrganizationDetails | RISK | 1.5 days | WP-8 |
| 8 | No explicit "Save Draft" vs "Publish" UX | MEDIUM | 1-2 days | WP-9 |
| 9 | My Registrations needs calendar button + discoverability check | LOW | 0.5 day | WP-4 |
| 10 | Share on EventCard (list view) — verify/add | LOW | 0.5 day | WP-7 |
| 11 | External API Key Phase 5 incomplete | RISK | disable for MVP | WP-12 |
| 12 | Navbar Customization Phase 7 incomplete | RISK | 2 days | WP-2 |
| 13 | No email unsubscribe mechanism (GDPR/CAN-SPAM risk) | BLOCKER | 1.5 days | WP-14 |
| 14 | No branded 404/403/500 pages (generic Error.razor only) | HIGH | 1 day | WP-15 |
| 15 | No sitemap.xml / robots.txt (SEO broken on launch) | HIGH | 1 day | WP-16 |
| 16 | Capacity not enforced → over-registration possible | HIGH | 1.5 days | WP-17 |
| 17 | No health checks for Redis/Keycloak/SMTP (silent dep failure) | MEDIUM | 1 day | WP-18 |
| 18 | Security audit trail incomplete (PII access, admin actions, setup-secret) | HIGH | 1.5 days | WP-19 |
| 19 | No JSON-LD on EventDetail/OrgProfile; missing OG tags on Landing/Home | MEDIUM | 1 day | WP-20 |
| 20 | Only 5 E2E smoke tests; no critical-flow E2E coverage | MEDIUM | 2 days | WP-21 |
| 21 | No snapshot tests for HATEOAS response contracts | MEDIUM | 1 day | WP-22 |
| 22 | Low ARIA density, no breadcrumbs, no focus management | LOW | 1 day | WP-23 |
| 23 | No PWA manifest (cannot install; no theme-color) | LOW | 0.5 day | WP-24 |
| 24 | Placeholder images (placehold.co, landing_image_nonuser.png) + TODO comments | LOW | 0.5 day | WP-25 |
| 25 | Obsolete `RoleHelper.CanManage` cousins, HAL `MapMethodToAction()` legacy fallback | RISK | 0.5 day (rolled into WP-8 / WP-19.7) | WP-8 / WP-19 |

### Explicitly Deferred (Post-MVP, Documented)
| # | Item | Reason |
|---|-----|--------|
| D1 | i18n (IStringLocalizer + resx + RTL) | All UI hardcoded English is acceptable for v1 English-only launch; full locale coverage deferred to post-MVP |
| D2 | Service worker + offline mode | Manifest only for MVP; offline cache strategy deferred |
| D3 | Soft-delete retention cleanup job | No pressing volume; defer |
| D4 | Outbox archive/cleanup job | Same |
| D5 | Orphan blob cleanup | Same |
| D6 | RSS/Atom feeds | Discovery polish; defer |
| D7 | Calendar subscription feeds (tenant-scoped .ics streams) | Single-event download is MVP; streams are post-MVP |
| D8 | Webhooks / push / SMS notification channels | Email + in-app only for MVP |
| D9 | Attendance tracking / check-in / QR | Out of MVP scope (see `dev/active/mvp-report.md` §5) |
| D10 | Payments / ticketing | Out of MVP scope |
| D11 | Waitlist auto-promotion on cancel | MVP = accept waitlist state; organizer manually promotes. Auto-promotion deferred |
| D12 | Organizer capacity alerts (email when near full) | Defer to post-MVP |
| D13 | Bulk registration approval UI | Per-item only for MVP |
| D14 | Registration CSV export | Organizer can query via API; UI export deferred |
| D15 | Mutation testing (Stryker.NET) | Coverage focus only for MVP |
| D16 | Audit log retention policy (90-day cleanup) | Table grows slowly for MVP volume |
| D17 | In-app docs viewer | External GitHub docs sufficient for MVP |

### In-Flight Work (Active Tracks)
1. **HATEOAS Client Alignment** (`dev/active/hateoas-client-alignment/`) — 5 phases, all not started. Phase 3 is MVP-critical (folds into WP-8).
2. **External API Access** (`dev/active/external-api-access/`) — Phases 0-4 complete. **Decision: disable endpoints for MVP** (WP-12).
3. **Navbar Customization** (`dev/active/navbar-customization/`) — Phases 1-6 complete, Phase 7 has open tasks (WP-2).
4. **Blazor Localization** (`dev/active/blazor-localization/`) — Explicitly deferred per deferral D1.
5. **Organizer Email Consent** (`dev/active/organizer-email-consent/`) — Overlaps with WP-14 scope; reconcile before starting WP-14.
6. **Session Series UX** (`dev/active/session-series-ux/`) — Out of MVP scope unless already shipped.
7. **RabbitMQ Messaging** (`dev/active/rabbitmq-messaging/`) — Out of MVP scope (in-process outbox sufficient).

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
- **Unsubscribe slot:** template MUST accept an unsubscribe URL injected by WP-14 and render it both in body and in `List-Unsubscribe` header context
- Acceptance: Builder renders clean HTML given event + registration data + unsubscribe URL

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
- **Consent check:** call WP-14 opt-out service before sending (skip + log if user unsubscribed from "registration-confirmations" category)
- Structured logging: `RegistrationId`, `EventId`, `UserId`, `TenantId`, `OutboxMessageId`
- Acceptance: Gate C passes (email sent, no duplicates on replay, dead-letter observable, unsubscribe respected)

**WP-3.5: Observability**
- Metric counter: `outbox.messages.processed` with dimensions `{event_type, outcome, tenant_id}`
- Metric counter: `outbox.messages.failed` with dimensions `{event_type, tenant_id}`
- Metric counter: `outbox.messages.skipped_opt_out` with dimensions `{event_type, category, tenant_id}`
- Dead-letter messages visible via `GetFailedEntries`
- Acceptance: Operators can monitor outbox health + opt-out rates

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
- **Legacy HAL fallback removal (NEW):** sweep every `LinkDefinition` derivation path for `PermissionResourceKind` set without `PermissionAction`; ensure all policies call `RequirePermission("resource", "action")` explicitly so the obsolete `MapMethodToAction()` path can be deleted. Mark `[Obsolete]` → hard-delete.
- Acceptance: OrganizationDetails derives action affordance from HAL links; no RoleHelper for action gating; `MapMethodToAction` no longer reachable

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

### Tier 2B — Launch-Blocking Polish (NEW, 2026-04-24)
*Estimated: 4-5 days. Discovered in codebase audit; cannot launch without these.*

#### WP-14: Email Unsubscribe & GDPR Compliance (1.5 days)

**Why MVP-critical:** Shipping transactional email without a working unsubscribe mechanism is a GDPR + CAN-SPAM violation. MUST land **before** WP-3 ships any email.

**WP-14.1: Notification Preferences Schema**
- Check if `UserNotificationPreferences` already exists (SettingsNotifications.razor references `NotificationPreferences`); reuse if present
- Required preference categories (minimum viable):
  - `registration-confirmations` — default: opt-in
  - `event-reminders` — default: opt-in (post-MVP when reminders ship)
  - `event-updates` — default: opt-in (post-MVP)
  - `organizer-announcements` — default: opt-in
- Table columns: `UserId` (FK), `Category` (string), `IsEnabled` (bool), `UpdatedAt`, `UpdatedBy`
- EF migration if new table

**WP-14.2: Unsubscribe Token Service**
- New: `Explore.Infrastructure/Mail/Unsubscribe/UnsubscribeTokenService.cs`
- Use `ITimeLimitedDataProtector` (DataProtection-scoped; 180-day token lifetime)
- Token payload: `{ userId, category, tenantId, issuedAt }`
- Signing + encryption via existing DataProtection key ring (WP-1.2 dependency)
- Timing-safe validation; treat invalid/expired tokens as "already unsubscribed" for UX

**WP-14.3: Unsubscribe Endpoint (GET + POST)**
- New: `Explore.API/Controllers/EmailUnsubscribeController.cs`
- `GET /api/email/unsubscribe?token=...` → `[AllowAnonymous]` + rate-limited (`global` policy)
- Returns branded confirmation page with CTA to re-subscribe or manage all preferences
- `POST /api/email/unsubscribe?token=...` → RFC 8058 `List-Unsubscribe=One-Click` compliance
- Updates `UserNotificationPreferences` for (userId, category)
- Writes audit log entry (`PreferenceChange`, actor=system, delegated=token-subject)

**WP-14.4: Email Header Injection**
- Update `RegistrationConfirmedEmailBuilder` to accept injected unsubscribe URL
- Set SMTP headers at `IEmailService` call site:
  - `List-Unsubscribe: <https://.../api/email/unsubscribe?token=...>, <mailto:unsubscribe@...>`
  - `List-Unsubscribe-Post: List-Unsubscribe=One-Click`
- Render same URL in visible email footer

**WP-14.5: Dispatch-Time Consent Check**
- `RegistrationConfirmedOutboxHandler` (WP-3.4) must query `UserNotificationPreferencesRepository.GetAsync(userId, "registration-confirmations")` and skip + log if opted-out
- Emit `outbox.messages.skipped_opt_out` counter

**WP-14.6: Tests**
- Unit: token round-trip (encrypt→decrypt), expiration, tamper detection
- Integration: end-to-end unsubscribe via GET + POST
- Integration: handler skips dispatch when user opted-out
- bUnit: Settings.razor / notification preferences page reflects opt-out state

**Acceptance:** Gate C + Gate E "unsubscribe works" pass; first transactional email includes working link; toggling preference prevents further emails in that category.

**Reconciliation:** merge any existing scope in `dev/active/organizer-email-consent/` before starting.

#### WP-15: Branded Error Pages (1 day)

**WP-15.1: Not Found (404)**
- New: `Explore.Blazor.Client/Pages/Errors/NotFound.razor` with `@page "/errors/404"`
- Add catch-all route in `Routes.razor`: `<Route Template="*" ...>` or `@page "/{*route}"` fallback
- Branded layout: logo, "Page Not Found" header, search bar, CTAs ("Return Home", "Browse Events")
- PageTitle "Not Found — {TenantName}" and `<meta name="robots" content="noindex">`

**WP-15.2: Unauthorized (403)**
- New: `Explore.Blazor.Client/Pages/Errors/Unauthorized.razor` with `@page "/errors/403"`
- Shown when user is authenticated but lacks permission (HAL link absent)
- CTAs: "Request Access" (if applicable), "Return Home"

**WP-15.3: Server Error (500)**
- New: `Explore.Blazor.Client/Pages/Errors/ServerError.razor` with `@page "/errors/500"`
- Enhance existing `Explore.Blazor/Components/Pages/Error.razor` to render branded content
- Display correlation ID for support; hide stack traces in production
- CTAs: "Return Home", "Contact Support" (prefilled with correlation ID)

**WP-15.4: Status Code Pages Middleware**
- In `Explore.Blazor/Program.cs`: `app.UseStatusCodePagesWithReExecute("/errors/{0}")`
- Verify it plays well with Blazor Server interactive routes (test 404 via direct URL + SPA navigation)

**WP-15.5: Tests**
- bUnit: each error page renders with correct copy + CTA
- Integration: direct URL to `/nonexistent` returns branded 404 page
- Integration: unauthenticated request to `[Authorize]` endpoint returns branded 403

**Acceptance:** Gate F "branded error pages" passes.

#### WP-16: SEO Foundation (1 day)

**WP-16.1: Sitemap Controller**
- New: `Explore.API/Controllers/SitemapController.cs`
- `GET /sitemap.xml` → `[AllowAnonymous]`, `Content-Type: application/xml`
- Output: static pages (Home, About, Contact, Privacy, Terms, Community Guidelines) + all **Published** events (respect tenant visibility + soft-delete filter)
- Per-URL: `<loc>`, `<lastmod>` (Event.UpdatedAt), `<changefreq>`, `<priority>`
- Tenant-aware: resolve canonical host from current tenant's custom domain/subdomain
- Output-cache 30 minutes

**WP-16.2: Robots.txt**
- New: `Explore.Blazor/wwwroot/robots.txt` (static) OR `Explore.API/Controllers/RobotsController.cs` (dynamic for per-tenant customization)
- Prod default: `User-agent: * / Allow: / / Sitemap: https://{host}/sitemap.xml`
- Dev default: `Disallow: /` (prevent indexing dev instances)

**WP-16.3: Canonical URLs on Public Pages**
- Add `<link rel="canonical" href="...">` to `Home.razor`, `LandingPageForNonUsers.razor`, `LandingPageForUsers.razor`, `OrganizationProfile.razor`, `OrganizationDetails.razor`, `EventList.razor`
- Use the same canonical URL helper from EventDetail; centralize if not already

**WP-16.4: Tests**
- Integration: `GET /sitemap.xml` returns valid XML with all published events
- Integration: Draft/Archived events absent from sitemap
- Integration: tenant A sitemap does not leak tenant B events (multi-tenant instance)
- Integration: `GET /robots.txt` returns expected content per environment

**Acceptance:** Gate F "sitemap + robots.txt" passes.

---

### Tier 3 — Must-Have Before Public Announcement
*Estimated: 5-6 days*

#### WP-17: Capacity Enforcement & Basic Waitlist (1.5 days)

**Why important:** Without capacity checks, an event with `MaxAudienceAttendees=50` can accept 500 registrations silently. This is a data-integrity + user-trust issue, not cosmetic.

**WP-17.1: Capacity Check in Registration Handler**
- File: `CreateEventRegistrationCommandHandler.cs`
- For each derived session in `ResolveChildSessionsAsync`, query current registration count + `MaxAudienceAttendees`
- **Concurrency-safe path:** use row-level SQL (`SELECT ... FOR UPDATE` on session row) or optimistic concurrency via `EventSession.ConcurrencyStamp` + retry loop
- Behavior options (decision required):
  - **Option A (Preferred):** auto-waitlist if any session is full → parent intent approval status = `Waitlisted`; children for full sessions get `Waitlisted`, others `Approved`/`Pending`
  - **Option B:** reject whole registration if any session is full (simpler, but worse UX)
- We choose **Option A** for MVP — see D22

**WP-17.2: Atomic Attendee Count Update**
- On successful child registration that results in `Approved`, increment `EventSession.CurrentAudienceAttendees` in same transaction
- Use raw SQL `UPDATE ... WHERE CurrentAudienceAttendees < MaxAudienceAttendees RETURNING ...` OR EF concurrency stamp + retry
- On cancellation (DELETE), decrement the count

**WP-17.3: Duplicate Registration Prevention**
- Unique index on `EventRegistration(UserId, EventSessionId)` where `IsDeleted=false`
- Handler short-circuits if duplicate found (return existing intent id — idempotent)
- Migration required

**WP-17.4: UI Feedback**
- `EventRegistration.razor`: if any session is full, show "Join waitlist" instead of "Register"
- Post-registration success: if `ApprovalStatus=Waitlisted`, render waitlist copy instead of confirmation copy
- `MyRegistrations.razor`: waitlist badge already exists (verify)

**WP-17.5: Tests**
- Unit: handler rejects over-capacity, creates waitlisted entry instead
- Integration: concurrent POSTs do not exceed capacity (race-condition test with 10 parallel requests)
- Integration: duplicate registration returns existing intent
- Integration: cancellation decrements count

**Acceptance:** Gate D "capacity-full shows waitlist; over-registration prevented; no double-register" passes.

**Deferred (see D11, D12):** waitlist auto-promotion when user cancels; capacity-alert emails to organizer.

#### WP-18: External Dependency Health Checks (1 day)

**WP-18.1: Redis Health Check**
- In `Explore.API/Program.cs` and `Explore.Blazor/Program.cs`: `.AddRedis(connectionString, tags: ["ready"])` if Redis configured
- Skip registration when Redis not configured (in-memory mode)
- Respect D8 graceful degradation: health check is `Degraded` (not `Unhealthy`) when Redis unreachable AND in-memory fallback active

**WP-18.2: Keycloak OIDC Discovery Health Check**
- Custom `HealthCheck` that HEADs `{authority}/.well-known/openid-configuration` with 5s timeout
- Tagged `ready`

**WP-18.3: SMTP Health Check**
- Reuse existing `SmtpEmailService.TestConnectionAsync()`
- Custom `HealthCheck` wraps it with 10s timeout
- Tagged `ready`; `Degraded` (not `Unhealthy`) on failure since email is async via outbox

**WP-18.4: Cerbos Health Check (Conditional)**
- If `CerbosSettings.Enabled == true`: register gRPC health check against Cerbos endpoint
- Tagged `ready`; failure mode aligned with existing fallback service

**WP-18.5: Operator Docs**
- Update `docs/TROUBLESHOOTING.md` (or equivalent) with health-check interpretation table
- `/health` responds 200 Degraded vs 503 Unhealthy; document SLO per dependency

**WP-18.6: Tests**
- Integration: `/health` returns 200 Healthy with all deps up
- Integration: `/health` returns 200 Degraded when Redis unreachable + in-memory fallback (NOT 503)
- Integration: `/alive` returns 200 regardless of external deps (liveness vs readiness split)

**Acceptance:** Gate A "all deps observable via /health" passes.

#### WP-19: Security Audit Trail Hardening (1.5 days)

**WP-19.1: Rate-Limit Setup-Secret Validation**
- Apply existing `setup_secret` policy (5/60s/IP) to:
  - `/api/InstanceOnboarding/validate-secret`
  - Any other endpoint accepting `X-Setup-Secret` header
- After 3 consecutive failures from the same IP, emit warning log with `ip`, `user-agent`, `correlation-id`

**WP-19.2: PII Access Audit**
- Wrap `UserPiiRepository` and `ActorPiiRepository` read methods with audit logging
- For every read, write to `AuditLog` with: `EntityType="UserPii"`, `EntityId=userId`, `Action="Read"`, `ActorId=principal.Sub`, `Timestamp`, `CorrelationId`, `Purpose` (derived from calling handler name)
- Self-reads by the owner: log with `IsSelfAccess=true` flag to filter noise
- Gate on existing `AuditLogRepository`

**WP-19.3: Admin Action Audit**
- Add audit-logging `IPipelineBehavior<TRequest, TResponse>` or equivalent decorator for all commands under:
  - `Explore.Application/Features/InstanceSettings/`
  - `Explore.Application/Features/TenantSettings/`
  - `Explore.Application/Features/Roles/`
  - `Explore.Application/Features/InstanceOnboarding/`
  - `Explore.Application/Features/TenantOnboarding/`
- Log: entity type, old vs new (JSON diff via `AffectedColumns` + `OldValues`/`NewValues`), actor, timestamp, correlation ID

**WP-19.4: Authorization Denial Audit**
- In `FallbackAuthorizationService.IsAllowedAsync`: on `Deny`, write audit log entry `Action="AuthorizationDenied"` with principal + resource + action
- Match in Cerbos-backed path via adapter
- Do NOT log allowed decisions (volume prohibitive; use metrics counter instead)

**WP-19.5: BFF CSP Header**
- New middleware: `Explore.Blazor/Middleware/BffCspMiddleware.cs`
- CSP for HTML responses: `default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'wasm-unsafe-eval'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'`
- Adjust `script-src` for Blazor WASM boot (needs `wasm-unsafe-eval`) and MudBlazor inline styles
- Register before `UseStaticFiles`; scoped to non-API Blazor responses

**WP-19.6: Audit Log Access Control**
- New permission: `audit_log:read` with tenant-admin and instance-admin bindings
- Instance admin: sees all tenants (IgnoreQueryFilters for Tenant filter, preserve SoftDelete filter)
- Tenant admin: sees their tenant only (via default filter)
- Regular users: can query own audit entries via dedicated endpoint (`/api/users/me/audit-log`) — scoped by actor claim

**WP-19.7: Remove Obsolete HAL Legacy Fallback**
- Ensure all 36 link policies call `RequirePermission("resource", "action")` explicitly
- Delete `[Obsolete] MapMethodToAction()` method and its callsites
- Architecture test to prevent reintroduction (match test: `AllLinkPoliciesHaveExplicitPermissionActions`)

**WP-19.8: Tests**
- Integration: setup-secret endpoint returns 429 after 5 attempts
- Integration: PII read writes audit entry
- Integration: setting change writes audit entry with old vs new JSON
- Integration: authorization denial writes audit entry
- Integration: BFF response carries CSP header
- Integration: tenant admin cannot query other tenant's audit logs
- Architecture: all link policies have explicit action set

**Acceptance:** Gate G passes fully.

#### WP-20: Public Page SEO & OG Polish (1 day)

**WP-20.1: JSON-LD Event Schema**
- `EventDetail.razor`: render `<script type="application/ld+json">` with `schema.org/Event`
  - Required: `@type`, `name`, `startDate`, `endDate`, `eventStatus` (EventScheduled/Postponed/Cancelled), `eventAttendanceMode` (OfflineEventAttendanceMode/OnlineEventAttendanceMode/MixedEventAttendanceMode)
  - `location`: `Place` for physical, `VirtualLocation` for online, both for hybrid
  - `organizer`: `Organization` or `Person`
  - `offers`: include if `Event.Price > 0` (else omit entirely — do not advertise `CurrencyCode` for unused price field)
  - `image`: featured image URL (absolute)

**WP-20.2: JSON-LD Organization Schema**
- `OrganizationProfile.razor`: `schema.org/Organization`
  - `name`, `url`, `logo`, `description`, `sameAs` (social links)

**WP-20.3: JSON-LD Breadcrumb Schema**
- Detail pages: `schema.org/BreadcrumbList` for Home → List → Detail path
- Add alongside `EventDetail`, `OrganizationProfile`, `OrganizationDetails`

**WP-20.4: OG/Twitter Meta Tags**
- Add to `Home.razor`, `LandingPageForNonUsers.razor`, `LandingPageForUsers.razor`, `OrganizationProfile.razor`, `OrganizationDetails.razor`
- Pattern mirrors EventDetail: `og:title`, `og:description`, `og:type`, `og:url`, `og:image`, `og:site_name`, `twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`
- Tenant-aware: brand name + logo from `PublicExperienceService`

**WP-20.5: Tests**
- Integration: scrape `/events/{id}` and validate JSON-LD via schema.org validator (or a sample validator library)
- Integration: assert OG tags present on Home, EventDetail, OrganizationProfile

**Acceptance:** Gate F "JSON-LD + OG tags everywhere" passes.

---

### Tier 4 — Test Coverage & Polish
*Estimated: 4-5 days*

#### WP-21: E2E Critical-Flow Tests (2 days)

Focus on the four flows the 2026-04-24 test-coverage audit flagged as "CRITICAL (block MVP)".

**WP-21.1: Registration End-to-End**
- `Explore.Blazor.Client.E2ETests/CriticalFlows/RegistrationFlowTests.cs`
- Playwright scenario: login → browse events → open event → register → confirmation UI → receive email (via outbox + SMTP mock) → open My Registrations → see registration
- Uses `AppHostFixture` (full Aspire stack) + `PostgreSqlContainerFixture`
- Validates Gates C + D end-to-end

**WP-21.2: Multi-Tenancy Isolation**
- Two tenant contexts set up; tenant A creates event → tenant B (different subdomain/slug) cannot see/access it via UI or API
- Validates that query filters plus middleware enforce isolation across the whole stack

**WP-21.3: Authorization Enforcement**
- Authenticated user WITHOUT edit permission navigates to org → UI does not render Edit button → direct API mutation attempt returns 403
- Gates WP-8 + WP-19 outputs

**WP-21.4: BFF Token-Forwarding Chain**
- Login via BFF → token forwarded through YARP → API receives valid JWT + tenant header → response returns to BFF with HAL links → Blazor renders correctly
- Exercises cookie handling, refresh, anti-forgery

**Acceptance:** All four flows green in CI; no flakiness (3 consecutive runs).

#### WP-22: Snapshot Tests for HATEOAS Contracts (1 day)

**WP-22.1: Add Snapshot Library**
- Install `Verify.TUnit` (or `Verify.Xunit` if TUnit adapter unavailable) in `Event.API.IntegrationTests`
- Configure snapshot directory: `tests/snapshots/`

**WP-22.2: Snapshot EventDto Responses**
- Anonymous GET event detail → snapshot
- Authenticated GET event detail → snapshot (more links)
- Organizer GET event detail → snapshot (edit/delete links present)
- List response (first 5 items) → snapshot

**WP-22.3: Snapshot OrganizationDto, UserDto, EventRegistrationDto**
- Public GET + authenticated GET for each

**WP-22.4: Snapshot ProblemDetails**
- 400 validation error, 401 unauthenticated, 403 forbidden, 404 not found, 500 generic → snapshots
- Validates RFC 7807 shape stability

**WP-22.5: PR Policy**
- Document snapshot-review policy in `docs/TESTING.md`: any snapshot change must be visually reviewed in PR diff

**Acceptance:** Baseline snapshots committed; CI re-runs confirm stability.

#### WP-11: Targeted Test Coverage (existing, now concrete)

Focus on high-value tests only. Avoid test-coverage ambition trap.

- [ ] Registration flow unit tests (approval policy resolution, waitlist derivation, capacity enforcement from WP-17)
- [ ] Visibility rules (Draft hidden, Archived 404) — API integration tests
- [ ] HATEOAS action gating (OrganizationDetails) — bUnit
- [ ] Calendar endpoint (valid .ics, 404 for non-public, UTC normalization)
- [ ] Session persistence regression (DataProtection key ring survives pod recycle)
- [ ] Unsubscribe flow end-to-end (WP-14)
- [ ] Rate limit enforcement on setup-secret (WP-19.1)
- [ ] Coverage target: 343 handlers → at least 1 test each (raise from current 39%)

Acceptance: Critical paths guarded; no test sprawl; handler coverage ≥70%.

---

### Tier 5 — Final Polish (Before Public Announcement)
*Estimated: 2-3 days*

#### WP-23: Accessibility Polish (1 day)

**WP-23.1: Breadcrumbs**
- Add `MudBreadcrumbs` to `EventDetail.razor`, `OrganizationDetails.razor`, `OrganizationProfile.razor`, `UserProfile.razor`, `MyRegistrations.razor`
- Pair with JSON-LD BreadcrumbList from WP-20.3 so structured data stays consistent

**WP-23.2: ARIA Landmarks**
- Ensure `<main>` has `aria-label="Main content"`, `<nav>` has `aria-label="Primary navigation"`, `<aside>` (sidebar) has `aria-label="Sidebar"`
- Apply in `MainLayout.razor` and `SetupLayout.razor`

**WP-23.3: Focus Management**
- Add route-change hook that sets focus to the page's first `<h1>` after SPA navigation
- Shared in a `FocusOnNavigate` component

**WP-23.4: Form Validation ARIA**
- Audit forms (CreateEvent, EditEvent, CreateOrganization): ensure validation messages have `aria-describedby` linking them to inputs

**WP-23.5: Lighthouse Audit**
- Run Lighthouse a11y audit on Home, EventList, EventDetail, CreateEvent, MyRegistrations, OrganizationDetails
- Fix findings with score < 90
- Snapshot Lighthouse score to prevent regression

**Acceptance:** Key pages score ≥90 on Lighthouse a11y.

#### WP-24: PWA Manifest Only (0.5 day)

> Scope strictly limited per D14: manifest only; no service worker, no offline mode for MVP.

**WP-24.1: Manifest**
- New: `Explore.Blazor/wwwroot/manifest.json`
- Fields: `name`, `short_name`, `description`, `start_url=/`, `display=standalone`, `background_color`, `theme_color`, `icons` (192/256/384/512 sizes)
- Icons generated from tenant brand logo (default to ISLAMU logo for instance default)
- Link from `App.razor` head: `<link rel="manifest" href="/manifest.json">`
- Add `<meta name="theme-color" content="...">` matching brand primary

**WP-24.2: Tenant Awareness**
- Per-tenant brand-aware manifest via controller endpoint (`/manifest.json`) rather than static file — reads `PublicExperienceSettings` for current tenant
- Cache 5 minutes

**Acceptance:** Lighthouse PWA audit shows "Manifest: yes" (install prompt available).

#### WP-25: Placeholder & TODO Cleanup (0.5 day)

**WP-25.1: Replace Placeholder Images**
- `MyRegistrations.razor`: swap `placehold.co` reference with real event image or branded fallback pattern
- `LandingPageForNonUsers.razor`: confirm `image/landing_image_nonuser.png` is a real asset in `wwwroot/image/`; if missing, provide replacement or remove reference

**WP-25.2: Resolve Critical TODOs**
- Sweep `EventList.razor`, `EventEdit.razor`, `CreateEvent.razor` for `TODO`/`FIXME` comments
- For each: either fix (if scope ≤30 min) OR file follow-up GitHub issue and link from comment

**WP-25.3: Price/CurrencyCode Audit**
- Ensure `Event.Price` / `Event.CurrencyCode` are NOT exposed in UI create/edit forms (per existing WP-13 acceptance)
- Decide: keep in domain for post-MVP payment work, OR remove in a follow-up migration
- Document decision in `dev/_journal/journal.md`

**WP-25.4: Final Docs Pass**
- Update `README.md` quickstart to mention `docker compose up` + Redis optionality
- Ensure `docs/API.md` reflects new endpoints (sitemap, calendar, unsubscribe)

**Acceptance:** No placeholder URLs, no lingering TODOs without tickets, Price/Currency decision documented.

---

### Tier 6 — Pre-Existing Deferred Items
*Estimated: 2 days (from original plan's Tier 3)*

#### WP-10: User Welcome/Onboarding — Gap Analysis (1 day)

> **PARTIALLY EXISTS:** Admin onboarding covers instance/tenant setup. User-level onboarding may be missing.

- Decision rule: if admin onboarding satisfies launch need, close fast. Do not invent a big first-run system late in MVP.
- If user-level gap exists: lightweight first-login detection + welcome modal
- Acceptance: First-time users have a guided path or the gap is documented as post-MVP

#### WP-12: Production Docker & External API Key (1 day)

**WP-12.1: docker-compose.prod.yml Override**
- Pre-built image references instead of `build:` directives
- Starter `prometheus.yml` scrape config
- Redis profile honors D8 (optional but recommended)
- Acceptance: `docker compose -f docker-compose.prod.yml up` works against pushed images

**WP-12.2: Disable External API Key Endpoints**
- **Decision: disable for MVP.** Do not ship with unlimited API key access.
- Add config flag `ExternalApiKeys:Enabled=false` default, or remove endpoints from routing until Phase 5 rate limiting is complete
- Acceptance: External API key surface is not exposed to production traffic

#### WP-13: Cleanup (rolled into WP-25; retained as checklist anchor)

Covered by WP-25.

---

## NSwag Client Regeneration Checklist

Apply this whenever API surface changes (WP-3, WP-6, WP-14, WP-16, any visibility changes):

1. Update API (controllers, DTOs, handlers)
2. Run API to export OpenAPI: `dotnet run --project Explore.API` → `swagger.json` refreshed
3. Rebuild Blazor client: `dotnet build Explore.Blazor.Client` → `EventApiClient.g.cs` regenerated
4. Fix any consuming code broken by contract changes
5. Run client tests: `dotnet test --project Explore.Blazor.Client.Tests`

---

## Extended Decision Log (NEW, D11–D22)

### D11: i18n Explicitly Deferred
- **Decision:** Hardcoded English only for v1 launch. No `IStringLocalizer`, no `.resx`, no RTL detection.
- **Rationale:** Launching v1 to English-speaking audience; full locale coverage is a 1-2 week project on its own.
- **Revisit:** After MVP launch, prioritize if non-English community adoption materializes. Existing `LanguagePicker.razor` component stays in place (hidden or disabled) as a hook.
- **Blazor-localization track:** Formally parked. Reference `dev/active/blazor-localization/`.

### D12: Capacity Enforcement Scope
- **MVP:** Prevent over-registration via atomic SQL check + auto-waitlist when any session is full.
- **Post-MVP:** Auto-promote waitlist on cancellation; capacity-alert emails to organizer; bulk approval UI; CSV export.
- **Rationale:** MVP must prevent data-integrity bugs; automation can follow without user-facing surprise.

### D13: Unsubscribe Mechanism
- **Implementation:** Per-category tokens (not a single kill-switch). Categories: `registration-confirmations`, `event-reminders` (future), `event-updates` (future), `organizer-announcements`.
- **One-click compliance:** RFC 8058 `List-Unsubscribe` + `List-Unsubscribe-Post` headers; GET link in email body.
- **Token encryption:** `ITimeLimitedDataProtector` via BFF's DataProtection key ring (reuses WP-1.2 infra). 180-day lifetime.
- **Tamper-safe:** invalid/expired tokens treated as "already unsubscribed" for UX; abuse caught by rate limit.

### D14: PWA Scope
- **MVP:** `manifest.json` only (makes app installable).
- **Post-MVP:** Service worker, offline caching, background sync, push notifications.
- **Rationale:** Service worker is a long-term commitment (cache invalidation is hard); defer until offline demand proves real.

### D15: Health-Check Strategy
- **Ready tagged (affects `/health`):** Database, Redis (if enabled), Keycloak OIDC discovery, SMTP.
- **Live tagged (affects `/alive`):** Self-check + Shutdown graceful-degradation.
- **Degraded vs Unhealthy:** Redis + SMTP report `Degraded` when the app has a working fallback (in-memory cache; outbox retries); `Unhealthy` would force K8s to mark the pod NotReady.
- **Cerbos:** conditional registration only if `CerbosSettings.Enabled==true`.

### D16: Audit-Log Access Control
- **Instance admin:** full visibility across all tenants.
- **Tenant admin:** only their tenant's audit entries.
- **Regular user:** own actions only via `/api/users/me/audit-log`.
- **Permission key:** `audit_log:read` (tenant-scoped) + `audit_log:read_all` (instance-scoped).

### D17: Setup-Secret Rate-Limiting Policy
- Reuse existing `setup_secret` policy (5/60s/IP) — no new policy needed.
- Apply to `validate-secret` endpoint; emit a warning log after 3 consecutive failures with `ip`, `user-agent`, `correlation-id`.
- Consider CAPTCHA post-MVP if brute-force attempts show up in logs.

### D18: Error-Page Strategy
- Three dedicated routes: `/errors/404`, `/errors/403`, `/errors/500`.
- Middleware re-executes status code pages for non-interactive responses.
- Pages branded with tenant logo + site name; display correlation ID on 500.
- `meta robots noindex` on all error pages to avoid SEO pollution.

### D19: RSS/ICS Feeds Deferred
- **MVP:** Single-event `.ics` download (WP-6).
- **Post-MVP:** Organization-level `.ics` feed (all org events), tenant-level `.ics`, RSS/Atom for discovery.
- **Rationale:** Single download closes the immediate user need; stream feeds are a discovery-polish feature.

### D20: Snapshot-Testing Library
- **Choice:** `Verify.TUnit` v26+ if TUnit adapter available; fall back to `Verify.Xunit` with a bridging adapter if not.
- **Snapshot location:** `tests/snapshots/` (shared across projects).
- **Review policy:** any snapshot diff must be PR-reviewed visually; flag snapshot-only changes in PR description.

### D21: Placeholder Asset Policy
- **Production rule:** no `placehold.co` references, no missing image paths. Every image either resolves or uses a branded CSS fallback pattern.
- **Dev/tenant fallback:** when tenant hasn't uploaded a logo, use instance default logo (not a placeholder service).

### D22: Capacity Enforcement Mode (Chosen: Option A)
- **Auto-waitlist on full** — parent intent status = `Waitlisted` when any child session is full; other child sessions get `Approved`/`Pending` per policy.
- **Alternative rejected:** Option B (reject whole registration) was simpler but produced a worse UX; user has to retry + guess which session is full.
- **Concurrency:** SQL `UPDATE ... WHERE CurrentAudienceAttendees < MaxAudienceAttendees RETURNING ...` inside the same transaction as registration insert. On conflict (0 rows updated), promote that child to Waitlisted.

---

## Risk Assessment (Extended)

| Risk | Impact | Mitigation |
|------|--------|------------|
| Duplicate confirmation email due to outbox replay | Medium | Idempotent handler keyed by `(RegistrationConfirmed, registrationId)` |
| Redis silently not used in runtime | Medium | Startup log effective cache backend; warn if configured but unavailable |
| DataProtection migration not applied in self-hosted install | High | Explicit migration path; auto-migrate at startup via MigrationService |
| File ownership confusion across Blazor/Client/API | Medium | Pre-flight file map per WP in context doc |
| WP-9 scope expansion (draft/publish UX) | Medium | Split: MVP = explicit buttons only; defer beforeunload + advanced transitions |
| iCal timezone bugs | Medium | UTC normalization; explicit test cases for timed events |
| NSwag client drift after API changes | Medium | Formal regeneration checklist |
| Outbox dispatcher wiring complexity | Low | LoggingOutboxMessageDispatcher already registered; routing dispatcher is additive |
| DataProtection migration on existing data | Low | New table, no data migration; one-time session invalidation acceptable |
| iCal library compatibility with .NET 10 | Low | Ical.Net targets netstandard2.0; verified compatible |
| **Over-registration via race conditions** | **High** | **WP-17 atomic SQL capacity check + concurrency stamp; integration test with 10 parallel requests** |
| **Email unsubscribe non-compliance (GDPR + CAN-SPAM)** | **High** | **WP-14 ships before WP-3; RFC 8058 `List-Unsubscribe` + POST; opt-out check at dispatch** |
| **Setup-secret brute-force during onboarding** | **Medium** | **WP-19.1 reuses `setup_secret` rate-limit policy; warning after 3 consecutive failures** |
| **PII access untraceable (insider threat)** | **Medium** | **WP-19.2 audit-logs every UserPii/ActorPii read with actor + purpose + correlation ID** |
| **Silent external dep failure (Redis/SMTP/Keycloak)** | **Medium** | **WP-18 tagged health checks; `/health` returns 503/Degraded with per-dep detail** |
| **SEO non-indexability on launch** | **High** | **WP-16 ships sitemap.xml + robots.txt before public announcement; canonical URLs on all public pages** |
| **Generic error-page UX breaks user trust** | **Medium** | **WP-15 branded 404/403/500 with CTAs; correlation ID on 500 for support** |
| **Capacity-waitlist UX confusion** | **Medium** | **WP-17.4 clear waitlist copy; distinct button text pre-submit** |
| **HAL legacy MapMethodToAction leaks in a corner** | **Medium** | **WP-19.7 deletes the method; architecture test enforces explicit action** |
| **Snapshot-test maintenance burden** | **Low** | **D20 policy: snapshot diffs get visual PR review; store under tests/snapshots/** |
| **Blazor WASM CSP conflict with MudBlazor inline styles** | **Medium** | **WP-19.5 CSP tuned for Blazor WASM + MudBlazor; integration test verifies page renders** |
| **Per-tenant manifest.json cache poisoning** | **Low** | **WP-24.2 5-min cache; vary by tenant + host; no user data in response** |

---

## Dependency Graph

```
Tier 1A (hard blockers):
  WP-1.4 (broken promise) ─── do FIRST
  WP-1.1 (Dockerfile) ─────── parallel
  WP-1.2 (DataProtection) ─── parallel (enables WP-14.2 token encryption)
  WP-1.3 (Redis) ──────────── parallel
  → Gate A + Gate B smoke test immediately after

Tier 1B (hardening):
  WP-2 (Navbar Ph7) ──── parallel with Tier 2 start

Tier 2 (user-facing):
  WP-6 (iCal) ────────── no deps, start early
  WP-7 (Share verify) ── no deps
  WP-4 (My Regs enhance) ── after WP-6
  WP-5 (Post-Reg UX) ──── after WP-6 + WP-7
  WP-14 (Unsubscribe) ── after WP-1.2 (DataProtection); MUST precede first email ship
  WP-3 (Email) ──────── after WP-1.4 + WP-14 (unsubscribe slot required)
  WP-8 (HATEOAS) ────── no deps (also delivers WP-19.7 obsolete removal)
  WP-9 (Draft/Publish) ── no deps
  → Gate C + Gate D after Tier 2

Tier 2B (launch-blocking polish):
  WP-15 (Error pages) ── no deps
  WP-16 (Sitemap/Robots) ── no deps
  → Gate E partial + Gate F partial

Tier 3 (must-have polish):
  WP-17 (Capacity) ──── no deps (touches registration handler only); MUST precede Gate D verification
  WP-18 (Health) ────── no deps; pairs with WP-1.3
  WP-19 (Security audit) ── no deps (partial overlap with WP-8 for WP-19.7)
  WP-20 (SEO+OG) ────── no deps; builds on WP-16
  → Gate F + Gate G complete

Tier 4 (test coverage):
  WP-21 (E2E) ───────── after WP-3, WP-6, WP-8, WP-17, WP-19 (tests the real behaviors)
  WP-22 (Snapshots) ──── after WP-3, WP-6, WP-8, WP-16 (snapshots the real contracts)
  WP-11 (Targeted tests) ── rolling throughout; final pass here

Tier 5 (polish):
  WP-23 (A11y) ──────── after WP-15, WP-20 (error pages + JSON-LD in scope)
  WP-24 (PWA manifest) ── no deps
  WP-25 (Placeholders) ── rolling; final pass at end

Tier 6 (pre-existing deferred):
  WP-10 (User onboarding) ── no deps
  WP-12 (Prod Docker + ExtAPIKey disable) ── no deps
```

### Recommended Sprint Order (Extended, 18-day target)

**Sprint 1 (Days 1-2):** WP-1 (all sub-tasks) → deploy smoke test (Gates A+B)

**Sprint 2 (Days 3-4):** WP-14 (Unsubscribe) + WP-15 (Error pages) + WP-16 (Sitemap/Robots)
- Land compliance + SEO foundation BEFORE any user-facing email ships
- Gate E partial + Gate F partial

**Sprint 3 (Days 5-7):** WP-17 (Capacity) + WP-18 (Health checks) + WP-19 (Security audit)
- Close correctness + observability + audit-trail gaps
- Gate G complete

**Sprint 4 (Days 8-10):** WP-3 (Email) + WP-6 (iCal) + WP-7 (Share verify) + WP-4 (MyReg) + WP-5 (Post-Reg UX)
- User-facing completion loop
- Gate C + Gate D complete

**Sprint 5 (Days 11-12):** WP-20 (SEO/OG polish) + WP-24 (PWA manifest) + WP-8 (HATEOAS incl. legacy removal)
- Gate F complete; HAL coherence landed

**Sprint 6 (Days 13-14):** WP-2 (Navbar Ph7) + WP-9 (Draft/Publish) + WP-12 (Prod docker + disable External API Key)

**Sprint 7 (Days 15-16):** WP-21 (E2E tests) + WP-22 (Snapshots) + WP-11 (test gap sweep)

**Sprint 8 (Days 17-18):** WP-23 (A11y) + WP-25 (Placeholders) + WP-10 (User onboarding decision) + final gate sign-off

**If schedule compresses:**
- Tier 4 (WP-21/22) can slip to Week 4 if Tier 1-3 held quality bar (snapshot stability still required).
- Tier 5 (WP-23/24) can slip to post-MVP if gates A-G are otherwise green.
- Never slip Tier 2B (WP-14/15/16) or Tier 3 (WP-17/18/19) — these are gate-critical.

---

## Success Metrics

1. **All seven gates pass** — Deployability, Session Integrity, Registration Truthfulness, Public Event Loop, Legal & Compliance, SEO & Discoverability, Security Audit Trail
2. **Zero broken promises** — Every UI text matches actual behavior
3. **Complete user loop** — Browse → Register → Confirm → Calendar → Share → Return → Unsubscribe
4. **Self-hosted deploy works** — `docker compose up` with or without Redis
5. **No random logouts** — DataProtection keys persist across restarts
6. **No over-registration** — Capacity enforced atomically; waitlist works
7. **Compliance-ready** — Unsubscribe works; preferences persist; audit trail captures admin/PII events
8. **Search-indexable** — Sitemap served; JSON-LD valid; canonical URLs present
9. **Operators can observe** — Cache backend logged, outbox metrics emitted, dead-letters visible, external-dep health checks tagged
10. **Test baseline holds** — Handler coverage ≥70%; 4 critical E2E flows green; HATEOAS snapshots stable
11. **Branded UX** — Error pages, legal pages, footer, PWA manifest all carry tenant brand
12. **Security auditable** — PII reads logged; admin actions logged; authz denials logged; setup-secret rate-limited

---

## Related Documents

- `dev/active/mvp-report.md` — Source readiness assessment (2026-03-28)
- `dev/active/mvp-launch/mvp-launch-context.md` — Session progress, decisions, file maps
- `dev/active/mvp-launch/mvp-launch-tasks.md` — Task-level checklist
- `dev/active/hateoas-client-alignment/` — HATEOAS fix track (folds into WP-8)
- `dev/active/external-api-access/` — API key track (disable for MVP via WP-12.2)
- `dev/active/navbar-customization/` — Navbar track (WP-2)
- `dev/active/blazor-localization/` — Parked per D11
- `dev/active/organizer-email-consent/` — Reconcile with WP-14 before starting
- `docs/ARCHITECTURE.md` — System architecture
- `docs/SECURITY.md` — Trust boundaries, auth flow
- `docs/MULTI_TENANCY.md` — Tenant resolution, isolation
- `docs/OUTBOX_PATTERN.md` — Outbox implementation reference
- `docs/BLAZOR.md` — Blazor frontend patterns
- `docs/API.md` — API patterns and contracts
- `docs/QUICK_REFERENCE.md` — Non-inferable project invariants
- `docs/GOVERNANCE.md` — Decision frameworks
