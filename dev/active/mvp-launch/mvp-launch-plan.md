ABOUTME: Strategic implementation plan for closing all MVP gaps before production launch.
ABOUTME: Prioritized into tiered gates with phased work, acceptance criteria, and risk assessment.

# MVP Launch — Implementation Plan

> **Created:** 2026-03-28 | **Revised:** 2026-03-29 (architect review) | **Extended:** 2026-04-24 (codebase audit synthesis) | **Updated:** 2026-04-28 (Enterprise Grade Alignment)
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

This plan organizes work into **7 tiers across 25 work packages** with **7 explicit go/no-go release gates**.

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

### Tier 2 — Enterprise Core & Compliance
*Estimated: 4-5 days. These are non-negotiable for enterprise grid apps.*

#### WP-14: Email Unsubscribe & GDPR Compliance (1.5 days)
**Why MVP-critical:** Shipping transactional email without a working unsubscribe mechanism is a GDPR + CAN-SPAM violation. MUST land **before** WP-3 ships any email.
- Implementation: Per-category tokens, one-click compliance (RFC 8058).
- Acceptance: Gate C + Gate E "unsubscribe works" pass.

#### WP-17: Capacity Enforcement & Basic Waitlist (1.5 days)
**Why important:** Data integrity is paramount. Without checks, over-registration is a severe trust risk.
- Implementation: Atomic SQL `UPDATE ... WHERE ...` to prevent race conditions.
- Acceptance: Gate D "capacity-full shows waitlist; over-registration prevented" passes.

#### WP-18: External Dependency Health Checks (1 day)
**Why important:** Operations/SRE readiness. App must report status of Redis, Keycloak, and SMTP.
- Implementation: Tagged health checks (`ready` vs `live`) with graceful degradation support.
- Acceptance: Gate A "all deps observable via /health" passes.

#### WP-19: Security Audit Trail Hardening (1.5 days)
**Why important:** Enterprise security requires strict auditing of PII access and administrative actions.
- Implementation: Audit logging for PII reads, admin actions, and authorization denials. Rate-limit setup-secret.
- Acceptance: Gate G passes fully.

---

### Tier 3 — Core Business Workflows
*Estimated: 6-7 days*

#### WP-3: Registration Confirmation Email (3 days)
- Implementation: Outbox pattern with reference payload, HTML templates, and idempotency.
- Dependency: WP-14 (Unsubscribe slot required).
- Acceptance: Gate C fully passes.

#### WP-6: Calendar Integration — iCal/.ics (1-2 days)
- Implementation: Stable `Ical.Net` endpoint and Blazor UI buttons.
- Acceptance: Gate D "user can download .ics" passes.

#### WP-8: HATEOAS Client Alignment — OrganizationDetails (1.5 days)
- Implementation: Action affordance from HAL links; remove obsolete RoleHelper.
- Acceptance: OrganizationDetails derives action affordance from HAL links.

#### WP-9: Save Draft vs Publish UX (1-1.5 days)
- Implementation: Explicit visibility control via "Save as Draft" vs "Publish".
- Acceptance: Organizers have clear control over event visibility.

---

### Tier 4 — Public Readiness & Discoverability
*Estimated: 3-4 days*

#### WP-4: My Registrations Enhancement (0.5 day)
- Implementation: Add calendar buttons and verify discoverability.

#### WP-5: Post-Registration Confirmation UX (1 day)
- Implementation: Enhanced success state with Calendar, Share, and "My Registrations" links.

#### WP-7: Event Sharing Verification (0.5 day)
- Implementation: Verify Web Share API and EventCard affordance.

#### WP-15: Branded Error Pages (1 day)
- Implementation: Custom 404, 403, 500 pages with tenant branding.
- Acceptance: Gate F "branded error pages" passes.

#### WP-16: SEO Foundation (1 day)
- Implementation: Sitemap.xml, Robots.txt, and canonical URLs.
- Acceptance: Gate F "sitemap + robots.txt" passes.

---

### Tier 5 — Quality Assurance
*Estimated: 4-5 days*

#### WP-21: E2E Critical-Flow Tests (2 days)
- Implementation: Playwright tests for Registration, Tenant Isolation, Authz, and Token Forwarding.
- Acceptance: All four critical flows green in CI.

#### WP-22: Snapshot Tests for HATEOAS Contracts (1 day)
- Implementation: Verify response stability via snapshot testing.
- Acceptance: Baseline snapshots committed and verified.

#### WP-11: Targeted Test Coverage (rolling)
- Implementation: Guard high-value paths and reach ≥70% handler coverage.

---

### Tier 6 — Final Polish
*Estimated: 2-3 days*

#### WP-20: Public Page SEO & OG Polish (1 day)
- Implementation: JSON-LD for Events/Orgs and comprehensive OpenGraph tags.
- Acceptance: Gate F complete.

#### WP-23: Accessibility Polish (1 day)
- Implementation: Breadcrumbs, ARIA landmarks, and focus management.
- Acceptance: Lighthouse a11y score ≥90.

#### WP-24: PWA Manifest Only (0.5 day)
- Implementation: Per-tenant installable manifest.
- Acceptance: Lighthouse PWA audit shows "Manifest: yes".

#### WP-25: Placeholder & TODO Cleanup (0.5 day)
- Implementation: Image replacement, TODO resolution, and final doc pass.

---

### Tier 7 — Pre-Existing Deferred Items
*Estimated: 2 days*

#### WP-10: User Welcome/Onboarding — Gap Analysis (1 day)
#### WP-12: Production Docker & External API Key (1 day)

---

## Recommended Sprint Order (Extended, 18-day target)

**Sprint 1 (Days 1-2):** WP-1 (Infrastructure Fixes)
- Deploy smoke test (Gates A+B).

**Sprint 2 (Days 3-5):** WP-14, WP-17, WP-18, WP-19 (Enterprise Core & Compliance)
- Compliance, Data Integrity, Security Audit, and Health.
- Gate E (Legal), Gate G (Audit), and Gate D (partial) complete.

**Sprint 3 (Days 6-8):** WP-3, WP-6, WP-8, WP-9 (Core Business Workflows)
- Emails, iCal, HATEOAS, and Publishing.
- Gate C (Registration) and Gate D (partial) complete.

**Sprint 4 (Days 9-10):** WP-15, WP-16, WP-4, WP-5, WP-7 (Public Readiness & UX)
- Error pages, SEO base, and user-facing polish.
- Gate F (partial) and Gate D (full) complete.

**Sprint 5 (Days 11-12):** WP-21, WP-22, WP-11 (Quality Assurance)
- Critical E2E, snapshots, and coverage.

**Sprint 6 (Days 13-14):** WP-2, WP-12 (Navbar Ph7, Prod Docker / Disable Ext API)
- Pre-launch hardening.

**Sprint 7 (Days 15-16):** WP-20, WP-23, WP-24 (SEO Polish, A11y, PWA)
- Gate F complete.

**Sprint 8 (Days 17-18):** WP-25, WP-10 (Placeholders, Onboarding decision)
- Final sign-off.

---

## Dependency Graph

```
Tier 1A (hard blockers):
  WP-1.4 (broken promise) ─── do FIRST
  WP-1.1 (Dockerfile) ─────── parallel
  WP-1.2 (DataProtection) ─── parallel (enables WP-14.2 token encryption)
  WP-1.3 (Redis) ──────────── parallel
  → Gate A + Gate B smoke test immediately after

Tier 2 (Enterprise Core & Compliance):
  WP-14 (Unsubscribe) ── after WP-1.2 (DataProtection); MUST precede first email ship
  WP-17 (Capacity) ──── no deps; MUST precede Gate D verification
  WP-18 (Health) ────── no deps; pairs with WP-1.3
  WP-19 (Security audit) ── no deps

Tier 3 (Core Business Workflows):
  WP-3 (Email) ──────── after WP-1.4 + WP-14 (unsubscribe slot required)
  WP-6 (iCal) ────────── no deps, start early
  WP-8 (HATEOAS) ────── no deps (also delivers WP-19.7 obsolete removal)
  WP-9 (Draft/Publish) ── no deps

Tier 4 (Public Readiness & UX):
  WP-15 (Error pages) ── no deps
  WP-16 (Sitemap/Robots) ── no deps
  WP-4 (My Regs enhance) ── after WP-6
  WP-5 (Post-Reg UX) ──── after WP-6 + WP-7
  WP-7 (Share verify) ── no deps

Tier 5 (Quality Assurance):
  WP-21 (E2E) ───────── after WP-3, WP-6, WP-8, WP-17, WP-19 (tests the real behaviors)
  WP-22 (Snapshots) ──── after WP-3, WP-6, WP-8, WP-16 (snapshots the real contracts)
  WP-11 (Targeted tests) ── rolling throughout

Tier 6 (Final Polish):
  WP-20 (SEO+OG) ────── after WP-16
  WP-23 (A11y) ──────── after WP-15, WP-20
  WP-24 (PWA manifest) ── no deps
  WP-25 (Placeholders) ── final pass at end
```

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
