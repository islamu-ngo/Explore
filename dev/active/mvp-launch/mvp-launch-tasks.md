ABOUTME: Executable checklist for MVP launch work packages with acceptance criteria and release gates.
ABOUTME: Rebaselined after the 2026-05-03 source/doc audit; use the plan for strategy and this file for execution.

# MVP Launch — Task Checklist

> **Last Updated:** 2026-05-03 (rebaselined closure checklist)

> **Current execution rule:** do not start from the old WP order. Begin with Phase 0 evidence/status reconciliation from `mvp-launch-plan.md`, then close runtime verification for already implemented foundations before adding new feature code.

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
- [ ] UI shows accurate message (no false promises)
- [ ] Outbox row created atomically with registration
- [ ] Background processor dispatches the message
- [ ] Email sent or gracefully retried
- [ ] No duplicate email on outbox replay (idempotent handler)
- [ ] Email contains working one-click unsubscribe link (RFC 8058 compliant)

### Gate D — Public Event Completion Loop
- [ ] Event detail loads anonymously
- [ ] User can share event
- [ ] User can download .ics calendar file
- [ ] User can view registrations after registering
- [ ] Draft hidden from anonymous; Archived returns 404
- [ ] Capacity-full sessions show clear waitlist state; over-registration prevented
- [ ] Users cannot double-register for the same session

### Gate E — Legal & Compliance (NEW)
- [ ] Privacy Policy, Terms of Service, Community Guidelines reachable from footer + direct routes
- [ ] Cookie consent banner functional (Accept/Decline; preference persisted 180 days)
- [ ] All transactional emails include working one-click unsubscribe (RFC 8058)
- [ ] User-preference-based email opt-outs respected at dispatch
- [ ] Accessibility statement and License pages reachable (or explicitly waived)

### Gate F — SEO & Discoverability (NEW)
- [ ] `/sitemap.xml` returns valid sitemap covering all Published events + static pages
- [ ] `/robots.txt` returns valid directives (allow prod, disallow dev)
- [ ] EventDetail renders valid JSON-LD `schema.org/Event`
- [ ] OrganizationProfile renders valid JSON-LD `schema.org/Organization`
- [ ] OG tags present on Home, Landing, EventDetail, OrganizationProfile, OrganizationDetail
- [ ] Canonical URLs present on all public pages
- [ ] Branded error pages (404, 403, 500) replace generic Error.razor

### Gate G — Security Audit Trail (NEW)
- [ ] Setup-secret endpoint rate-limited (5 attempts / 60s / IP)
- [ ] PII reads write audit log entry (who accessed whose PII, with correlation ID)
- [ ] Admin/instance/tenant setting changes write audit log entries
- [ ] Authorization denial events logged with principal + resource + action
- [ ] BFF responses carry CSP header
- [ ] Audit log queries restricted: instance admin = full; tenant admin = their tenant; users = own actions

---

## Tier 1A — Hard Launch Blockers

### WP-1: Infrastructure Fixes ✅ IMPLEMENTED — Docker smoke blocked locally (1-2 days)

> **2026-04-29 implementation note:** WP-1 code changes are implemented and compiler/unit-style verification is green. Docker/Testcontainers verification is blocked in the current local environment because `docker info` fails with `Docker Desktop is unable to start` / `qemu: process terminated unexpectedly`; rerun Docker-dependent smoke and integration checks once Docker is healthy.

#### WP-1.4: Fix Broken Email Promise ✅ IMPLEMENTED
- [x] Open `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor`
- [x] Line 87: Remove "You will receive a confirmation email shortly."
- [x] Replace with: "Your registration has been confirmed."
- [x] Run Blazor client tests — no regressions
- Acceptance: No false promises in UI

#### WP-1.1: Fix Blazor Dockerfile .NET Version
- [x] Open `Explore.Blazor/Dockerfile`
- [x] Line 6: `FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base` → `10.0`
- [x] Line 12: `FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build` → `10.0`
- [x] Verify `Explore.API/Dockerfile` already uses 10.0 ✅
- [ ] Docker build verification — blocked locally by Docker Desktop/QEMU startup failure
- Acceptance: `docker build` succeeds for both Dockerfiles

#### WP-1.2: Configure DataProtection Key Persistence
- [x] Add `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` to `Explore.Persistence` and `Explore.Blazor` (central package version currently 10.0.7)
- [x] **Do NOT add to Explore.API** (API is bearer-only, never needs BFF key ring)
- [x] Create `Explore.Persistence/DataProtectionKeyContext.cs`:
  - Separate DbContext implementing `IDataProtectionKeyContext`
  - NOT on `ExploreDbContext` (keys are global, not tenant-scoped)
  - `DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();`
  - ABOUTME header, file-scoped namespace
- [x] Register `DataProtectionKeyContext` in DI with `UseNpgsql(DefaultConnection)`
- [x] Create EF migration: `--context DataProtectionKeyContext --output-dir Migrations/DataProtection`
- [x] Register in `Explore.Blazor/Program.cs` ONLY:
  ```csharp
  builder.Services.AddDataProtection()
      .SetApplicationName("islamu-event")
      .PersistKeysToDbContext<DataProtectionKeyContext>();
  ```
- [x] Verify migration auto-applies via `Event.MigrationService` wiring
- [x] Run build — no errors
- [ ] Run all test projects — Docker/Testcontainers suites blocked locally; non-Docker build, architecture tests, and Blazor client tests pass
- Acceptance: Gate B passes — sessions survive restart

#### WP-1.3: Add Redis to docker-compose.yml (with graceful degradation)
- [x] Add Redis service:
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
- [x] Add `redis_data` to volumes section
- [x] Wire Redis connection for Blazor service
- [x] Implement in-memory fallback path when Redis is absent or unavailable
- [x] Implement startup log for effective cache backend (Redis with fallback vs memory)
- [x] Implement degradation warning when Redis is configured but unavailable
- [ ] Runtime Docker verification of Redis/fallback behavior — blocked locally by Docker Desktop/QEMU startup failure
- Acceptance: Gate A passes — works with or without Redis

#### Post-WP-1: Deploy Smoke Test
- [ ] Run `docker compose up` → all services healthy — blocked locally by Docker Desktop/QEMU startup failure
- [ ] Login → navigate → restart → login persists (Gate B) — pending Docker/runtime verification
- [ ] Redis down → app still works (Gate A degradation) — pending Docker/runtime verification

---

## Tier 1B — Pre-Launch Hardening

### WP-2: Sidebar/Dock Layout Handoff or MVP Removal ⏳ NEEDS DECISION
- [ ] Remove stale `dev/active/navbar-customization/` dependency from MVP scope.
- [ ] If shell/sidebar work is still launch-critical, reconcile against `dev/active/sidebar-dock-layout-refactor/` instead.
- [ ] If not launch-critical, explicitly mark WP-2 deferred/removed for MVP.
- [ ] Run affected Blazor client tests if any shell/layout source is touched.
- Acceptance: MVP plan no longer depends on a dead active track.

---

## Tier 2 — Enterprise Core & Compliance 🟡 PARTIAL (4-5 days)

### WP-14: Email Unsubscribe & GDPR Compliance (1.5 days)

> **CRITICAL:** Must land BEFORE WP-3 ships any email. GDPR + CAN-SPAM violation without it.
>
> **2026-04-29 implementation note:** WP-14 foundation is implemented through the preference schema, DataProtection-backed unsubscribe tokens, and anonymous GET/POST unsubscribe endpoint. Header injection and dispatch-time consent remain open because the repository does not yet contain `RegistrationConfirmedEmailBuilder` or `RegistrationConfirmedOutboxHandler`; those should be completed when WP-3 introduces the registration-confirmation email flow.

#### WP-14.0: Reconcile Existing Track
- [x] Read `dev/active/organizer-email-consent/` — merge any existing scope
- [x] Confirm no duplicated effort with existing notification preference work
- Acceptance: No scope overlap

#### WP-14.1: Notification Preferences Schema
- [x] Check if `UserNotificationPreferences` already exists — reuse if present
- [x] Required preference categories: `registration-confirmations` (opt-in), `organizer-announcements` (opt-in)
- [x] Future categories (schema only): `event-reminders`, `event-updates`
- [x] Table columns: `UserId` (FK), `Category` (string), `IsEnabled` (bool), `UpdatedAt`, `UpdatedBy`
- [x] EF migration if new table
- Acceptance: Preference table exists with required categories

#### WP-14.2: Unsubscribe Token Service
- [x] Create `Explore.Infrastructure/Mail/Unsubscribe/UnsubscribeTokenService.cs`
- [x] Use `ITimeLimitedDataProtector` (180-day token lifetime, reuses WP-1.2 DataProtection keys)
- [x] Token payload: `{ userId, category, tenantId, issuedAt }`
- [x] Timing-safe validation; invalid/expired → treat as "already unsubscribed"
- [x] Unit test: token round-trip, expiration, tamper detection
- Acceptance: Tokens encrypt/decrypt correctly; tampered tokens fail safely

#### WP-14.3: Unsubscribe Endpoint (GET + POST)
- [x] Create `Explore.API/Controllers/EmailUnsubscribeController.cs`
- [x] `GET /api/email/unsubscribe?token=...` → `[AllowAnonymous]` + rate-limited (`global` policy)
- [ ] Returns branded confirmation page with CTA to re-subscribe or manage preferences — endpoint foundation returns JSON status; branded page remains UI follow-up
- [x] `POST /api/email/unsubscribe?token=...` → RFC 8058 `List-Unsubscribe=One-Click` compliance
- [x] Updates `UserNotificationPreferences` for (userId, category)
- [ ] Writes audit log entry (`PreferenceChange`, actor=system, delegated=token-subject)
- Acceptance: Both GET and POST unsubscribe work; preference updated

#### WP-14.4: Email Header Injection
- [ ] Update `RegistrationConfirmedEmailBuilder` to accept injected unsubscribe URL — pending WP-3 email builder creation
- [ ] Set SMTP headers: `List-Unsubscribe: <url>, <mailto:...>` + `List-Unsubscribe-Post: List-Unsubscribe=One-Click` — transport supports `EmailMessage.CustomHeaders`; pending WP-3 builder/dispatcher
- [ ] Render same URL in visible email footer — pending WP-3 email template
- Acceptance: Email contains valid List-Unsubscribe headers

#### WP-14.5: Dispatch-Time Consent Check
- [ ] In `RegistrationConfirmedOutboxHandler` (WP-3.5): query preferences before sending — pending WP-3 handler creation
- [ ] Skip + log if user opted-out of "registration-confirmations" — preference repository is available for WP-3
- [ ] Emit `outbox.messages.skipped_opt_out` counter — pending WP-3 handler/metrics wiring
- Acceptance: Opted-out users don't receive emails

#### WP-14.6: Tests
- [x] Unit: token round-trip, expiration, tamper detection
- [ ] Integration: end-to-end unsubscribe via GET + POST — pending Docker/Testcontainers availability or non-container API test harness
- [ ] Integration: handler skips dispatch when user opted-out — pending WP-3 registration email handler
- Acceptance: All tests pass

**Acceptance:** Gate C + Gate E "unsubscribe works" pass.

### WP-17: Capacity Enforcement & Basic Waitlist (1.5 days)

> **2026-05-03 audit note:** Tasks below were originally written as greenfield work, but source now appears to contain capacity/waitlist support in the registration intent repository, waitlist UI, lookup seed, and approval status enum. Reconcile the implementation before writing new code. Convert any already-satisfied items to verification/tests instead of duplicating logic.

#### WP-17.1: Capacity Check in Registration Handler
- [ ] Open `CreateEventRegistrationCommandHandler.cs`
- [ ] For each session: query `CurrentAudienceAttendees` vs `MaxAudienceAttendees`
- [ ] Use atomic SQL `UPDATE ... WHERE CurrentAudienceAttendees < MaxAudienceAttendees RETURNING ...`
- [ ] On conflict (0 rows returned) → that child session = `Waitlisted`
- [ ] Parent intent = `Waitlisted` if any child is waitlisted
- [ ] Unit test: handler creates waitlisted entry when at capacity
- Acceptance: Over-registration prevented; auto-waitlist works

#### WP-17.2: Atomic Attendee Count Update
- [ ] On successful `Approved` child registration: increment `CurrentAudienceAttendees` in same transaction
- [ ] On cancellation (DELETE): decrement count
- [ ] Integration test: count matches actual registration count
- Acceptance: Attendee count stays accurate

#### WP-17.3: Duplicate Registration Prevention
- [ ] Add unique index on `EventRegistration(UserId, EventSessionId)` where `IsDeleted=false`
- [ ] Handler short-circuits if duplicate (return existing intent id — idempotent)
- [ ] Migration required
- Acceptance: No double-registration possible

#### WP-17.4: UI Feedback
- [ ] `EventRegistration.razor`: if any session full → "Join Waitlist" button instead of "Register"
- [ ] Post-registration: if `ApprovalStatus=Waitlisted` → waitlist copy, not confirmation copy
- [ ] `MyRegistrations.razor`: verify waitlist badge renders
- Acceptance: User sees clear waitlist state

#### WP-17.5: Tests
- [ ] Unit: handler rejects over-capacity, creates waitlisted entry
- [ ] Integration: concurrent POSTs do not exceed capacity (10 parallel requests)
- [ ] Integration: duplicate registration returns existing intent
- [ ] Integration: cancellation decrements count
- Acceptance: Tests pass

**Acceptance:** Gate D "capacity + waitlist + no double-register" passes.

### WP-18: External Dependency Health Checks ✅ SOURCE COMPLETE — runtime smoke remains (1 day)

> **2026-05-03 implementation note:** `/health` is now readiness-only and `/alive` is liveness-only in `Explore.ServiceDefaults`. API readiness includes database, distributed cache, OIDC discovery, SMTP, conditional Cerbos PDP readiness, and secret-provider checks. Blazor BFF readiness includes database, distributed cache/fallback state, OIDC discovery, and downstream API readiness. API build passes after conditional Cerbos readiness; targeted no-Keycloak API integration tests pass via TUnit `--treenode-filter` and confirm `/alive` returns 200 without OIDC. Current Blazor build is blocked by unrelated dirty publish-flow/client work.

#### WP-18.1: Redis Health Check
- [x] Add distributed-cache readiness check tagged `ready`
- [x] Report configured cache state and Blazor Redis fallback degradation when fallback is active
- Acceptance: Redis/effective cache health reported in `/health`

#### WP-18.2: Keycloak OIDC Discovery Health Check
- [x] Custom health check: GET configured metadata address or `{authority}/.well-known/openid-configuration` with 5s timeout
- [x] Tagged `ready`
- Acceptance: Keycloak health reported in `/health`

#### WP-18.3: SMTP Health Check
- [x] Reuse `IEmailService.TestConnectionAsync()`
- [x] Tagged `ready`; missing SMTP configuration reports `Degraded`; configured connection failures report `Unhealthy`
- Acceptance: SMTP health reported in `/health`

#### WP-18.4: Cerbos Health Check (Conditional)
- [x] If instance `authorization.provider` is `cerbos`: gRPC health check against the configured Cerbos endpoint
- [x] Local provider mode reports healthy/skipped so local-only/self-hosted deployments are not made unhealthy by unused Cerbos defaults
- [x] Tagged `ready`; configured Cerbos unreachable reports `Unhealthy`, aligned with existing instance Cerbos fail-closed authorization behavior
- Acceptance: Cerbos health reported when enabled

#### WP-18.5: Operator Docs
- [x] Update troubleshooting docs with health-check interpretation table — `docs/OPERATIONS.md`
- [x] Document: `/health` 200 Degraded vs 503 Unhealthy; dependency-specific operator actions documented
- Acceptance: Docs updated

#### WP-18.6: Tests
- [ ] Integration: `/health` returns 200 Healthy with all deps up
- [ ] Integration: `/health` returns 200 Degraded when Redis unreachable (NOT 503)
- [x] Integration: `/alive` returns 200 regardless of missing OIDC authority
- Acceptance: Tests pass

**Acceptance:** Gate A "all deps observable via /health" passes in source/build/test evidence; live Docker/Aspire dependency smoke remains.

### WP-19: Security Audit Trail Hardening (1.5 days)

#### WP-19.1: Rate-Limit Setup-Secret Validation
- [ ] Apply existing `setup_secret` policy to `validate-secret` endpoint and any `X-Setup-Secret` endpoints
- [ ] After 3 consecutive failures: emit warning log with `ip`, `user-agent`, `correlation-id`
- Acceptance: 429 after 5 attempts from same IP

#### WP-19.2: PII Access Audit
- [ ] Wrap `UserPiiRepository` and `ActorPiiRepository` read methods with audit logging
- [ ] For every read: write to `AuditLog` with `EntityType`, `EntityId`, `Action="Read"`, `ActorId`, `Timestamp`, `CorrelationId`, `Purpose`
- [ ] Self-reads: log with `IsSelfAccess=true` to filter noise
- Acceptance: PII reads create audit entries

#### WP-19.3: Admin Action Audit
- [ ] Create `AuditLoggingBehavior<TRequest, TResponse>` (MediatR pipeline behavior)
- [ ] Apply to commands under: InstanceSettings, TenantSettings, Roles, InstanceOnboarding, TenantOnboarding
- [ ] Log: entity type, old vs new (JSON diff), actor, timestamp, correlation ID
- Acceptance: Setting changes create audit entries

#### WP-19.4: Authorization Denial Audit
- [ ] In `FallbackAuthorizationService.IsAllowedAsync`: on `Deny`, write audit entry with principal + resource + action
- [ ] Do NOT log allowed decisions (volume prohibitive; use metrics counter)
- Acceptance: Authz denials logged

#### WP-19.5: BFF CSP Header
- [x] Add BFF security-header middleware through `Explore.Blazor/Extensions/MiddlewareExtensions.cs`
- [x] CSP: `default-src 'self'; img-src 'self' data: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'wasm-unsafe-eval'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'`
- [x] Register immediately after forwarded headers/graceful shutdown and before static assets, routing, controllers, and BFF endpoints
- [x] Add companion headers: `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`
- [x] Add integration coverage in `BffNoKeycloakResilienceTests.StaticPages_CarryContentSecurityPolicyHeader`
- [ ] Runtime browser smoke: verify Blazor/MudBlazor assets still load with the CSP in a healthy app host
- Acceptance: BFF HTML responses carry CSP header

#### WP-19.6: Audit Log Access Control
- [ ] New permission: `audit_log:read` (tenant-scoped) + `audit_log:read_all` (instance-scoped)
- [ ] Instance admin: IgnoreQueryFilters for Tenant filter (preserve SoftDelete)
- [ ] Tenant admin: default filter (their tenant only)
- [ ] Regular users: `/api/users/me/audit-log` scoped by actor claim
- Acceptance: Access control enforced

#### WP-19.7: Remove Obsolete HAL Legacy Fallback
- [x] Ensure link policies call `RequirePermission(...)` with explicit `AuthorizationActions` metadata
- [x] Delete `[Obsolete] MapMethodToAction()` method and callsites from `HateoasAuthorizationEvaluator`
- [x] Fail closed when a permission-bound link has `PermissionResourceKind` but no explicit `PermissionAction`
- [x] Unit regression added: `PermissionResourceWithoutAction_Denied_NoProviderCall`
- [x] Architecture test added: `AllLinkPoliciesHaveExplicitPermissionActions`
- [x] Verification: `dotnet run --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release -- --treenode-filter "/*/*/*AuthorizationParityTests*/*" --minimum-expected-tests 1 --no-progress` passed 12 tests
- [ ] Verification blocked: targeted `Event.Application.UnitTests` run currently cannot compile because unrelated custom-property projection tests are stale against in-flight constructor/signature changes
- Acceptance: Legacy fallback deleted; architecture test passes; unit source diagnostics clean; targeted unit runtime pending unrelated test-project compile fix

#### WP-19.8: Tests
- [ ] Integration: setup-secret returns 429 after 5 attempts
- [ ] Integration: PII read writes audit entry
- [ ] Integration: setting change writes audit entry with old vs new
- [ ] Integration: authz denial writes audit entry
- [x] Integration: BFF response carries CSP header
- [ ] Integration: tenant admin cannot query other tenant's audit logs
- [x] Architecture: all link policies have explicit action set
- Acceptance: Tests pass

**Acceptance:** Gate G passes fully.

---

## Tier 3 — Core Business Workflows 🟡 PARTIAL (6-7 days)

### WP-3: Registration Confirmation Email (3 days)

#### WP-3.1: Email Template Builder
- [ ] Create `Explore.Infrastructure/Mail/Templates/RegistrationConfirmedEmailBuilder.cs`
- [ ] ABOUTME header, file-scoped namespace
- [ ] String interpolation (no template engine for MVP)
- [ ] Inputs: event name, date/time, location, organizer, event URL, calendar URL, **unsubscribe URL**
- [ ] Output: HTML email body
- [ ] Unsubscribe slot: template renders URL in body footer
- [ ] Unit test for builder
- Acceptance: Clean HTML rendered from event data

#### WP-3.2: IOutboxMessageHandler Interface
- [ ] Create `Explore.Application/Contracts/Outbox/IOutboxMessageHandler.cs`
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
- [ ] **Consent check (WP-14 dependency):** query `UserNotificationPreferencesRepository.GetAsync(userId, "registration-confirmations")` — skip + log if opted-out
- [ ] Deserialize reference payload → fetch fresh data from repos → build email → send via `IEmailService`
- [ ] Inject unsubscribe URL from WP-14 token service
- [ ] Set SMTP headers: `List-Unsubscribe`, `List-Unsubscribe-Post: List-Unsubscribe=One-Click`
- [ ] Structured logging: `RegistrationId`, `EventId`, `UserId`, `TenantId`, `OutboxMessageId`
- [ ] Unit test with mocked repos and IEmailService
- Acceptance: Email sent; no duplicates on replay; unsubscribe respected

#### WP-3.6: Observability
- [ ] Counter: `outbox.messages.processed` with `{event_type, outcome, tenant_id}`
- [ ] Counter: `outbox.messages.failed` with `{event_type, tenant_id}`
- [ ] Counter: `outbox.messages.skipped_opt_out` with `{event_type, category, tenant_id}`
- [ ] Dead-letter visibility via `GetFailedEntries`
- Acceptance: Operators can monitor outbox health

#### WP-3.7: Restore Email Promise + Integration Test
- [ ] Restore line 87: "You will receive a confirmation email shortly."
- [ ] Integration test: register → outbox message created → handler processes → email sent → unsubscribe link valid
- [ ] Run all tests
- Acceptance: Gate C fully passes

### WP-6: Calendar Integration — iCal/.ics (1-2 days)

#### WP-6.1: Add Ical.Net NuGet Package
- [x] Add `Ical.Net` to `Explore.API` via central package management (`Ical.Net` 5.2.1)
- Acceptance: Package restores and builds

#### WP-6.2: API — Calendar Endpoint
- [x] Add `GET /api/event/{id}/calendar` to `EventController`
- [x] `[AllowAnonymous]`
- [x] `Content-Type: text/calendar; charset=utf-8`
- [x] `Content-Disposition: attachment; filename="{sanitized-slug}.ics"`
- [x] VEVENT: SUMMARY, DTSTART/DTEND (UTC), LOCATION, DESCRIPTION, URL (canonical), UID (event GUID)
- [x] Non-negotiable checks:
  - [x] UID = event GUID (stable, not random)
  - [x] All timestamps UTC normalized
  - [x] Filename sanitized (strip special chars)
  - [x] 404 for Draft, Archived, non-existent
  - [x] Canonical URL matches `GetCanonicalUrl()` pattern
- [x] Route name: `RouteNames.GetEventCalendar`
- Acceptance: .ics opens correctly in Calendar apps — pending runtime/calendar-app smoke

#### WP-6.3: Blazor — Calendar Buttons
- [x] `EventDetail.razor`: "Add to Calendar" button → `/api/event/{id}/calendar`
- [x] `MyRegistrations.razor`: per-event calendar icon button
- [x] Post-registration success (WP-5): calendar button
- [ ] Optional: Google Calendar URL link
- Acceptance: Calendar buttons work from 3 touchpoints

#### WP-6.4: NSwag Client Regeneration
- [ ] Build `Explore.API` so build-time OpenAPI generation refreshes `Explore.API/swagger.json`
- [ ] Rebuild Blazor client so NSwag regenerates `EventApiClient.g.cs`
- [x] Fix consuming code — direct download anchors used; generated client not required by UI path
- [x] Run client tests
- Acceptance: Client builds cleanly; OpenAPI regeneration pending build-time API generation and Blazor client rebuild

#### WP-6.5: Tests
- [ ] Integration: valid .ics with correct Content-Type — pending runtime/API smoke
- [x] 404 for Draft/Archived events — covered by `GetEventCalendarExportRequestHandlerTests`
- [x] UTC timestamps correct — covered by `IcalNetEventCalendarFileBuilderTests`
- Acceptance: Build, application unit, Blazor client, and architecture tests pass; runtime/API integration pending

### WP-8: HATEOAS Client Alignment (1.5 days)

- [ ] Read `dev/active/hateoas-client-alignment/hateoas-client-alignment-tasks.md`
- [ ] Phase 2.1: Extract `HasHalLinkInAdditionalProperties` private helper
- [ ] Phase 2.2: Add `HasHalLink` for `OrganizationDto` and `OrganizationListDto`
- [ ] Phase 3.1: Remove `CheckEditPermissions()`, delete `currentUserRole`, use `HasHalLink("edit")`
- [ ] Phase 3.2: Verify `_links` preservation in service
- [ ] **Grep for `RoleHelper.CanManage` in adjacent pages** — fix any cousins
- [ ] **Legacy HAL removal (WP-19.7 overlap):** ensure all link policies call `RequirePermission("resource", "action")` explicitly
- [ ] Phase 4.3: bUnit tests for OrganizationDetails HATEOAS consumption
- [ ] Run all tests — no regressions
- Acceptance: No RoleHelper for action gating; HAL links are source of truth

### WP-9: Save Draft vs Publish UX (1-1.5 days)

> **Scope warning:** MVP minimum only. Defer beforeunload and advanced transitions.

#### WP-9.1: MVP — Explicit Buttons
- [ ] Event create form: "Save as Draft" (secondary) + "Publish" (primary) buttons
- [ ] Event edit form for Draft: "Save" + "Publish"; for Published: "Save" only
- [ ] Verify Draft → Published transition works in API handler
- [ ] bUnit tests for button rendering based on status
- Acceptance: Organizers choose visibility via explicit button

---

## Tier 4 — Public Readiness & Discoverability 🟡 PARTIAL (3-4 days)

### WP-15: Branded Error Pages ✅ IMPLEMENTED — runtime smoke pending (1 day)

> **2026-04-29 implementation note:** Branded 404/403/500 pages, Blazouter route registrations, server status-code re-execution middleware, and bUnit/route tests are implemented. Browser/runtime integration checks remain pending because the local Docker/Testcontainers/runtime environment is unavailable.

#### WP-15.1: 404 Not Found
- [x] Create `Explore.Blazor.Client/Pages/Errors/NotFound.razor` with `@page "/errors/404"`
- [x] Branded layout: logo, "Page Not Found", search bar, CTAs ("Return Home", "Browse Events")
- [x] `<PageTitle>Not Found — {TenantName}</PageTitle>` and `<meta name="robots" content="noindex">`
- Acceptance: Direct URL to `/nonexistent` shows branded 404 — code-supported via status-code middleware; browser smoke pending

#### WP-15.2: 403 Unauthorized
- [x] Create `Explore.Blazor.Client/Pages/Errors/Unauthorized.razor` with `@page "/errors/403"`
- [x] CTAs: "Request Access" (if applicable), "Return Home"
- Acceptance: Auth failure shows branded 403

#### WP-15.3: 500 Server Error
- [x] Create `Explore.Blazor.Client/Pages/Errors/ServerError.razor` with `@page "/errors/500"`
- [x] Enhance existing `Explore.Blazor/Components/Pages/Error.razor` for branded content
- [x] Display correlation ID for support; hide stack traces in production
- [x] CTAs: "Return Home", "Contact Support" (prefilled with correlation ID)
- Acceptance: Server error shows branded 500 with correlation ID

#### WP-15.4: Status Code Pages Middleware
- [x] In `Explore.Blazor/Program.cs`: `app.UseStatusCodePagesWithReExecute("/errors/{0}")`
- [ ] Verify works with Blazor Server interactive routes — pending runtime/browser smoke
- [x] Add catch-all route in `Routes.razor` if needed
- Acceptance: All HTTP errors redirect to branded pages — runtime smoke pending

#### WP-15.5: Tests
- [x] bUnit: each error page renders with correct copy + CTA
- [ ] Integration: `/nonexistent` returns branded 404 page — pending runtime/browser smoke
- [ ] Integration: unauthenticated `[Authorize]` endpoint returns branded 403 — pending runtime/browser smoke
- Acceptance: Tests pass

**Acceptance:** Gate F "branded error pages" passes.

### WP-16: SEO Foundation (1 day)

#### WP-16.1: Sitemap Controller
- [x] Create `Explore.API/Controllers/SitemapController.cs`
- [x] `GET /sitemap.xml` → `[AllowAnonymous]`, `Content-Type: application/xml`
- [x] Output: static pages + all Published events (respect tenant visibility + soft-delete filter)
- [x] Per-URL: `<loc>`, `<lastmod>`, `<changefreq>`, `<priority>`
- [x] Tenant-aware: canonical host from current tenant's domain/subdomain
- [x] Output-cache 30 minutes
- Acceptance: `/sitemap.xml` returns valid XML — implementation complete; runtime/API smoke pending

#### WP-16.2: Robots.txt
- [x] Create `Explore.Blazor/wwwroot/robots.txt` (static) OR dynamic controller
- [x] Prod: `User-agent: * / Allow: / / Sitemap: https://{host}/sitemap.xml`
- [x] Dev: `Disallow: /`
- Acceptance: `/robots.txt` returns correct content per environment — implementation complete; runtime smoke pending

#### WP-16.3: Canonical URLs on Public Pages
- [x] Add `<link rel="canonical">` to: Home, LandingForNonUsers, LandingForUsers, OrganizationProfile, OrganizationDetails, EventList
  - Note: `Home.razor` emits the authenticated landing canonical, so the `LandingForUsers` render path is covered without duplicating canonical tags.
- [x] Centralize canonical URL helper if not already shared
- Acceptance: Canonical URLs on all public pages — source tests pass

#### WP-16.4: Tests
- [ ] Integration: `/sitemap.xml` returns valid XML with published events — pending runtime/API smoke
- [ ] Integration: Draft/Archived events absent from sitemap — pending runtime/API smoke
- [ ] Integration: tenant A sitemap doesn't leak tenant B events — pending runtime/API smoke
- [ ] Integration: `/robots.txt` returns expected content — pending runtime/API smoke
- [x] Unit/source tests: sitemap query handler and canonical metadata coverage
- Acceptance: Tests pass for build, unit, architecture, and Blazor client suites; runtime/API integration pending

**Acceptance:** Gate F "sitemap + robots.txt" passes.

### WP-4: My Registrations — Enhancement (0.5 day)
- [x] Verify NavMenu/user menu link — covered by `NavMenu_AuthenticatedUser_ShowsMyRegistrationsLink`
- [x] Verify discoverability from nav AND post-registration flow — added `/my/registrations` CTAs after success/already-registered states
- [x] Add "Add to Calendar" icon per registration card (after WP-6) — direct `/api/event/{id}/calendar` download action in `MyRegistrations.razor`
- [x] Verify empty state UX — upgraded to accessible AppCard empty state with Browse Events CTA
- Acceptance: Gate D "user can view registration later" passes in build/unit coverage; runtime calendar-app smoke pending

### WP-5: Post-Registration Confirmation UX (1 day)
- [x] Add "Add to Calendar" button (WP-6 dependency) — direct `.ics` download action in post-registration states
- [x] Add "Share this Event" button (reuse `ShareEventAsync` pattern) — Web Share API with clipboard fallback
- [x] Add "View My Registrations" link → `/my/registrations`
- [x] Keep it lightweight — 3 actions only, no workflow engine
- [x] bUnit test for enhanced success state — covered by `InlineRegistrationSuccess_RendersThreeActionChoices`
- Acceptance: Post-registration has calendar, share, and registration follow-up actions; runtime calendar-app smoke pending

### WP-7: Event Sharing — Verification (0.5 day)
- [x] Verify share button visible on EventDetail — covered by source test for `Share Event` + `ShareEventAsync`
- [x] Check EventCard (list view) — added accessible share icon to event cards
- [x] Verify canonical URL tenant-aware in multi-tenant — uses `CanonicalUrlHelper.Build(...)` from current navigation base URI
- [x] If URL generation duplicated → centralize into shared helper — card/list share flow reuses `CanonicalUrlHelper`
- Acceptance: Gate D "user can share" passes in build/unit coverage; runtime mobile/desktop smoke pending

---

## Tier 5 — Quality Assurance 🟡 PARTIAL (4-5 days)

### WP-21: E2E Critical-Flow Tests (2 days)

#### WP-21.1: Registration End-to-End
- [x] Create `Explore.Blazor.Client.E2ETests/Flows/CriticalFlows/RegistrationFlowTests.cs`
- [x] Playwright: login → browse → open event → register → confirmation → My Registrations — scaffolded as infrastructure-gated critical-flow test
- [x] Uses `AppHostFixture` + `PostgreSqlContainerFixture` — local E2E PostgreSQL fixture added for scenario-owned data reset/seed
- [ ] Validates Gates C + D end-to-end — pending Docker/Aspire/Keycloak runtime execution
- Acceptance: Registration E2E compiles; runtime green pending full infrastructure smoke

#### WP-21.2: Multi-Tenancy Isolation
- [x] Two tenant contexts; tenant A creates event → tenant B cannot see it — scaffolded with local `TenantIsolationScenarioSeed`
- [ ] Validates query filters + middleware isolation — pending Docker/Aspire tenant host/header runtime execution
- Acceptance: Tenant isolation E2E compiles; runtime green pending full infrastructure smoke

#### WP-21.3: Authorization Enforcement
- [x] User without edit permission → no Edit button → direct API mutation returns 403 — scaffolded in `AuthorizationEnforcementFlowTests` with protected-route redirects, 403 shell, and direct mutation denial checks
- [ ] Runtime low-privilege browser state verifies no Edit affordance + authenticated mutation returns 403 — pending Docker/Aspire/Keycloak auth-state wiring
- Acceptance: Authz enforcement E2E compiles; runtime green pending full infrastructure smoke

#### WP-21.4: BFF Token-Forwarding Chain
- [x] Login → BFF → YARP → API JWT + tenant header → HAL links → Blazor renders — scaffolded in `BffTokenForwardingChainFlowTests` with `/auth/status`, tenant header, proxied `/api/event`, and HAL-driven UI assertions
- [ ] Runtime cookie-authenticated browser state verifies JWT forwarding end-to-end — pending Docker/Aspire/Keycloak auth-state wiring
- Acceptance: Token chain E2E compiles; runtime green pending full infrastructure smoke

**Acceptance:** All 4 flows green in CI (3 consecutive runs, no flakiness).

### WP-22: Snapshot Tests for HATEOAS Contracts (1 day)

#### WP-22.1: Add Snapshot Library
- [x] Install `Verify.TUnit` (or `Verify.Xunit`) in `Event.API.IntegrationTests` — pinned `Verify.TUnit` 31.9.4 for TUnit 1.33 compatibility
- [x] Configure snapshot directory: `tests/snapshots/`
- Acceptance: Library installed and configured

#### WP-22.2: Snapshot EventDto Responses
- [ ] Anonymous GET event detail → snapshot
- [ ] Authenticated GET event detail → snapshot (more links)
- [ ] Organizer GET event detail → snapshot (edit/delete links)
- [x] List response (first 5) → snapshot — anonymous and authenticated Contract-profile baselines committed
- Acceptance: Initial event-list baselines committed; event detail role matrix pending

#### WP-22.3: Snapshot OrganizationDto, UserDto, EventRegistrationDto
- [ ] Public GET + authenticated GET for each
- Acceptance: Baseline DTO snapshots committed

#### WP-22.4: Snapshot ProblemDetails
- [x] 400 and 404 → snapshots (RFC 7807 shape) — Contract-profile baselines committed
- [ ] 401, 403, 500 ProblemDetails snapshots
- Acceptance: Initial error contract snapshots committed; auth/server-error variants pending

#### WP-22.5: PR Policy
- [x] Document snapshot-review policy in testing docs
- Acceptance: Policy documented

**Acceptance:** Initial Contract-profile baselines are committed and focused snapshot tests pass; broader DTO/problem-details matrix remains pending.

### WP-11: Targeted Test Coverage (rolling)
- [ ] Registration flow unit tests (approval policy, waitlist, capacity from WP-17)
- [x] Visibility rules (Draft hidden, Archived 404) — Contract API tests cover public list exclusion and anonymous detail 404s; PostgreSQL query-spec translation remains covered by persistence tests
- [x] HATEOAS action gating (OrganizationDetails) — bUnit verifies Create Event, Members, and Edit affordances follow `_links.edit`
- [x] Calendar endpoint (valid .ics, 404 non-public, UTC normalization) — controller contract covers text/calendar attachment + 404; handler/builder tests cover non-public filtering and UTC VEVENT fields
- [x] Session persistence regression (DataProtection key ring) — persistence regression verifies EF-backed DataProtection keys can unprotect session payloads across fresh providers sharing the persisted key store
- [x] Unsubscribe flow end-to-end (WP-14) — Contract API tests cover valid token confirmation, valid POST persistence, malformed-token generic response/no state change, and anonymous + Global rate-limit endpoint metadata
- [x] Rate limit enforcement on setup-secret (WP-19.1) — metadata test verifies `ValidateSecret` is anonymous and wired to `SetupSecretPolicy`; stress 429 scaffold added and skipped pending enabled-host limiter enforcement fix
- [ ] Handler coverage target: ≥70% (from current ~39%)
- Acceptance: Critical paths guarded; handler coverage ≥70%

---

## Tier 6 — Final Polish ⏳ OPEN (2-3 days)

### WP-20: Public Page SEO & OG Polish (1 day)

#### WP-20.1: JSON-LD Event Schema
- [ ] `EventDetail.razor`: render `<script type="application/ld+json">` with `schema.org/Event`
- [ ] Required: `@type`, `name`, `startDate`, `endDate`, `eventStatus`, `eventAttendanceMode`
- [ ] `location`: `Place` / `VirtualLocation` / both (hybrid)
- [ ] `organizer`: `Organization` or `Person`
- [ ] `offers`: include only if `Price > 0`
- [ ] `image`: featured image URL (absolute)
- Acceptance: Valid JSON-LD on EventDetail

#### WP-20.2: JSON-LD Organization Schema
- [ ] `OrganizationProfile.razor`: `schema.org/Organization`
- [ ] Fields: `name`, `url`, `logo`, `description`, `sameAs` (social links)
- Acceptance: Valid JSON-LD on OrganizationProfile

#### WP-20.3: JSON-LD Breadcrumb Schema
- [ ] Detail pages: `schema.org/BreadcrumbList` (Home → List → Detail)
- Acceptance: Breadcrumb structured data on detail pages

#### WP-20.4: OG/Twitter Meta Tags
- [ ] Add to: Home, LandingForNonUsers, LandingForUsers, OrganizationProfile, OrganizationDetails
- [ ] Pattern: `og:title`, `og:description`, `og:type`, `og:url`, `og:image`, `og:site_name`, `twitter:card`, etc.
- [ ] Tenant-aware: brand name + logo from `PublicExperienceService`
- Acceptance: OG tags present on all public pages

#### WP-20.5: Tests
- [ ] Integration: scrape EventDetail → validate JSON-LD
- [ ] Integration: assert OG tags on Home, EventDetail, OrganizationProfile
- Acceptance: Tests pass

**Acceptance:** Gate F "JSON-LD + OG tags everywhere" passes.

### WP-23: Accessibility Polish (1 day)

#### WP-23.1: Breadcrumbs
- [ ] Add `MudBreadcrumbs` to EventDetail, OrganizationDetails, OrganizationProfile, UserProfile, MyRegistrations
- [ ] Pair with JSON-LD BreadcrumbList from WP-20.3
- Acceptance: Breadcrumbs on key pages

#### WP-23.2: ARIA Landmarks
- [ ] `<main>` → `aria-label="Main content"`
- [ ] `<nav>` → `aria-label="Primary navigation"`
- [ ] `<aside>` → `aria-label="Sidebar"`
- [ ] Apply in `MainLayout.razor` and `SetupLayout.razor`
- Acceptance: Landmarks present

#### WP-23.3: Focus Management
- [ ] Create `FocusOnNavigate` component
- [ ] Set focus to page `<h1>` after SPA navigation
- Acceptance: Focus moves on route change

#### WP-23.4: Form Validation ARIA
- [ ] Audit: CreateEvent, EditEvent, CreateOrganization
- [ ] Ensure validation messages have `aria-describedby` linking to inputs
- Acceptance: Forms accessible

#### WP-23.5: Lighthouse Audit
- [ ] Run Lighthouse a11y on: Home, EventList, EventDetail, CreateEvent, MyRegistrations, OrganizationDetails
- [ ] Fix findings < 90
- Acceptance: Key pages score ≥90 Lighthouse a11y

### WP-24: PWA Manifest Only (0.5 day)

> Per D14: manifest only; no service worker, no offline for MVP.

#### WP-24.1: Manifest
- [ ] Create `Explore.Blazor/wwwroot/manifest.json` or controller endpoint
- [ ] Fields: `name`, `short_name`, `description`, `start_url=/`, `display=standalone`, `background_color`, `theme_color`, `icons` (192/256/384/512)
- [ ] Link from `App.razor`: `<link rel="manifest" href="/manifest.json">`
- [ ] Add `<meta name="theme-color" content="...">` matching brand primary
- Acceptance: Lighthouse shows "Manifest: yes"

#### WP-24.2: Tenant Awareness
- [ ] Per-tenant brand-aware manifest via controller endpoint (reads `PublicExperienceSettings`)
- [ ] Cache 5 minutes; vary by tenant + host
- Acceptance: Per-tenant manifest works

### WP-25: Placeholder & TODO Cleanup (0.5 day) — PARTIAL

#### WP-25.1: Replace Placeholder Images
- [x] `ImageHelper`: replace external placeholder URLs with local SVG data URI fallbacks
- [x] `MyEvents.razor.cs`: delegate fallback generation to `ImageHelper`
- [x] `MyRegistrations.razor`: swap `placehold.co` with real event image or branded fallback
- [x] Add `ImageHelperTests` coverage for featured image passthrough, local SVG fallbacks, and invalid color fallback
- [x] `LandingPageForNonUsers.razor`: verified `image/landing_image_nonuser.png` is backed by `Explore.Blazor.Client/wwwroot/image/landing_image_nonuser.png`
- Acceptance: No external placeholder URLs in production source; runtime visual smoke remains

#### WP-25.2: Resolve Critical TODOs
- [x] Replace `LandingPageService` hardcoded member-count TODO with `GetActorsAsync(...).TotalCount`
- [x] Add `LandingPageServiceTests` coverage for actor total-count extraction and API failure fallback
- [ ] Defer `ImageStorageService.DeleteImageAsync` TODO until API delete support is available
- [ ] Defer `EventSessionSpeakerService` generated-client TODOs until NSwag/client regeneration exposes the missing operations
- [ ] Sweep `EventList.razor`, `EventEdit.razor`, `CreateEvent.razor` for TODO/FIXME after publish-flow dirty work is reconciled
- Acceptance: Critical touched-file TODOs removed; generated-client/API-dependent TODOs explicitly deferred

#### WP-25.3: Price/CurrencyCode Audit
- [x] Source audit found `Price` / `CurrencyCode` display-only usage in event cards/detail/list/created pages
- [x] Source audit found no visible `Price` / `CurrencyCode` inputs in `EventEdit.razor`; `TechAspectEditDialog` prize currency is unrelated
- [ ] Re-check `CreateEvent` after publish-flow dirty work is reconciled before final sign-off
- Acceptance: Decision documented; no misleading UI

#### WP-25.4: Final Docs Pass
- [ ] Update README quickstart (Redis optionality)
- [ ] Ensure API docs reflect new endpoints (sitemap, calendar, unsubscribe)
- Acceptance: Docs current

---

## Tier 7 — Pre-Existing Deferred Items ⏳ DECISION NEEDED (2 days)

### WP-10: User Onboarding — Gap Analysis (1 day)
### WP-12: Production Docker & External API Key (1 day)

---

## Closure Order (Rebaselined 2026-05-03)

| Phase | Work Packages | Gate Target |
|-------|---------------|-------------|
| 0 | Evidence baseline: reconcile task statuses, dirty worktree, build/test baseline, Docker blocker | No stale assumptions |
| 1 | WP-1, WP-6, WP-14, WP-15, WP-16, WP-17 verification/closure | Gates A/B/D/E/F partial |
| 2 | WP-3 registration email + remaining WP-14 dispatch consent/header work + registration duplicate/waitlist evidence | Gates C/D/E |
| 3 | WP-18 health, WP-19 audit/CSP, WP-8 HAL fallback/RoleHelper cleanup, WP-12 disable/harden external API keys, WP-9 publish flow after in-flight source is reconciled | Gate G |
| 4 | WP-20 JSON-LD/OG, WP-23 accessibility, WP-24 manifest, WP-25 placeholder/TODO cleanup, WP-10 waiver/closure | Gate F polish |
| 5 | WP-21 E2E, WP-22 snapshots, WP-11 handler/test coverage, final gate sign-off | Gate H / all gates green |

### If Schedule Compresses
- Defer manifest/offline-adjacent polish before deferring registration integrity, unsubscribe, health, audit, or runtime evidence.
- Do not defer WP-21/WP-22 entirely; at minimum, critical-flow E2E and HATEOAS/ProblemDetails snapshots must provide release evidence.
- Never slip: registration truthfulness, capacity/duplicate safety, unsubscribe compliance, health/readiness, audit/security, and BFF token safety.

---

## NSwag Regeneration Checklist

Apply whenever API surface changes (WP-3, WP-6, WP-14, WP-16):

1. Update API (controllers, DTOs, handlers)
2. Build `Explore.API` → build-time OpenAPI generation refreshes `swagger.json`
3. `dotnet build Explore.Blazor.Client` → `EventApiClient.g.cs` regenerated
4. Fix consuming code broken by contract changes
5. `dotnet test --project Explore.Blazor.Client.Tests`

---

## Quick Resume

1. Read `mvp-launch-context.md` for key decisions
2. Check this file for current progress
3. Start with Phase 0 evidence/status reconciliation from `mvp-launch-plan.md`, not WP-1.4
4. Close runtime evidence for implemented foundations: WP-1, WP-6, WP-14, WP-15, WP-16, WP-17
5. Then complete registration email/consent, health/audit/CSP/HAL cleanup, public SEO polish, and test evidence in that order
6. Follow NSwag checklist after any API changes

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
