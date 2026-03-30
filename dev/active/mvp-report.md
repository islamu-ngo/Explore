# ISLAMU Event Platform — MVP Readiness Report

> ABOUTME: MVP readiness assessment for production with real organizers and event seekers.
> ABOUTME: Generated 2026-03-28. Branch: develop.

**Date:** 2026-03-28
**Branch:** `develop`
**Assessment Type:** Production readiness for self-hosting with real organizers and event seekers

---

## Executive Summary

The platform has strong architectural bones — Clean Architecture, CQRS, BFF, multi-tenancy, HAL/HATEOAS, rate limiting, observability, Docker deployment — but it is **not yet ready for production with real users**. It is approximately **65–70% MVP-complete**. The core event discovery and creation loop works. However, several indispensable user-facing features are absent (email confirmations, post-registration flow, a self-service user sign-up path) and at least two critical infrastructure bugs exist (Blazor Dockerfile on wrong .NET version, DataProtection keys not persisted for multi-instance deployments). Three active feature tracks are in-flight with open compliance tasks.

**Bottom line:** A focused 3–4 week sprint can close the gap. Do not ship to real users without closing the blockers listed in Section 3.

---

## MVP Readiness Score by Area

| Area | Score | Status |
|---|---|---|
| Core event CRUD | 9/10 | Production-ready |
| Event discovery & filtering | 8/10 | Production-ready |
| Event registration flow | 5/10 | Works but broken promises (no confirmation email) |
| Organizer management | 7/10 | Solid, minor HATEOAS gap |
| Authentication & authorization | 8/10 | Production-ready |
| Email notifications | 2/10 | Infrastructure only; zero wiring |
| User self-service sign-up | 4/10 | Relies entirely on Keycloak; no in-app onboarding |
| Payments & ticketing | 0/10 | Not started |
| Attendance / check-in | 0/10 | Not started |
| Admin & settings | 8/10 | Solid |
| Multi-tenancy | 8/10 | Production-ready |
| Docker deployment | 6/10 | Files exist; .NET version mismatch is a blocker |
| Infrastructure resilience | 7/10 | DataProtection persistence gap |
| Test coverage | 6/10 | Gaps in registration + public visibility paths |
| Active in-flight work | — | 3 open tracks, compliance tasks pending |

---

## Section 1 — What IS Production-Ready (Strengths)

### 1.1 Core Event Lifecycle

- Full CRUD with multi-session support, agenda items, and speakers
- Status lifecycle enforced: `Draft → Published → Cancelled / Completed → Archived`
- Visibility enforcement: Draft events are hidden from the public unless the caller is the owner; Archived events return 404 — correct and safe
- Complex filtering: date range, category, tag (tri-state AND/OR), format, audience demographics, language, location, `madhab`, skill level, gender mode — unique differentiator vs. generic event platforms

### 1.2 Event Discovery (Public Experience)

- Anonymous users can browse and filter events with no login required
- `EventList.razor` + `EventDetail.razor` both work unauthenticated
- OG meta tags on event detail pages (social sharing ready)
- Landing page for non-users is well-designed with clear CTAs

### 1.3 Authentication (OIDC / BFF)

- Keycloak + BFF pattern correctly implemented
- Token forwarding chain: `AccessTokenForwarding → TenantHeaderForwarding → SetupSecretForwarding`
- Auth cookies are HttpOnly, Secure, SameSite — correct
- `SetupSecret` bootstrap sequence with 60-minute TTL is a smart self-hosting UX decision

### 1.4 Multi-Tenancy

- Runtime mode switching (`SingleTenant` / `MultiTenant`) via DB setting — no code change required
- Tenant resolution order: `X-Tenant-Slug` header → custom domain → subdomain → fail-closed 404
- EF Core named query filters (`SoftDelete`, `Tenant`) enforce isolation without leaking cross-tenant data
- `BlockInSingleTenant` and `RequireMultiTenant` action filters correctly restrict endpoint visibility per mode

### 1.5 API Layer

- 58 controllers, all HAL/HATEOAS wrapped with pagination
- 4-tier rate limiting (global/IP, authenticated/user, write, setup-secret) — production-grade
- 3-tier caching: Output Cache → HybridCache → ETags (SHA256 weak, 304 support)
- RFC 7807 ProblemDetails with `traceId`, `correlationId`, `timestamp` extensions
- Idempotency key support on write operations

### 1.6 Infrastructure

- MailKit-based SMTP with Polly resilience pipelines (3 retries, exponential backoff, correct transient detection)
- S3/MinIO object storage with pre-signed URLs
- Outbox pattern infrastructure (transactional, retry, dead-letter) — not yet wired for registration emails
- OpenTelemetry traces + metrics (`Explore.Business` meter)
- Structured logging via Serilog
- Health endpoints: liveness, readiness, shutdown-aware (returns 503 on SIGTERM — correct for zero-downtime)

### 1.7 Organization & Admin

- Organization CRUD, membership, approval workflow
- Group management
- Instance onboarding with secret-gated endpoints
- Hierarchical settings: `System → Tenant → Org/Group` with governance locks
- Soft delete with auditing on all core entities (`CreatedAt/By`, `UpdatedAt/By`, `DeletedAt/By`)

---

## Section 2 — Indispensable Features Missing

These are the features without which the platform **cannot be considered a usable MVP** for real organizers and event seekers.

### 2.1 [CRITICAL] Registration Confirmation Email

**What exists:** SMTP service, Polly resilience, `IEmailService` interface, outbox infrastructure.
**What is missing:** `CreateEventRegistrationCommandHandler` does **not** call `IEmailService` or emit any outbox message. The UI (`EventRegistration.razor`) explicitly says *"You will receive a confirmation email shortly"* — this is a **broken promise**.

**Impact:** Every person who registers for an event gets no email confirmation. Users will not know their registration succeeded, cannot add to calendar, cannot find the event later. This is a first-impression trust failure.

**Effort to fix:** Medium (2–3 days). The infrastructure exists. Wire the handler to emit an `OutboxMessage` of type `RegistrationConfirmed`, create a simple HTML email template, and connect the processor to the SMTP service.

---

### 2.2 [CRITICAL] User Self-Service Onboarding Flow

**What exists:** Keycloak handles user creation. Account settings pages work for existing authenticated users.
**What is missing:** No in-app user onboarding flow. After a user creates an account through Keycloak and returns to the app, they land without any guidance — no profile completion prompt, no welcome screen, no first-step nudge.

**Impact:** First-time users are dropped into the event list with no context. Organizers who just self-hosted don't know how to create their first event. Churn risk is high in the first session.

**Effort to fix:** Medium (3–4 days). A simple post-login check (e.g., `IsProfileComplete` flag) + a one-time welcome/setup modal or page is sufficient for MVP.

---

### 2.3 [CRITICAL] My Registrations / Personal Dashboard

**What exists:** `MyEvents.razor` exists for organizers to see their events. `UserService` has `GetRegistrationsAsync`.
**What is missing:** A clear "My Registrations" view where an attendee can see all events they registered for, with status, date, and a way to cancel. The user settings area has security/privacy/connected apps tabs but no "My Events I'm Attending" view.

**Impact:** Attendees cannot manage their event commitments. They cannot cancel a registration, see a reminder, or find an event they signed up for. A personal dashboard is the minimum that keeps attendees coming back.

**Effort to fix:** Low–Medium (2–3 days). A page calling `EventRegistrationController` filtered by userId with a cancel button.

---

### 2.4 [HIGH] No Post-Registration "What Next" Flow

**What exists:** Registration modal shows a success toast, then silently closes.
**What is missing:** After registering, the user sees nothing actionable — no "Add to calendar" link, no "Share this event" button, no link to "Your Registrations".

**Impact:** Successful registrations feel anticlimactic and reduce engagement. The value loop (register → confirmation → look forward to event → attend → come back) is broken at step two.

**Effort to fix:** Low (1 day). Add a confirmation step with: iCal download link, share link, "View your registrations" CTA.

---

### 2.5 [HIGH] Event Sharing / Public URL Visibility

**What exists:** `EventDetail.razor` has OG meta tags.
**What is missing:** No visible "Copy shareable link" button on event cards or detail pages. Organizers cannot easily share an event link with prospective attendees.

**Impact:** Organizers have no mechanism to promote their events. Word-of-mouth and social sharing are primary discovery channels; if sharing is invisible, organic growth stalls.

**Effort to fix:** Low (0.5–1 day). A copy-to-clipboard button + properly formatted canonical URL on the event detail page.

---

### 2.6 [HIGH] Calendar Integration (Add to Calendar)

**What exists:** Event has `StartDate`, `EndDate`, `Location` — all the data needed.
**What is missing:** No `.ics` / iCal file generation. No "Add to Google Calendar", "Add to Apple Calendar", or "Add to Outlook" button anywhere.

**Impact:** Attendees cannot add events to their personal calendars. Without this, attendance rates drop because people forget.

**Effort to fix:** Low (1–2 days). Generate an `.ics` response from the API (`Content-Type: text/calendar`) and expose a button on the event detail and post-registration pages.

---

### 2.7 [MEDIUM] No "Draft Save" UX for Organizers

**What exists:** Event status defaults to `Draft` on creation.
**What is missing:** No explicit "Save as Draft" vs "Publish" differentiation in the UI flow. No unsaved-changes warning when navigating away from a partially filled event form. For long forms (sessions, aspects, custom properties), data loss is a real risk.

**Effort to fix:** Medium (2–3 days). Add explicit "Save Draft" / "Publish" buttons with distinct visual treatment, and a `beforeunload` guard.

---

## Section 3 — Critical Blockers (Infrastructure / Production Safety)

### 3.1 [BUG] Blazor Dockerfile Uses .NET 9, Project Targets `net10.0`

**File:** `Explore.Blazor/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base   # wrong
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build      # wrong
```

The entire solution targets `net10.0`. This Dockerfile will either fail the build outright or produce a misconfigured container running the wrong runtime.

**Fix:** Change both `FROM` lines to `mcr.microsoft.com/dotnet/aspnet:10.0` and `mcr.microsoft.com/dotnet/sdk:10.0`.

---

### 3.2 [BUG] DataProtection Keys Not Persisted for Multi-Instance Deployments

**What exists:** `IDataProtectionProvider` injected in `DynamicAuthSchemeManager`. No `AddDataProtection().PersistKeysTo*()` call found anywhere.
**What happens:** ASP.NET Core's default DataProtection stores keys in-memory per instance. In any container or multi-instance deployment, cookies encrypted by one instance cannot be decrypted by another → random logouts. After container restart, all sessions are invalidated.

**Fix for self-hosted single-instance:**
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ExploreDbContext>()
    .SetApplicationName("explore-blazor");
```

**Fix for multi-instance (Redis already in the stack):**
```csharp
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys")
    .SetApplicationName("explore-blazor");
```

---

### 3.3 [BUG] Broken Promise in Registration UI

`EventRegistration.razor` displays: *"You will receive a confirmation email shortly."*
No email is ever sent. Either implement the email (Section 2.1) or remove the text before launch. Do not ship a broken user-facing promise.

---

### 3.4 [RISK] Redis Missing from docker-compose.yml

`AddRedisDistributedCache` is called in both API and Blazor via Aspire integration. The `docker-compose.yml` does not include a Redis service. Self-hosters deploying with docker-compose will silently fall back to the in-process `DistributedMemoryCache` — not shared, not persistent, not suitable for production.

**Fix:** Add a Redis service to `docker-compose.yml`:
```yaml
redis:
  image: redis:7-alpine
  restart: unless-stopped
  volumes:
    - redis_data:/data
```

---

### 3.5 [RISK] HATEOAS Client Violation in OrganizationDetails

**File:** `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs` line ~103
**Problem:** Uses `RoleHelper.CanManage(currentUserRole)` to gate edit actions. Correct pattern: `organization?.HasHalLink("edit") ?? false`. The API is the authorization authority.

**Impact:** If Cerbos policy changes who can edit, the UI drifts out of sync silently. Creates security/UX inconsistency.

**Active task:** `dev/active/hateoas-client-alignment/` — 13 tasks, Phases 1–5 not started.

---

### 3.6 [RISK] External API Key Rate Limiting Incomplete

**Active track:** `dev/active/external-api-access/` Phase 5 in-progress.
Per-key throttling and audit logs are not yet implemented. If the external API key feature is exposed before Phase 5 is complete, a single key holder can saturate the system. Either complete Phase 5 before launch or disable the external API key endpoints.

---

## Section 4 — Partially Implemented Features

### 4.1 Email Notifications — Infrastructure ✅, Wiring ❌

The SMTP service is solid (MailKit, Polly retries, per-tenant config, TLS support). Missing:
- No email template engine registered (no Liquid, Razor, or Scriban)
- No `RegistrationConfirmed` outbox message handler
- No `EventCancelled` notification to registrants
- No reminder emails

### 4.2 Navbar Customization — Phases 1–6 ✅, Phase 7–10 In-Flight

- Phase 7 (compliance: soft-delete, URL validators, cache invalidation) — 7 open tasks
- Phase 8 (admin panel extraction to `TenantNavigationLinksSection`) — 3 tasks
- Phase 9 (unit + integration tests) — 7 test cases pending
- Phase 10 (responsive sidebar rendering) — 6 tasks

Feature is partially functional but not test-covered and not fully convention-compliant.

### 4.3 Event Customization Sidebar — Track A ✅, Track B Partial

- Track A (settings platform): 34/34 complete
- Track B (EventList UI): 28/34 complete
- Outstanding: visual baseline screenshots (B0), visual regression tests (B8), one deferred EventCard visual check

### 4.4 Pricing Fields — Present in Domain, Unused Everywhere

`Event.Price` and `Event.CurrencyCode` exist with correct `decimal(19,4)` precision and DB check constraints. They are never referenced in any handler, validator, DTO, or UI component. Benign for MVP (payments are out of scope) but should either be wired into forms or removed to avoid confusion.

---

## Section 5 — Features Explicitly Out of MVP Scope

Do not block launch on these. They are correctly deferred.

| Feature | Reason |
|---|---|
| Payment processing (Stripe) | Significant compliance and integration effort |
| PDF ticket generation | Depends on payment integration |
| QR code check-in | Depends on ticket generation |
| SMS / push notifications | Infrastructure not set up |
| Maps / location autocomplete | Third-party API key and cost |
| Federation (ATProto/ActivityPub) | Foundation exists; full gateway not complete |
| Localization (multi-language UI) | English-only is acceptable for v1 |
| Personalized recommendations | Requires data accumulation |
| CFP (Call for Papers) | Conference-specific, not core |
| Mobile native app | Web is sufficient for MVP |
| Advanced analytics dashboards | PostHog/Plausible integration handles basics |

---

## Section 6 — Infrastructure & Deployment Readiness

### 6.1 Docker Deployment

- `docker-compose.yml` orchestrates: `postgres`, `keycloak-db`, `keycloak`, `explore-api`, `explore-blazor`; optional `minio` (storage profile) and `cerbos` (authz profile)
- Health checks on all services
- Multi-stage Dockerfiles for both API and Blazor
- Zero-downtime compatible (SIGTERM grace period 25s)
- **Blocker:** Blazor Dockerfile uses .NET 9 SDK (see Section 3.1)
- **Gap:** No Redis in docker-compose.yml (see Section 3.4)
- **Gap:** No `docker-compose.prod.yml` override — current compose uses `build:` directives; production should use pre-built image references

### 6.2 .NET Aspire AppHost (Development Only)

- Startup sequencing: Redis → MigrationService → API → Blazor — correct
- 20-second startup health delay is a reasonable guard
- Auto-migration on startup acceptable for development; migration-as-job-before-deploy is safer at scale

### 6.3 Observability

- OpenTelemetry configured with `Explore.Business` metrics and traces
- Health endpoints: `/health`, `/alive`, `/metrics` (Prometheus)
- Serilog structured logging
- `PerformanceBehavior` logs MediatR handlers exceeding 500ms
- **Gap:** No starter `prometheus.yml` scrape config. Self-hosters must configure this themselves. Low priority but reduces onboarding friction if provided.

### 6.4 Secrets Management

- `Explore.Secrets` project supports environment variables + Infisical compatibility
- No plaintext secrets in config files — correct
- `SETUP_SECRET` injectable via environment for headless setup
- DataProtection key persistence gap (see Section 3.2)

---

## Section 7 — Security Posture

| Control | Status |
|---|---|
| HTTPS enforced | ✅ |
| Security headers (CSP, X-Frame-Options, etc.) | ✅ |
| JWT Bearer + OIDC (Keycloak) | ✅ |
| BFF pattern (tokens never in browser) | ✅ |
| Rate limiting (4 tiers) | ✅ |
| Idempotency keys on write operations | ✅ |
| CORS explicit allow-list | ✅ |
| Input validation (FluentValidation) | ✅ |
| SQL injection protection (EF Core parameterized) | ✅ |
| Soft delete (no hard-delete data loss) | ✅ |
| Audit trails (CreatedBy, UpdatedBy, DeletedBy) | ✅ |
| Authorization (Cerbos or local, MediatR behavior) | ✅ |
| Multi-tenant data isolation (EF query filters) | ✅ |
| DataProtection key persistence | ❌ Gap |
| HATEOAS client auth compliance (OrganizationDetails) | ⚠️ Violation |

Security posture is strong. Both gaps are addressable before launch.

---

## Section 8 — Test Coverage Assessment

### Strengths

- 45 unit test files (application layer)
- 51 integration test files (API layer)
- Architecture tests (layer dependency enforcement, naming, auth parity)
- `ApiEndpointSmokeTests.cs` validates 56 controller endpoints
- TUnit + bUnit stack is consistent throughout

### Critical Gaps

| Missing Test Area | Risk |
|---|---|
| `EventRegistration` — no unit tests | Approval, waitlist, capacity limits untested |
| `EventRegistration` — no API integration tests | Full registration flow never end-to-end tested |
| Public visibility enforcement | Draft/Archived hiding rules have no test guard |
| Email sending from registration handler | No regression guard when email is wired |
| Navbar customization soft-delete | Phase 7 compliance tasks open |
| HATEOAS Org/Group per-item links | Phase 1 of client-alignment pending |

---

## Section 9 — Prioritized Action Plan for MVP Launch

### Tier 1 — Must-Do Before Any Production Traffic

1. **Fix Blazor Dockerfile .NET version** — `9.0` → `10.0` for both base and SDK images (1 hour)
2. **Configure DataProtection key persistence** — Redis-backed or DB-backed in `Explore.Blazor/Program.cs` (half day)
3. **Add Redis to docker-compose.yml** (1 hour)
4. **Fix or remove "confirmation email" UI text** in `EventRegistration.razor` — remove the broken promise or implement the feature (1 hour to remove; 2–3 days to implement)
5. **Complete Navbar Customization Phase 7** — soft-delete compliance tasks (2 days)

### Tier 2 — Must-Do Before First Real Users

6. **Wire registration confirmation email** — emit `OutboxMessage`, add HTML template, connect processor to SMTP (3 days)
7. **Add "My Registrations" page for attendees** — paginated list with cancel action (2 days)
8. **Post-registration confirmation UX** — "Add to calendar" + "Share" + "View registrations" next step (1 day)
9. **"Copy shareable link" on event detail page and cards** (half day)
10. **Complete HATEOAS client alignment** — remove `RoleHelper.CanManage`, add `HasHalLink` for Org/Group DTOs (1.5 days)
11. **Add iCal / .ics endpoint** — generate from event data, expose on detail + post-registration (1 day)
12. **Add explicit "Save Draft" vs "Publish" UI in event create/edit** (1.5 days)
13. **Complete External API key Phase 5** (per-key rate limiting, throttling) or disable endpoints (3 days)

### Tier 3 — Polish (Before Public Announcement)

14. User welcome/onboarding screen after first login
15. Registration email integration tests (once wired)
16. Public visibility enforcement tests (Draft/Archived)
17. Navbar customization Phases 8–10
18. `docker-compose.prod.yml` override with pre-built image references
19. Wire or remove `Event.Price`/`Event.CurrencyCode` fields from create/edit forms

---

## Summary

The ISLAMU Event platform is a technically sophisticated, architecturally sound foundation built on correct industry patterns. It is deployment-ready in a narrow technical sense — auth works, the core event CRUD works, the data model is solid. It falls short of a usable MVP for real users because:

1. The registration confirmation email is promised in the UI and not delivered — first-impression trust failure
2. Attendees have no personal dashboard to manage their registrations
3. Organizers have no shareable link mechanism for promoting events
4. The Blazor Docker image will not build correctly against the project's target framework
5. Multi-instance DataProtection key management will cause random logouts in production

Close these five gaps and the platform delivers a genuine, defensible MVP experience for both organizers and event seekers. The remaining Tier 2 and Tier 3 items extend the experience from functional to polished.
