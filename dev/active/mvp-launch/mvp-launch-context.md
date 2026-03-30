ABOUTME: Key decisions, files, and constraints for the MVP launch sprint.
ABOUTME: Read this file first when resuming work on MVP launch tasks.

# MVP Launch — Context

## SESSION PROGRESS (2026-03-29)

### ✅ COMPLETED
- Full MVP report analysis (`dev/active/mvp-report.md`)
- Architecture, domain, API, security, outbox, and Blazor docs review
- Verified Blazor Dockerfile bug (confirmed .NET 9.0, API Dockerfile already at 10.0)
- Verified Redis missing from docker-compose.yml
- Reviewed in-flight tracks: HATEOAS (5 phases not started), External API (Phases 0-4 done), Navbar (Phases 1-6 done)
- Created implementation plan with 13 work packages across tiered gates
- Architect review incorporated — tiers split, gates added, scope refined

### ✅ IMPORTANT DISCOVERIES (from codebase exploration)
- **MyRegistrations page EXISTS** at `Pages/User/MyRegistrations.razor` (route `/my/registrations`)
- **Share functionality EXISTS** in `EventDetail.razor` via `ShareEventAsync()` (Web Share API + clipboard)
- **EventRegistrationService** has full CRUD including `GetRegistrationsByUserAsync`, `CancelRegistrationAsync`
- **EventRegistrationController** has `GET /api/eventregistration/by-user/{userId}` and `DELETE`
- **Admin onboarding EXISTS** in `Pages/Onboarding/` (InstanceOnboarding, TenantOnboarding, StartupGate)
- WP-4, WP-7, WP-10 scope dramatically reduced

### 🟡 IN PROGRESS
- Nothing yet — ready for execution

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

### WP-6: iCal
| File | Change |
|------|--------|
| `Explore.API/Controllers/EventController.cs` | Add calendar endpoint |
| `Explore.API/Hateoas/RouteNames.cs` | Add `GetEventCalendar` constant |
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor` | Add calendar button |
| `Explore.Blazor.Client/Pages/User/MyRegistrations.razor` | Add per-event calendar button |

### WP-5: Post-Registration UX
| File | Change |
|------|--------|
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` lines 77-96 | Enhance success state |

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
10. TUnit is the test framework (not xUnit/NUnit)
11. bUnit for Blazor component tests
12. App must work without Redis (in-memory fallback)

---

## Quick Resume

1. Read this context file
2. Check tasks file for current progress
3. Read the plan for overall strategy and release gates
4. Start with WP-1.4 (broken promise fix) → then WP-1.1/1.2/1.3 → smoke test Gates A+B
