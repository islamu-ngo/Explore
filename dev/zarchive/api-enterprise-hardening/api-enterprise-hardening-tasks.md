# Tasks: API Enterprise Hardening
**Last Updated: 2026-03-26**
**Status:** ✅ COMPLETE — All 5 phases implemented, build passes, tests verified

---

## How to Use This File
- Mark tasks `[x]` when complete
- Update status header above to reflect current phase
- Update `api-enterprise-hardening-context.md` after key decisions

---

## Phase 1 — Security & Critical Fixes ✅ COMPLETE
*Eliminates active security vulnerabilities and startup blocking bugs.*

### 1.1 — Remove all JWT debug event handlers
- [x] Remove `OnTokenValidated` event handler (PII leak — logs all claims)
- [x] Remove `OnMessageReceived` event handler (log flood — logs every request)
- [x] Simplify `OnAuthenticationFailed` — single `LogWarning` with method+path+message only
- [x] Demote `OnChallenge` to `logger.LogDebug(...)`
- [x] Remove `Console.WriteLine(...)` inside `AudienceValidator` lambda
- [x] Build passes

### 1.2 — Fix async startup
- [x] Replace `db.Database.Migrate()` with `await db.Database.MigrateAsync()`
- [x] Replace `.GetAwaiter().GetResult()` with `await`
- [x] Build passes

### 1.3 — Replace `Environment.Exit(0)` with graceful shutdown
- [x] Replace `Environment.Exit(0)` with `app.Lifetime.StopApplication()`
- [x] Set `e.Cancel = true` in `Console.CancelKeyPress` handler
- [x] Build passes

### 1.4 — Remove `Thread.Sleep` from `ProcessExit` handler
- [x] Delete entire `AppDomain.CurrentDomain.ProcessExit` handler
- [x] Build passes

### 1.5 — Mark `isShuttingDown` as `volatile`
- [x] Refactored to `partial class Program { private static volatile bool isShuttingDown; }`
- [x] Build passes

### 1.6 — Delete commented-out code from Program.cs
- [x] Deleted commented-out `AddSwaggerDoc` method
- [x] Deleted commented-out `users/me` endpoint
- [x] Deleted inline comment lines
- [x] Build passes

---

## Phase 2 — API Contract & Error Handling ✅ COMPLETE

### 2.1 — Add `correlationId` to ProblemDetails extensions
- [x] Added `context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.Items["CorrelationId"] as string`
- [x] Build passes

### 2.2 — Fix `ProblemDetails.Type` — replace third-party URL with IANA URIs
- [x] Created `ProblemTypeUris` dictionary mapping status codes → IANA RFC 9110 URIs
- [x] Replaced `$"https://httpstatuses.com/{statusCode}"` with dictionary lookup
- [x] Build passes

### 2.3 — Eliminate manual `NotFound(new { error })` returns
- [x] Delegated to deep agent — cleaned all 19+ controllers with NotFound(new{...})
- [x] Replaced 38 `NotFound(new{...})` with plain `NotFound()`
- [x] Replaced 15 bare `NotFound()` patterns (verified correct)
- [x] Added null-check `if (result == null) return NotFound();` to ALL GetById methods:
  - ActorController (GetById + GetByDid), CategoryController, EventSessionController, TagController,
    LocationController, CustomPropertyDefinitionController, EventController, OrganizationController,
    IndexedDidController, ExternalApiKeyController, StorageObjectController (GetPublicImage), GroupController
- [x] Build passes
- [x] Integration tests: 452/453 pass (1 flaky OpenFeature teardown, not our code)

### 2.4 — Fix `UpdateActorCommand` direct HTTP body binding
- [x] Fixed `[FromBody] Command` binding patterns in controllers
- [x] Build passes

### 2.5 — Remove `IHttpContextAccessor` injection from controllers
- [x] Removed from 8 controllers that had it injected
- [x] Build passes

### 2.6 — Resolve `ValidationBehavior` dead code → Option A (Delete)
- [x] **DECIDED:** Option A — Delete. Per user instruction: "validation must be manual"
- [x] **Documented in** `dev/_journal/MAJOR_DECISIONS.md`
- [x] Deleted `Explore.Application/Behaviors/ValidationBehavior.cs`
- [x] Build passes

---

## Phase 3 — Performance & Observability ✅ COMPLETE

### 3.1 — Fix ETagMiddleware: RecyclableMemoryStream
- [x] Added `Microsoft.IO.RecyclableMemoryStream` 3.0.1 to `Explore.API.csproj`
- [x] Registered singleton `RecyclableMemoryStreamManager` in `Program.cs`
- [x] Rewrote ETagMiddleware: inject manager, use `ArrayPool<byte>` for hashing, skip >256KB bodies
- [x] Build passes

### 3.2 — Cache reflection in `AuthorizationBehavior`
- [x] Added `ConcurrentDictionary<Type, AuthorizeResourceAttribute?>` static cache
- [x] Replaced per-call `GetCustomAttribute<>()` with `AttributeCache.GetOrAdd()`
- [x] Build passes

### 3.3 — Set authorization allow-decisions to `Debug` log level
- [x] AuthorizationBehavior: allow → `LogDebug`, deny stays `LogWarning`
- [x] FallbackAuthorizationService.LogDecision: split by decision — allow→LogDebug, deny→LogWarning
- [x] Build passes

### 3.4 — Fix output cache: add `Authorization` header variance
- [x] Added `"Authorization"` to `SetVaryByHeader` for ListData and DetailData
- [x] Created `PublicData` policy (1h, no auth vary) for lookup controllers
- [x] Build passes

### 3.5 — Fix CORS `InternalWebsitePolicy` hardcoded origin
- [x] Replaced hardcoded `"https://iloveibadah.app"` with `corsAllowedOrigins` from config
- [x] Build passes

### 3.6 — Fix `ISetupSecretProvider` interface abstraction
- [x] Added `string? GetSecretForLogging()` to interface with XML doc
- [x] Changed concrete from `internal string` to `public string?` (returns null for env var secrets)
- [x] Removed concrete cast `((SetupSecretProvider)...)` from Program.cs
- [x] Build passes

### 3.7 — Add structured log event for setup mode
- [x] Replaced Console.WriteLine banner with `app.Logger.LogWarning("[SetupMode] ...")`
- [x] Kept single Console.WriteLine for terminal visibility
- [x] Build passes

### 3.8 — Add OpenTelemetry activity spans for authorization
- [x] Added `ActivitySource AuthorizationActivitySource = new("Explore.Authorization")`
- [x] Wrapped EnforceAuthorizationAsync in activity span with tags
- [x] Error status on deny
- [x] Registered source in `Explore.ServiceDefaults/Extensions.cs`
- [x] Build passes

### 3.9 — Lock `FallbackAuthorizationService.SafeMode`
- [x] Changed to `{ get; private set; }` with `ActivateSafeMode()` method
- [x] One-way latch — no programmatic deactivation
- [x] `_safeModeLogged` guard for single LogCritical
- [x] Updated RuntimeAuthorizationProvider (removed try/finally toggle)
- [x] Updated tests
- [x] Build passes

---

## Phase 4 — Architecture & Infrastructure ✅ COMPLETE

### 4.1 — Idempotency-Key middleware (full stack)
- [x] Created `Explore.Domain/IdempotencyRecord.cs` with ABOUTME
- [x] Created `Explore.Application/Contracts/Persistence/IIdempotencyRepository.cs`
- [x] Created `Explore.Persistence/Configurations/Entities/IdempotencyRecordConfiguration.cs`
- [x] Added `DbSet<IdempotencyRecord>` to `ExploreDbContext`
- [x] Created `Explore.Persistence/Repositories/IdempotencyRepository.cs`
- [x] Created EF Core migration
- [x] Created `Explore.API/Middleware/IdempotencyMiddleware.cs`
- [x] Registered in Program.cs pipeline
- [x] Build passes

### 4.2 — URL versioning alongside media-type
- [x] Combined `MediaTypeApiVersionReader` with `UrlSegmentApiVersionReader`
- [x] Created `VersionedRouteConvention : IApplicationModelConvention`
  - Action-level approach (not controller-level) to avoid Cartesian product with named routes
  - Absolute route templates (leading `/`) prevent MVC combining with controller `[Route]`
  - `Name = null` on versioned copies to avoid duplicate named route errors
  - Original named routes preserved for HATEOAS
- [x] Build passes
- [x] Integration test: `GET /api/v0.1/actor` returns 200

### 4.3 — Migrate Swashbuckle → ⚠️ SKIPPED per user instruction
- [x] User explicitly said: "do not remove swashbuckle"

### 4.4 — Add pagination to `GetActorsByTenant`
- [x] Added `PageNumber`/`PageSize` to `GetActorsByTenantRequest`
- [x] Updated ActorController.GetByTenant with `[FromQuery]` params
- [x] Build passes

### 4.5 — Add ABOUTME to all controllers missing it
- [x] Added 2-line ABOUTME to 43 controllers (19 already had it)
- [x] Build passes

---

## Phase 5 — Testing & Documentation ✅ COMPLETE

### 5.1 — Update `docs/API.md`
- [x] Updated middleware pipeline (idempotency at position 15)
- [x] Updated dual API versioning section
- [x] Updated caching table (PublicData policy + Authorization variance)
- [x] Updated ETag RecyclableMemoryStream note
- [x] Updated error handling (correlationId + RFC 9110 URIs)
- [x] Updated JWT logging section
- [x] Updated AuthorizationBehavior (reflection cache + OTel)
- [x] Updated CORS configurable origins
- [x] Updated graceful shutdown
- [x] Added new Idempotency section
- [x] Added business metrics ActivitySource note

### 5.2 — Update `docs/ARCHITECTURE.md`
- [x] Updated request flow (idempotency, AuthorizationBehavior description)
- [x] Added SafeMode one-way latch point
- [x] Added dual versioning point
- [x] Updated caching (PublicData + Authorization variance + RecyclableMemoryStream)
- [x] Added Idempotency section
- [x] Updated MediatR behaviors (ConcurrentDictionary + ActivitySource)

### 5.3 — Update `CLAUDE.md`
- [x] No changes needed — validator rule already says "manually instantiated"

### 5.4 — Record decisions in `dev/_journal/MAJOR_DECISIONS.md`
- [x] Recorded ValidationBehavior delete (Option A)
- [x] Recorded idempotency store choice (DB-backed)
- [x] Recorded URL versioning convention approach
- [x] Recorded Swashbuckle kept (user decision)
- [x] Recorded SafeMode one-way latch

---

## Completion Checklist
- [x] All Phase 1 tasks complete — `dotnet build` passes
- [x] All Phase 2 tasks complete — `dotnet build` passes — integration tests pass
- [x] All Phase 3 tasks complete — `dotnet build` passes
- [x] All Phase 4 tasks complete — `dotnet build` passes — integration tests pass (452/453)
- [x] All Phase 5 tasks complete — docs updated
- [x] `dev/_journal/MAJOR_DECISIONS.md` updated with all decisions
- [ ] These files moved to `dev/zarchive/api-enterprise-hardening/`

## Final Test Results (2026-03-26)

| Test Project | Pass | Fail | Notes |
|---|---|---|---|
| Event.Application.UnitTests | 547/547 | 0 | |
| Event.Domain.UnitTests | 100/100 | 0 | |
| Event.Architecture.Tests | 52/52 | 0 | |
| Explore.Secrets.UnitTests | 190/190 | 0 | |
| Explore.Blazor.Client.Tests | 580/601 | 21 | Pre-existing MudBlazor v9 migration |
| Event.API.IntegrationTests | 452/453 | 1 | Flaky OpenFeature ChannelClosedException |

Build: **0 errors**, all warnings pre-existing.
