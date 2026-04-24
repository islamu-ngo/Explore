ABOUTME: Key decisions, files, and constraints for the MVP launch sprint.
ABOUTME: Read this file first when resuming work on MVP launch tasks.

# MVP Launch — Context

## SESSION PROGRESS

### ✅ COMPLETED (2026-03-29)
- Full MVP report analysis (`dev/active/mvp-report.md`)
- Architecture, domain, API, security, outbox, and Blazor docs review
- Verified Blazor Dockerfile bug (confirmed .NET 9.0, API Dockerfile already at 10.0)
- Verified Redis missing from docker-compose.yml
- Reviewed in-flight tracks: HATEOAS (5 phases not started), External API (Phases 0-4 done), Navbar (Phases 1-6 done)
- Created implementation plan with 13 work packages across tiered gates
- Architect review incorporated — tiers split, gates added, scope refined

### ✅ COMPLETED (2026-04-24)
- **6 parallel codebase audits** launched and completed:
  - `bg_c31f5002`: Event domain + registration flow mapping
  - `bg_38ed2dd0`: Blazor UI completeness audit
  - `bg_2c93737d`: Infra/notifications/observability/config audit
  - `bg_82fa4e05`: Test coverage + architecture tests audit
  - `bg_e5a7ccf0`: Multi-tenancy + auth + security audit
  - `bg_3189f603`: Docs + PWA + public-facing surface audit
- **Plan extended** from 13 WPs / 4 gates / 4 tiers → **25 WPs / 7 gates / 6 tiers**
- **12 new decisions** added (D11–D22)
- **12 new work packages** added (WP-14 through WP-25)
- **3 new release gates** added (Gate E: Legal/Compliance, Gate F: SEO/Discoverability, Gate G: Security Audit Trail)
- `mvp-launch-plan.md` fully rewritten (~1066 lines)
- Risk table expanded from 10 → 22 risks
- Sprint plan reorganized to 8 sprints / 18-day target

### ✅ IMPORTANT DISCOVERIES (2026-03-29)
- **MyRegistrations page EXISTS** at `Pages/User/MyRegistrations.razor` (route `/my/registrations`)
- **Share functionality EXISTS** in `EventDetail.razor` via `ShareEventAsync()` (Web Share API + clipboard)
- **EventRegistrationService** has full CRUD including `GetRegistrationsByUserAsync`, `CancelRegistrationAsync`
- **EventRegistrationController** has `GET /api/eventregistration/by-user/{userId}` and `DELETE`
- **Admin onboarding EXISTS** in `Pages/Onboarding/` (InstanceOnboarding, TenantOnboarding, StartupGate)
- WP-4, WP-7, WP-10 scope dramatically reduced

### ✅ IMPORTANT DISCOVERIES (2026-04-24 audit)
- **Legal pages exist** (Privacy, Terms, Community Guidelines) — good baseline, no License/Accessibility statement yet
- **Cookie consent banner exists** — GDPR-compliant, non-blocking, equal Accept/Decline buttons
- **Analytics bridge exists** — privacy-first (PostHog/Plausible/Umami support), consent-driven
- **Audit log entity exists** — schema is there but admin-action/PII-access logging is missing
- **Notification entity exists** — in-app notifications already implemented (MarkAsRead, Archive, Snooze handlers)
- **Registration intent aggregate exists** — parent `EventRegistrationIntent` + child `EventRegistration` rows with policy snapshot
- **`Ical.Net`-compatible infrastructure not yet added** — library is green-lit via D7 but not installed
- **No dedicated error pages** — only generic `Error.razor` (development-focused, not branded)
- **No sitemap.xml / robots.txt** — SEO baseline broken
- **No JSON-LD on any page** — structured data missing
- **All UI strings are hardcoded English** — no `IStringLocalizer` usage; `LanguagePicker.razor` exists but non-functional
- **Obsolete HAL legacy fallback** — `MapMethodToAction()` still active for policies missing explicit `PermissionAction`
- **Capacity fields exist** (`EventSession.MaxAudienceAttendees`, `CurrentAudienceAttendees`) but **not enforced** in registration handler
- **Only 5 E2E smoke tests** — no critical-flow coverage
- **Placeholder images** (placehold.co references, landing_image_nonuser.png potentially missing)
- **Rate limit on setup-secret already exists** as `setup_secret` policy (5/60s/IP) — just needs to be applied to the validation endpoint

### 🟡 IN PROGRESS
- `mvp-launch-context.md` — being updated now

### ⚠️ BLOCKERS
- None

---

## Key Decisions

### D1: Email Verification Ownership
- **Email verification is Keycloak's job** (user correction)
- AT Proto handle → PDS's responsibility
- App does NOT implement email verification or account creation

### D2: DataProtection Strategy
- **Blazor BFF only.** Do NOT register in API project. API is bearer-only, never needs the BFF key ring.
- **Separate `DataProtectionKeyContext`** — NOT on `ExploreDbContext` (keys are global, not tenant-scoped; avoids pooling/filter complexity)
- NuGet: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` v10.0.5 (explicit reference)
- `SetApplicationName("islamu-event")`
- Migration: `--context DataProtectionKeyContext --output-dir Migrations/DataProtection`
- Migration committed to source; auto-applied at startup by `Event.MigrationService`
- Table: `DataProtectionKeys` (Id int PK, FriendlyName text?, Xml text?) — entity provided by package

### D3: Outbox Payload — Reference (not Snapshot)
- Payload contains **IDs only**: `registrationId`, `eventId`, `eventSessionId`, `userId`, `tenantId`, `correlationId`
- Handler fetches fresh data from repos at dispatch time
- Why: smaller payload, fresher data, no stale snapshot if event edited after registration
- Trade-off: more DB reads at dispatch; acceptable for MVP volume

### D4: Email Idempotency
- Handler keyed by `(EventType="RegistrationConfirmed", registrationId)`
- Must check prior send state before dispatching
- Options: `ConfirmationEmailSentAt` marker on registration entity, or accept rare duplicates with logging
- Structured logging fields: `RegistrationId`, `EventId`, `UserId`, `TenantId`, `OutboxMessageId`

### D5: Outbox Dispatcher Strategy
- Replace `LoggingOutboxMessageDispatcher` with `RoutingOutboxMessageDispatcher`
- Strategy pattern: `IOutboxMessageHandler` per event type
- Unhandled types fall back to logging (preserves current behavior)

### D6: Email Template Approach
- Simple string interpolation for MVP (no Razor/Liquid/Scriban engine)
- Template lives in `Explore.Infrastructure/Mail/Templates/`
- Tenant branding (logo, colors) is post-MVP

### D7: iCal Library
- `Ical.Net` v5.2.1 (NuGet) — netstandard2.0, MIT, 28M+ downloads
- Stable UID = event GUID (not random — allows calendar app updates)
- UTC normalization for all timestamps
- Google Calendar URL as optional complement

### D8: Redis — Graceful Degradation
- **App must work optimally without Redis.** In-memory fallback is mandatory.
- Context: self-hostable platform. Minimal infra = Blazor + API + DB. Redis/Cerbos/Keycloak etc. are optional enhancements.
- App must log effective cache backend at startup (Redis vs in-memory)
- If Redis configured but unavailable: log warning, degrade to in-memory, do NOT fail startup

### D9: External API Key — Disable for MVP
- Do NOT ship with unlimited API key access
- Disable endpoints via config until Phase 5 rate limiting is complete
- Safer product decision than partial security

### D10: WP-9 Scope — Split
- **MVP minimum:** Explicit "Save as Draft" / "Publish" buttons on create/edit form
- **Post-MVP:** `beforeunload` guard, advanced status transitions, undo publish
- `beforeunload` is easy to underestimate and annoying if done poorly

### D11: i18n Explicitly Deferred (NEW 2026-04-24)
- **Decision:** Hardcoded English only for v1 launch. No `IStringLocalizer`, no `.resx`, no RTL detection.
- **Rationale:** Launching v1 to English-speaking audience; full locale coverage is a 1-2 week project on its own.
- **Revisit:** After MVP launch, prioritize if non-English community adoption materializes.
- **Blazor-localization track:** Formally parked. Reference `dev/active/blazor-localization/`.

### D12: Capacity Enforcement Scope (NEW 2026-04-24)
- **MVP:** Prevent over-registration via atomic SQL check + auto-waitlist when any session is full.
- **Post-MVP:** Auto-promote waitlist on cancellation; capacity-alert emails; bulk approval UI; CSV export.

### D13: Unsubscribe Mechanism (NEW 2026-04-24)
- **Implementation:** Per-category tokens (not a single kill-switch). Categories: `registration-confirmations`, `event-reminders` (future), `event-updates` (future), `organizer-announcements`.
- **One-click compliance:** RFC 8058 `List-Unsubscribe` + `List-Unsubscribe-Post` headers; GET link in email body.
- **Token encryption:** `ITimeLimitedDataProtector` via BFF's DataProtection key ring (reuses WP-1.2 infra). 180-day lifetime.

### D14: PWA Scope (NEW 2026-04-24)
- **MVP:** `manifest.json` only (makes app installable).
- **Post-MVP:** Service worker, offline caching, background sync, push notifications.

### D15: Health-Check Strategy (NEW 2026-04-24)
- **Ready tagged:** Database, Redis (if enabled), Keycloak OIDC discovery, SMTP.
- **Degraded vs Unhealthy:** Redis + SMTP report `Degraded` when app has working fallback; `Unhealthy` forces pod NotReady.

### D16: Audit-Log Access Control (NEW 2026-04-24)
- **Instance admin:** full visibility across all tenants.
- **Tenant admin:** only their tenant's audit entries.
- **Regular user:** own actions only via `/api/users/me/audit-log`.
- **Permission key:** `audit_log:read` (tenant-scoped) + `audit_log:read_all` (instance-scoped).

### D17: Setup-Secret Rate-Limiting Policy (NEW 2026-04-24)
- Reuse existing `setup_secret` policy (5/60s/IP) — no new policy needed.
- Apply to `validate-secret` endpoint; emit warning log after 3 consecutive failures.

### D18: Error-Page Strategy (NEW 2026-04-24)
- Three dedicated routes: `/errors/404`, `/errors/403`, `/errors/500`.
- Middleware re-executes status code pages for non-interactive responses.
- Pages branded with tenant logo + site name; display correlation ID on 500.

### D19: RSS/ICS Feeds Deferred (NEW 2026-04-24)
- **MVP:** Single-event `.ics` download (WP-6).
- **Post-MVP:** Organization-level `.ics` feed, tenant-level `.ics`, RSS/Atom for discovery.

### D20: Snapshot-Testing Library (NEW 2026-04-24)
- **Choice:** `Verify.TUnit` v26+ if TUnit adapter available; fall back to `Verify.Xunit`.
- **Snapshot location:** `tests/snapshots/` (shared across projects).
- **Review policy:** any snapshot diff must be PR-reviewed visually.

### D21: Placeholder Asset Policy (NEW 2026-04-24)
- **Production rule:** no `placehold.co` references, no missing image paths.
- Every image either resolves or uses a branded CSS fallback pattern.
- When tenant hasn't uploaded a logo, use instance default logo.

### D22: Capacity Enforcement Mode (NEW 2026-04-24)
- **Auto-waitlist on full** — parent intent = `Waitlisted` when any child session is full.
- **Rejected alternative:** Option B (reject whole registration) — worse UX.
- **Concurrency:** SQL `UPDATE ... WHERE CurrentAudienceAttendees < MaxAudienceAttendees RETURNING ...` in same transaction.

---

## Key Files by Work Package

### WP-1: Infrastructure Fixes
| File | Change |
|------|--------|
| `Explore.Blazor/Dockerfile` | .NET 9 → 10 (both base and SDK lines) |
| `Explore.API/Dockerfile` | Already .NET 10 — no change |
| `docker-compose.yml` | Add Redis service + redis_data volume |
| `Explore.Blazor/Program.cs` | AddDataProtection().PersistKeysToDbContext() |
| New: `Explore.Persistence/DataProtectionKeyContext.cs` | Separate DbContext for keys |
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` line 87 | Remove broken email promise |

### WP-2: Navbar Customization Phase 7
| File | Change |
|------|--------|
| `dev/active/navbar-customization/navbar-customization-tasks.md` | Reference for Phase 7 tasks |
| Various Blazor UI files | Soft-delete compliance, URL validators, cache invalidation |

### WP-3: Registration Email
| File | Change |
|------|--------|
| `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` | Create OutboxMessage with reference payload |
| New: `Explore.Application/Contracts/Outbox/IOutboxMessageHandler.cs` | Handler interface |
| New: `Explore.Infrastructure/Outbox/RoutingOutboxMessageDispatcher.cs` | Replaces logging dispatcher |
| New: `Explore.Infrastructure/Outbox/Handlers/RegistrationConfirmedOutboxHandler.cs` | Email dispatch |
| New: `Explore.Infrastructure/Mail/Templates/RegistrationConfirmedEmailBuilder.cs` | HTML template |
| `Explore.Application/Contracts/Infrastructure/IEmailService.cs` | Existing — no change |
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | Existing — no change |

### WP-4: My Registrations Enhancement
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/User/MyRegistrations.razor` | Add calendar button per card |
| `Explore.Blazor.Client/Shared/NavMenu.razor` or user menu | Verify link exists |

### WP-5: Post-Registration UX
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` lines 77-96 | Enhance success state |

### WP-6: iCal
| File | Change |
|------|--------|
| `Explore.API/Controllers/EventController.cs` | Add calendar endpoint |
| `Explore.API/Hateoas/RouteNames.cs` | Add `GetEventCalendar` constant |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Add calendar button |
| `Explore.Blazor.Client/Pages/User/MyRegistrations.razor` | Add per-event calendar button |

### WP-7: Share Verification
| File | Check |
|------|-------|
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Share button visible? |
| `Explore.Blazor.Client/Pages/Events/Components/EventCard.razor` | Share on list cards? |

### WP-8: HATEOAS Fix
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs` | Remove RoleHelper.CanManage, use HasHalLink |
| `Explore.Blazor.Client/Helpers/HalResourceExtensions.cs` | Add HasHalLink for Org/Group DTOs |
| `Explore.API/Hateoas/LinkDefinitionDerivation.cs` | Remove obsolete `MapMethodToAction()` |
| All link policy classes | Ensure explicit `RequirePermission("resource", "action")` |

### WP-9: Save Draft vs Publish
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` | Add "Save as Draft" + "Publish" buttons |
| `Explore.Blazor.Client/Pages/Events/EditEvent.razor` | Conditional buttons based on status |

### WP-14: Email Unsubscribe (NEW)
| File | Change |
|------|--------|
| New: `Explore.Infrastructure/Mail/Unsubscribe/UnsubscribeTokenService.cs` | Token encrypt/decrypt via DataProtection |
| New: `Explore.API/Controllers/EmailUnsubscribeController.cs` | GET + POST unsubscribe endpoints |
| New or existing: `UserNotificationPreferences` entity + repository | Preference categories table |
| `Explore.Infrastructure/Mail/Templates/RegistrationConfirmedEmailBuilder.cs` | Accept + render unsubscribe URL |
| `Explore.Infrastructure/Mail/SmtpEmailService.cs` | Inject List-Unsubscribe headers |
| `dev/active/organizer-email-consent/` | Reconcile scope before starting |

### WP-15: Branded Error Pages (NEW)
| File | Change |
|------|--------|
| New: `Explore.Blazor.Client/Pages/Errors/NotFound.razor` | `@page "/errors/404"` |
| New: `Explore.Blazor.Client/Pages/Errors/Unauthorized.razor` | `@page "/errors/403"` |
| New: `Explore.Blazor.Client/Pages/Errors/ServerError.razor` | `@page "/errors/500"` |
| `Explore.Blazor/Components/Pages/Error.razor` | Enhance with branded content |
| `Explore.Blazor/Program.cs` | `UseStatusCodePagesWithReExecute("/errors/{0}")` |
| `Explore.Blazor.Client/Routes.razor` | Add catch-all route fallback |

### WP-16: SEO Foundation (NEW)
| File | Change |
|------|--------|
| New: `Explore.API/Controllers/SitemapController.cs` | `GET /sitemap.xml` |
| New: `Explore.Blazor/wwwroot/robots.txt` (static) or controller | Per-environment directives |
| Multiple `.razor` pages | Add `<link rel="canonical">` tags |

### WP-17: Capacity Enforcement (NEW)
| File | Change |
|------|--------|
| `CreateEventRegistrationCommandHandler.cs` | Capacity check + auto-waitlist |
| `EventRegistration.razor` | "Join waitlist" button when full |
| `MyRegistrations.razor` | Verify waitlist badge renders |
| New migration | Unique index on `(UserId, EventSessionId)` where `IsDeleted=false` |

### WP-18: Health Checks (NEW)
| File | Change |
|------|--------|
| `Explore.API/Program.cs` | Add Redis/Keycloak/SMTP health checks |
| `Explore.Blazor/Program.cs` | Same |
| New: `Explore.Infrastructure/HealthChecks/KeycloakHealthCheck.cs` | OIDC discovery check |
| New: `Explore.Infrastructure/HealthChecks/SmtpHealthCheck.cs` | SMTP connection check |
| `docs/TROUBLESHOOTING.md` | Health-check interpretation table |

### WP-19: Security Audit Trail (NEW)
| File | Change |
|------|--------|
| `Explore.API/Controllers/InstanceOnboardingController.cs` | Apply `setup_secret` rate limit |
| `Explore.Persistence/Repositories/UserPiiRepository.cs` | Wrap reads with audit logging |
| `Explore.Persistence/Repositories/ActorPiiRepository.cs` | Same |
| New: `Explore.Application/Common/Behaviors/AuditLoggingBehavior.cs` | MediatR pipeline behavior |
| `Explore.Application/Common/Authorization/FallbackAuthorizationService.cs` | Log denials |
| New: `Explore.Blazor/Middleware/BffCspMiddleware.cs` | CSP header middleware |
| `Explore.API/Hateoas/LinkDefinitionDerivation.cs` | Delete `MapMethodToAction()` |
| All 36 link policy classes | Ensure explicit `RequirePermission` |

### WP-20: Public Page SEO/OG (NEW)
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | JSON-LD `schema.org/Event` |
| `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor` | JSON-LD `schema.org/Organization` |
| `Home.razor`, `LandingPageForNonUsers.razor`, `LandingPageForUsers.razor` | OG/Twitter meta tags |
| `OrganizationDetails.razor` | OG tags + breadcrumbs |

### WP-21: E2E Critical-Flow Tests (NEW)
| File | Change |
|------|--------|
| New: `Explore.Blazor.Client.E2ETests/CriticalFlows/RegistrationFlowTests.cs` | Full registration E2E |
| New: `Explore.Blazor.Client.E2ETests/CriticalFlows/MultiTenancyIsolationTests.cs` | Tenant isolation |
| New: `Explore.Blazor.Client.E2ETests/CriticalFlows/AuthorizationEnforcementTests.cs` | Authz check |
| New: `Explore.Blazor.Client.E2ETests/CriticalFlows/BffTokenForwardingTests.cs` | Token chain |

### WP-22: Snapshot Tests (NEW)
| File | Change |
|------|--------|
| `Event.API.IntegrationTests` project | Install `Verify.TUnit` or `Verify.Xunit` |
| New: `tests/snapshots/` directory | Baseline snapshot storage |
| New test files | Snapshot EventDto, OrgDto, UserDto, ProblemDetails |

### WP-23: Accessibility Polish (NEW)
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Add breadcrumbs |
| `Explore.Blazor.Client/Layout/MainLayout.razor` | ARIA landmarks |
| `Explore.Blazor.Client/Layout/SetupLayout.razor` | ARIA landmarks |
| New: `Explore.Blazor.Client/Shared/FocusOnNavigate.razor` | Focus management component |

### WP-24: PWA Manifest (NEW)
| File | Change |
|------|--------|
| New: `Explore.Blazor/wwwroot/manifest.json` or controller endpoint | PWA manifest |
| `Explore.Blazor/App.razor` | Link manifest + theme-color meta |

### WP-25: Placeholder & TODO Cleanup (NEW)
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/User/MyRegistrations.razor` | Replace placehold.co |
| `Explore.Blazor.Client/Pages/Landing/LandingPageForNonUsers.razor` | Verify landing image |
| `EventList.razor`, `EventEdit.razor`, `CreateEvent.razor` | Resolve TODOs |

---

## Technical Constraints

1. Repositories return entities, never DTOs — mapping in handlers
2. Validators are manually instantiated — no DI
3. Commands return `BaseCommandResponse<Guid>`
4. GET = AllowAnonymous, write = Authorize
5. File-scoped namespaces for new C# files
6. ABOUTME header on all new files
7. Outbox messages are at-least-once — consumers must be idempotent
8. NSwag client regeneration required after API changes (see checklist in plan)
9. Named query filters for soft delete: `.HasQueryFilter(name: "SoftDelete", ...)`
10. Never blunt `IgnoreQueryFilters()` — always use `IgnoreQueryFilters([QueryFilterNames.SoftDelete])`
11. TUnit is the test framework (not xUnit/NUnit)
12. bUnit for Blazor component tests
13. App must work without Redis (in-memory fallback)
14. HAL `_links` is exclusive source of UI action affordance — never use `RoleHelper.CanManage` or `IsInRole`
15. EF Core named query filters: `.HasQueryFilter(name: "SoftDelete", predicate: e => !e.IsDeleted)`
16. UserId extraction fallback: `sub` → `nameidentifier` → `sid`
17. HATEOAS link policies use `yield return` pattern (not `list.Add()`)
18. `EventQuerySpecification` is immutable — every `With*()` returns new instance
19. Capacity enforcement uses atomic SQL `UPDATE ... WHERE ... RETURNING ...` (not application-level check)
20. Unsubscribe tokens use `ITimeLimitedDataProtector` — 180-day lifetime, per-category
21. CSP header must include `wasm-unsafe-eval` for Blazor WASM + `'unsafe-inline'` for MudBlazor styles

---

## Quick Resume

1. Read this context file
2. Check `mvp-launch-tasks.md` for current progress
3. Read `mvp-launch-plan.md` for overall strategy and release gates
4. Start with WP-1.4 (broken promise fix) → then WP-1.1/1.2/1.3 → smoke test Gates A+B
5. Follow NSwag checklist (in plan) after any API changes
6. Sprint order: WP-1 → WP-14/15/16 → WP-17/18/19 → WP-3/6/7/4/5 → WP-20/24/8 → WP-2/9/12 → WP-21/22/11 → WP-23/25/10
