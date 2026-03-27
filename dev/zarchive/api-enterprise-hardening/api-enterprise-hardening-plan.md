# Plan: API Enterprise Hardening
**Last Updated: 2026-03-26**

## Executive Summary

A systematic audit of the `Explore.API` stack identified **32 distinct findings** across Security, Performance, Code Quality, API Contract, Observability, and Architecture dimensions. The current API scores approximately **73/100** against enterprise-grade criteria. This plan addresses every finding through four ordered phases, delivering a **92+ score** that is genuinely ready to serve as the foundation for AT-Protocol PDS authentication, webhooks, MCP server, and API key infrastructure.

The phases are ordered by risk priority: Phase 1 eliminates active security liabilities and blocking bugs (P0), Phase 2 unifies the error contract (P1), Phase 3 hardens performance and observability (P2), Phase 4 adds missing infrastructure required before ATProto work begins (P3).

**No structural rewrites are required.** All fixes are targeted, isolated changes within existing files. The Clean Architecture layer boundaries are intact and correct.

---

## Current State Analysis

### Confirmed Problem Files

| File | Problems |
|------|----------|
| `Explore.API/Program.cs` | JWT event debug handlers (PII leak, log flood), sync startup calls, Thread.Sleep, Environment.Exit, commented code, volatile flag |
| `Explore.API/Middleware/ETagMiddleware.cs` | Full response body buffered in MemoryStream + ToArray() double-allocation |
| `Explore.API/Controllers/ActorController.cs` | UpdateActorCommand bound from body, IHttpContextAccessor unused, NotFound anonymous object |
| `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs` | Missing correlationId in extensions; ProblemDetails.Type uses third-party URL |
| `Explore.Application/Behaviors/AuthorizationBehavior.cs` | Reflection on every MediatR call, allow-decisions at Information level |
| `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` | Allow-decisions at Information level, SafeMode is public mutable property |
| `Explore.Application/Contracts/Services/ISetupSecretProvider.cs` | Missing GetSecretForLogging() method |
| `Explore.Application/ApplicationServicesRegistration.cs` | ValidationBehavior not registered (dead code clarification needed) |

### Confirmed Architecture State

- `ExceptionHandlingExtensions.cs` already adds `traceId` and `timestamp` to all ProblemDetails — **partial compliance with API-3**
- `ValidationBehavior.cs` exists but is **not registered** in `ApplicationServicesRegistration.cs` — validators are manually instantiated per CLAUDE.md rule, behavior is dead code
- `AddApiExceptionHandling()` chains `ValidationExceptionHandler` before `GlobalExceptionHandler` — correct order
- Output cache policies (`LookupData`, `ListData`, `DetailData`) missing `SetVaryByHeader("Authorization")` — security gap for HATEOAS auth-aware responses
- CORS `InternalWebsitePolicy` hardcodes `https://iloveibadah.app` — inconsistent with all other policies using config
- No `Idempotency-Key` header processing infrastructure anywhere in the stack

---

## Proposed Future State

1. **Zero PII-leaking JWT event handlers** — all four verbose JWT event callbacks removed from production, replaced with minimal targeted logging
2. **Uniform RFC 7807 error contract** — all 404/400/403 responses go through `GlobalExceptionHandler`; no anonymous-object returns
3. **Async startup** — no blocking calls on startup thread
4. **ETag with pooled buffers** — `RecyclableMemoryStream` eliminates per-request heap allocations
5. **Authorization decisions at correct log level** — `allow` → `Debug`, `deny` → `Warning`
6. **Idempotency-Key header support** — middleware stores and checks keys for write operations
7. **URL versioning alongside media-type** — both `/api/v0.1/actor` and `Accept: application/json;v=0.1` work
8. **Single documentation system** — Swashbuckle removed, native OpenAPI + Scalar only
9. **Validated, interface-clean setup secret** — `ISetupSecretProvider` extended, concrete cast removed from Program.cs
10. **ValidatorBehavior dead code resolved** — either registered (with validators moved to DI) or deleted

---

## Phase 1 — Security & Critical Fixes (P0)
*Goal: Eliminate active security vulnerabilities and startup blocking bugs before any other work. No behavioral changes to consumers.*

### Task 1.1 — Remove all four JWT debug event handlers
- **File:** `Explore.API/Program.cs` — JWT Bearer options block (lines 345–422)
- **Actions:**
  - Remove `OnTokenValidated` event handler entirely (logs all claims — PII leak)
  - Remove `OnMessageReceived` event handler entirely (logs every request — log flood)
  - Simplify `OnAuthenticationFailed` to a single `logger.LogWarning(...)` with only the exception message — remove raw token parsing
  - Remove `OnChallenge` or demote to `logger.LogDebug(...)` (non-actionable noise)
  - Remove `Console.WriteLine(...)` inside `AudienceValidator` lambda (line ~314)
  - Move all remaining JWT debug logging under `if (builder.Environment.IsDevelopment())`
- **Acceptance Criteria:**
  - [ ] No JWT claims appear in application logs at any log level in non-Development environments
  - [ ] No `Console.WriteLine` calls inside JWT configuration
  - [ ] Auth failures still log a Warning with exception message and request path
  - [ ] Auth challenges still log at Debug level
  - [ ] Unit test: mock `OnAuthenticationFailed` with a `SecurityTokenValidationException` — verify no claim values in log output
- **Effort:** S
- **Skills:** `auth-patterns`

---

### Task 1.2 — Remove `OnTokenValidated` PII token logging
*(Covered in 1.1 — explicitly called out for the test verification step)*
- The `[JWT] Token validated successfully` line with all claims must produce zero log output at `Information` or `Warning`
- **Effort:** XS (part of 1.1)

---

### Task 1.3 — Fix async startup: migrate to `MigrateAsync` and `await SeedAsync`
- **File:** `Explore.API/Program.cs` — startup migration block (lines 521–545)
- **Actions:**
  - Replace `db.Database.Migrate()` with `await db.Database.MigrateAsync()`
  - Replace `DatabaseSeeder.SeedAsync(db, app.Environment).GetAwaiter().GetResult()` with `await DatabaseSeeder.SeedAsync(db, app.Environment)`
  - Wrap the startup block in an async lambda or use top-level `await` (valid in Program.cs top-level statements)
- **Acceptance Criteria:**
  - [ ] No `.GetAwaiter().GetResult()` calls in startup
  - [ ] No synchronous `Migrate()` call
  - [ ] Application still starts correctly and applies migrations
  - [ ] Integration test: startup completes without `ThreadAbortException` or deadlock
- **Effort:** S
- **Skills:** `dotnet-efcore-guidelines`

---

### Task 1.4 — Replace `Environment.Exit(0)` with graceful host shutdown
- **File:** `Explore.API/Program.cs` — SIGINT handler (lines 497–504)
- **Actions:**
  - Replace `Environment.Exit(0)` with `app.Lifetime.StopApplication()`
  - Set `e.Cancel = true` to prevent immediate CLR termination (let host drain)
  - Remove `shutdownCts.Cancel()` if no other code depends on it
- **Acceptance Criteria:**
  - [ ] SIGINT (Ctrl+C) triggers graceful shutdown via `IHostApplicationLifetime`
  - [ ] `IHostedService.StopAsync()` is called for OutboxProcessor and PdsSyncWorker on Ctrl+C
  - [ ] No `Environment.Exit` calls in Program.cs
- **Effort:** S

---

### Task 1.5 — Remove `Thread.Sleep` from `ProcessExit` handler
- **File:** `Explore.API/Program.cs` — ProcessExit handler (lines 506–519)
- **Actions:**
  - Remove the entire `AppDomain.CurrentDomain.ProcessExit` handler
  - The host's `ShutdownTimeout` (configured at line 55, `GracefulShutdownSeconds + 5`) already handles the grace period correctly
  - Verify `ConfigureHostOptions` `ShutdownTimeout` value is sufficient (currently `GracefulShutdownSeconds + 5 = 30s`)
- **Acceptance Criteria:**
  - [ ] No `Thread.Sleep` in Program.cs
  - [ ] No `ProcessExit` handler in Program.cs
  - [ ] Graceful shutdown behavior preserved via host `ShutdownTimeout`
- **Effort:** XS

---

### Task 1.6 — Mark `isShuttingDown` as `volatile`
- **File:** `Explore.API/Program.cs` — line 40
- **Actions:**
  - Change `var isShuttingDown = false;` to `volatile var isShuttingDown = false;`
  - *Note: Top-level statement variables cannot use `volatile`. Move to a static field: `static volatile bool isShuttingDown = false;`*
- **Acceptance Criteria:**
  - [ ] `isShuttingDown` is readable from health check threads without stale cached value
  - [ ] No compiler warning about thread visibility
- **Effort:** XS

---

### Task 1.7 — Delete commented-out code from Program.cs
- **File:** `Explore.API/Program.cs` — lines 675–710 (`AddSwaggerDoc` block)
- **Actions:**
  - Delete the entire commented-out `AddSwaggerDoc` method
  - Delete the commented-out `app.MapGet("users/me", ...)` endpoint (lines 668–671)
  - Delete the comment `//builder.Services.AddSwaggerGen();` (line 176)
  - Delete the comment `//builder.Services.AddAuthorization();` (line 437)
- **Acceptance Criteria:**
  - [ ] No commented-out code blocks remain in Program.cs
  - [ ] Program.cs compiles and runs identically
- **Effort:** XS

---

## Phase 2 — API Contract & Error Handling (P1)
*Goal: Achieve a fully uniform RFC 7807 error contract. Every error response must be ProblemDetails, regardless of code path.*

### Task 2.1 — Add `correlationId` to ProblemDetails extensions
- **File:** `Explore.API/Extensions/ExceptionHandlingExtensions.cs`
- **Context:** `traceId` and `timestamp` are already added via `CustomizeProblemDetails`. `correlationId` (set by `CorrelationIdMiddleware` in `HttpContext.Items["CorrelationId"]`) is missing.
- **Actions:**
  - In `AddApiExceptionHandling`, extend `CustomizeProblemDetails`:
    ```csharp
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.Items["CorrelationId"] as string;
    };
    ```
- **Acceptance Criteria:**
  - [ ] Every error response includes `correlationId` field
  - [ ] `correlationId` matches the `X-Correlation-ID` response header
  - [ ] Integration test: confirm ProblemDetails body contains `correlationId` on 400/404/403/500
- **Effort:** S
- **Skills:** `error-tracking`

---

### Task 2.2 — Replace `ProblemDetails.Type` third-party URL with IANA standard URIs
- **File:** `Explore.API/ExceptionHandling/GlobalExceptionHandler.cs`
- **Actions:**
  - Replace `$"https://httpstatuses.com/{statusCode}"` with IANA RFC 9110 section URIs:
    - 400 → `"https://tools.ietf.org/html/rfc9110#section-15.5.1"`
    - 404 → `"https://tools.ietf.org/html/rfc9110#section-15.5.5"`
    - 403 → `"https://tools.ietf.org/html/rfc9110#section-15.5.4"`
    - 500 → `"https://tools.ietf.org/html/rfc9110#section-15.6.1"`
  - Create a `private static readonly` dictionary to map status code → URI (no magic strings)
- **Acceptance Criteria:**
  - [ ] No dependency on `httpstatuses.com` in any ProblemDetails response
  - [ ] All `type` values are valid, stable IANA URI references
  - [ ] No string interpolation for the type URI
- **Effort:** S

---

### Task 2.3 — Eliminate manual `NotFound(new { error })` returns from controllers
- **Scope:** All 58 controllers — audit and fix all occurrences
- **Pattern to remove:**
  ```csharp
  if (actor is null) return NotFound(new { error = "Actor not found" });
  ```
- **Correct pattern:** Handlers should throw `NotFoundException` when entity is not found; controllers should never null-check query results.
- **Actions:**
  - Search for `NotFound(new {` across all controllers — fix each occurrence
  - Verify corresponding query handlers throw `NotFoundException` on null result (not return null)
  - Verify corresponding query handlers use the pattern: `throw new NotFoundException(nameof(EntityType), id)`
  - Remove null-guard returns from `GetById`, `GetByDid`, `GetByTenant` in `ActorController`
  - Extend audit to: `EventController`, `OrganizationController`, `GroupController`, `UserController`, `NotificationController` (the five highest-traffic controllers)
- **Acceptance Criteria:**
  - [ ] Zero `NotFound(new {` patterns in any controller
  - [ ] Zero `NotFound()` (empty) returns in any controller
  - [ ] All 404 responses are RFC 7807 ProblemDetails with `type`, `title`, `detail`, `instance`, `traceId`, `correlationId`
  - [ ] Integration tests: 404 response body is valid ProblemDetails JSON
- **Effort:** L
- **Skills:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`

---

### Task 2.4 — Fix `UpdateActorCommand` direct HTTP body binding
- **File:** `Explore.API/Controllers/ActorController.cs` (and audit other controllers for the same pattern)
- **Problem:** `[FromBody] UpdateActorCommand command` exposes the Application layer command shape directly as the HTTP contract
- **Actions:**
  - Verify `UpdateActorDto` exists in `Explore.Application/DTOs/Actor/UpdateActorDto.cs` (confirmed present in git status)
  - In `ActorController.Update`, change parameter to `[FromBody] UpdateActorDto dto`
  - Map `dto` → `new UpdateActorCommand { Id = id, ActorDto = dto }` in the action body
  - Audit all other controllers for `[FromBody] [SomeName]Command command` pattern — fix each occurrence
- **Acceptance Criteria:**
  - [ ] No MediatR `*Command` class appears as `[FromBody]` parameter in any controller
  - [ ] API contract (OpenAPI schema) reflects DTO shape, not command shape
  - [ ] All existing tests still pass
- **Effort:** M
- **Skills:** `clean-architecture-rules`

---

### Task 2.5 — Remove `IHttpContextAccessor` injection from controllers
- **File:** `Explore.API/Controllers/ActorController.cs` (and audit all controllers)
- **Actions:**
  - Search all controllers for `IHttpContextAccessor` in constructor — remove every occurrence
  - Replace any `_httpContextAccessor.HttpContext` usage with `this.HttpContext` (available in ControllerBase)
  - Remove `_logger` from `ActorController` if it is genuinely unused (confirm by searching `_logger.Log` in the file)
  - Audit all 58 controllers for both patterns
- **Acceptance Criteria:**
  - [ ] Zero `IHttpContextAccessor` injections in controller constructors
  - [ ] Zero unused `_logger` fields in controllers
  - [ ] All 58 controllers compile without warnings
- **Effort:** M

---

### Task 2.6 — Resolve `ValidationBehavior` dead code
- **File:** `Explore.Application/Behaviors/ValidationBehavior.cs`, `Explore.Application/ApplicationServicesRegistration.cs`
- **Context:** `ValidationBehavior` exists but is not registered. CLAUDE.md says validators are manually instantiated. The behavior assumes DI-registered validators.
- **Decision options (choose one, document in `MAJOR_DECISIONS.md`):**
  - **Option A (Keep manual pattern):** Delete `ValidationBehavior.cs` entirely. Document that validation is enforced manually in handlers per CLAUDE.md.
  - **Option B (Enable pipeline validation):** Register `ValidationBehavior` in `ApplicationServicesRegistration` and register all validators with DI via `services.AddValidatorsFromAssembly(...)`. Update CLAUDE.md.
- **Recommended:** Option B — pipeline validation is the correct pattern for Clean Architecture with MediatR. Eliminates forgotten validations. Update CLAUDE.md rule to reflect reality.
- **Acceptance Criteria (if Option B):**
  - [ ] `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>))` added before `AuthorizationBehavior` in pipeline
  - [ ] `services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())` added
  - [ ] CLAUDE.md rule updated: validators are DI-registered, manually instantiating is removed as rule
  - [ ] All existing validators implement `AbstractValidator<T>` (no interface change)
  - [ ] All handler tests that bypass validation still work (validation only runs via MediatR pipeline, not in unit tests that call handlers directly)
- **Effort:** M
- **Skills:** `cqrs-mediatr-guidelines`

---

## Phase 3 — Performance & Observability (P2)
*Goal: Eliminate per-request memory allocations, GC pressure, and log noise at scale.*

### Task 3.1 — Fix ETagMiddleware: replace MemoryStream with RecyclableMemoryStream
- **File:** `Explore.API/Middleware/ETagMiddleware.cs`
- **Package:** `Microsoft.IO.RecyclableMemoryStream` — add to `Explore.API.csproj`
- **Actions:**
  - Register a singleton `RecyclableMemoryStreamManager` in `Program.cs` or in a DI extension
  - Inject `RecyclableMemoryStreamManager` into `ETagMiddleware` constructor
  - Replace `using var bufferStream = new MemoryStream()` with `using var bufferStream = _streamManager.GetStream("etag-middleware")`
  - Replace `SHA256.HashData(bufferStream.ToArray())` with span-based hashing that reads from the stream without `.ToArray()`:
    ```csharp
    bufferStream.Position = 0;
    var hash = SHA256.HashData(bufferStream.GetReadOnlySequence()); // or read via spans
    ```
    *If `GetReadOnlySequence()` is not available, use: `ReadOnlySpan<byte> buffer = bufferStream.ToArray()` only after confirming the stream's actual length is reasonable (e.g., < 64KB for ETags; skip ETag computation for large responses)*
  - Add a size check: skip ETag computation for responses > 256KB (these should be handled by output cache alone)
  - Skip ETag computation when `HttpContext.Items["OutputCacheHit"] == true` (coordinate with output cache)
- **Acceptance Criteria:**
  - [ ] `ETagMiddleware` uses `RecyclableMemoryStreamManager` — no `new MemoryStream()` allocation
  - [ ] No `.ToArray()` that creates a second copy of the body
  - [ ] Responses > 256KB skip ETag computation (no buffering)
  - [ ] Unit test: inject mock stream manager, verify `GetStream()` is called instead of `new MemoryStream()`
- **Effort:** M
- **Skills:** `error-tracking`

---

### Task 3.2 — Cache reflection result in `AuthorizationBehavior`
- **File:** `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- **Actions:**
  - Add a static `ConcurrentDictionary<Type, AuthorizeResourceAttribute?>` field
  - Replace `typeof(TRequest).GetCustomAttribute<AuthorizeResourceAttribute>()` with:
    ```csharp
    private static readonly ConcurrentDictionary<Type, AuthorizeResourceAttribute?> _attributeCache = new();

    var attribute = _attributeCache.GetOrAdd(
        typeof(TRequest),
        static t => t.GetCustomAttribute<AuthorizeResourceAttribute>());
    ```
- **Acceptance Criteria:**
  - [ ] `GetCustomAttribute` is called at most once per unique `TRequest` type across the application lifetime
  - [ ] Benchmark test (or assertion): second call for same type does not invoke reflection
  - [ ] No behavior change in authorization decisions
- **Effort:** S
- **Skills:** `cqrs-mediatr-guidelines`

---

### Task 3.3 — Set authorization allow-decisions to `Debug` log level
- **Files:**
  - `Explore.Application/Behaviors/AuthorizationBehavior.cs` (line 107)
  - `Explore.Infrastructure/Services/FallbackAuthorizationService.cs` (LogDecision method, line 693)
- **Actions:**
  - In `AuthorizationBehavior.LogInformation("Authorization decision: allow ...")` → change to `LogDebug`
  - In `FallbackAuthorizationService.LogDecision`:
    - When `decision == "allow"` → `_logger.LogDebug(...)`
    - When `decision == "deny"` → `_logger.LogWarning(...)` (keep current behavior)
    - When `SafeMode == true` → `_logger.LogCritical(...)` (elevated — safe mode is an incident)
- **Acceptance Criteria:**
  - [ ] In production log output (log level = Information), zero authorization "allow" entries appear
  - [ ] Authorization "deny" entries still appear as Warning
  - [ ] Log volume reduces by ~90% on list endpoints that evaluate HATEOAS permissions
- **Effort:** S

---

### Task 3.4 — Fix output cache: add `Authorization` header variance
- **File:** `Explore.API/Program.cs` — output cache policy configuration (lines 101–119)
- **Actions:**
  - Add `.SetVaryByHeader("Authorization")` to both `ListData` and `DetailData` policies
  - Create a separate `PublicData` policy (no auth vary) for endpoints that are truly identical for all callers (lookup tables: categories, tags, languages, etc.)
  - Audit controllers: apply `[OutputCache(PolicyName = "PublicData")]` on `[AllowAnonymous]` lookup controllers that serve the same data regardless of auth state
- **Acceptance Criteria:**
  - [ ] Authenticated and anonymous callers of the same URL receive separately cached entries for `ListData` and `DetailData`
  - [ ] HATEOAS links in cached responses correctly reflect the caller's permission set
  - [ ] Lookup table endpoints (CategoryController, TagController, LanguageController, etc.) use `PublicData` policy for optimal cache hit rates
- **Effort:** M
- **Skills:** `auth-patterns`

---

### Task 3.5 — Fix CORS `InternalWebsitePolicy` hardcoded origin
- **File:** `Explore.API/Program.cs` — CORS configuration (lines 223–228)
- **Actions:**
  - Replace hardcoded `"https://iloveibadah.app"` with `corsAllowedOrigins` (already loaded from `Cors:AllowedOrigins` config)
  - If a specific "website" origin is needed as distinct from "app" origins, add a `Cors:WebsiteOrigins` configuration key
- **Acceptance Criteria:**
  - [ ] No hardcoded domain strings in any CORS policy
  - [ ] All policies derive origins from configuration
- **Effort:** XS

---

### Task 3.6 — Fix `ISetupSecretProvider`: add `GetSecretForLogging()` to interface
- **Files:**
  - `Explore.Application/Contracts/Services/ISetupSecretProvider.cs`
  - `Explore.Infrastructure/Services/SetupSecretProvider.cs`
  - `Explore.API/Program.cs` (line 561)
- **Actions:**
  - Add method to interface: `string? GetSecretForLogging();`
  - Add XML doc: explains this returns the auto-generated secret (null if from env var) — only used for bootstrap banner
  - Implement in `SetupSecretProvider` (already implemented as the concrete method being cast to)
  - In `Program.cs`, replace `((SetupSecretProvider)setupSecretProvider).GetSecretForLogging()` with `setupSecretProvider.GetSecretForLogging()`
- **Acceptance Criteria:**
  - [ ] No concrete type cast in `Program.cs`
  - [ ] Interface method is documented
  - [ ] Unit test: mock `ISetupSecretProvider` in tests that need to simulate setup mode without the concrete class
- **Effort:** S
- **Skills:** `clean-architecture-rules`

---

### Task 3.7 — Add structured log event for setup mode activation
- **File:** `Explore.API/Program.cs` — setup secret bootstrap section (lines 547–594)
- **Actions:**
  - Replace `Console.WriteLine("+=====...")` banner with Serilog structured logging at `Warning` level:
    ```csharp
    app.Logger.LogWarning(
        "[SetupMode] Instance is unclaimed. Auto-generated setup secret active. " +
        "Visit /setup to claim. Secret: {SetupSecret}",
        setupSecretForStartupReminder);
    ```
  - Keep one `Console.WriteLine` for the secret value itself (for terminal readability when SSH'd into a container), but remove the ASCII art banner format
  - The `app.Lifetime.ApplicationStarted` reminder can remain as a simpler `Console.WriteLine` — this is the established bootstrap pattern in `InfisicalConfigurationProvider.cs`
- **Acceptance Criteria:**
  - [ ] Setup mode activation produces a structured `Warning` log with `SetupSecret` as a named property
  - [ ] Log aggregators can alert on this event by filtering `[SetupMode]`
  - [ ] Console output is still visible for manual SSH scenarios
- **Effort:** S

---

### Task 3.8 — Add OpenTelemetry activity spans for authorization decisions
- **File:** `Explore.Application/Behaviors/AuthorizationBehavior.cs`
- **Actions:**
  - Add `using System.Diagnostics;` (already imported)
  - Wrap `EnforceAuthorizationAsync` call in an `ActivitySource` span:
    ```csharp
    private static readonly ActivitySource _activitySource = new("Explore.Authorization");

    using var activity = _activitySource.StartActivity("authorization.evaluate");
    activity?.SetTag("resource.kind", resourceKind);
    activity?.SetTag("resource.action", action);
    activity?.SetTag("request.type", requestType);
    ```
  - Set span status to `Error` on deny decision
  - Register `_activitySource` with OpenTelemetry in `ApplicationServicesRegistration` (via `AddSource("Explore.Authorization")`)
- **Acceptance Criteria:**
  - [ ] Authorization decisions appear as child spans in distributed traces
  - [ ] Span contains `resource.kind` and `resource.action` tags
  - [ ] Denied decisions set span status to `ActivityStatusCode.Error`
- **Effort:** M
- **Skills:** `error-tracking`

---

### Task 3.9 — Lock `FallbackAuthorizationService.SafeMode` behind an activation method
- **File:** `Explore.Infrastructure/Services/FallbackAuthorizationService.cs`
- **Actions:**
  - Change `public bool SafeMode { get; set; }` to `public bool SafeMode { get; private set; }`
  - Add public method: `public void ActivateSafeMode()` — sets the flag and logs a critical event
  - Update all callers that set `SafeMode = true` to call `ActivateSafeMode()` instead
  - Add `private bool _safeModeLogged` guard so the critical log fires only once
- **Acceptance Criteria:**
  - [ ] `SafeMode` cannot be set to `false` via the property from outside the class
  - [ ] `ActivateSafeMode()` logs at `Critical` level with resource context
  - [ ] No external code can deactivate safe mode (intentional — only instance restart resets it)
- **Effort:** S

---

## Phase 4 — Architecture & Infrastructure (P3)
*Goal: Add the missing infrastructure layer required for ATProto PDS, webhooks, and API key clients.*

### Task 4.1 — Implement `Idempotency-Key` middleware for write operations
*This is the most significant task in the plan — required for ATProto PDS at-least-once delivery.*

#### 4.1.1 — Domain: `IdempotencyRecord` entity
- **File:** `Explore.Domain/IdempotencyRecord.cs`
- **Actions:**
  - Create entity with: `Guid Id` (UUIDv7), `string Key`, `Guid? TenantId`, `string? UserId`, `int StatusCode`, `string? ResponseBody`, `string? ContentType`, `DateTime CreatedAt`, `DateTime ExpiresAt`
  - Entity is immutable after creation (no `Update` method)
  - Expiration: 24 hours default (configurable)
- **Acceptance Criteria:**
  - [ ] Entity follows auditing pattern (CreatedAt, CreatedBy)
  - [ ] No default values in domain entity per CLAUDE.md
  - [ ] Entity has no navigation properties

#### 4.1.2 — Persistence: `IdempotencyRecordConfiguration` + `IIdempotencyRepository`
- **Files:**
  - `Explore.Persistence/Configurations/Entities/IdempotencyRecordConfiguration.cs`
  - `Explore.Application/Contracts/Persistence/IIdempotencyRepository.cs`
  - `Explore.Persistence/Repositories/IdempotencyRepository.cs`
  - EF Core migration: `AddIdempotencyRecords`
- **Actions:**
  - Configure unique index on `(Key, TenantId)` — same key can be reused across tenants
  - Configure soft-expiry query: `WHERE ExpiresAt > NOW()`
  - Add `DbSet<IdempotencyRecord>` to `ExploreDbContext`
  - Interface methods: `Task<IdempotencyRecord?> FindAsync(string key, Guid tenantId)`, `Task SaveAsync(IdempotencyRecord record)`
  - Apply named soft-delete filter: `HasQueryFilter(name: "NotExpired", predicate: r => r.ExpiresAt > DateTime.UtcNow)`

#### 4.1.3 — API: `IdempotencyMiddleware`
- **File:** `Explore.API/Middleware/IdempotencyMiddleware.cs`
- **Actions:**
  - Read `Idempotency-Key` header (skip if missing — idempotency is opt-in per request)
  - Validate key format (max 128 chars, no whitespace)
  - Check `IIdempotencyRepository` for existing record with matching key + tenant
  - If found and status 2xx: return cached response body with original status code + `X-Idempotency-Replay: true` header
  - If found and status non-2xx: return the error response as-is
  - If not found: capture response via body-wrapping (similar to ETagMiddleware, use `RecyclableMemoryStreamManager`), persist after successful completion
  - Only applies to `POST`, `PUT`, `PATCH`, `DELETE` methods (not `GET`/`HEAD`)
  - Register in middleware pipeline after `UseAuthentication` (requires user context for tenant scoping)
- **Acceptance Criteria:**
  - [ ] Same `Idempotency-Key` + `POST` returns identical response on replay
  - [ ] Replay response includes `X-Idempotency-Replay: true` header
  - [ ] Keys expire after 24 hours
  - [ ] Missing key header → normal processing (no error)
  - [ ] Invalid key format → 400 Bad Request
  - [ ] Integration tests: create + replay → same 201 response
- **Effort:** L
- **Skills:** `clean-architecture-rules`, `dotnet-efcore-guidelines`

---

### Task 4.2 — Add URL versioning alongside media-type versioning
- **File:** `Explore.API/Extensions/ApiVersioningExtensions.cs`
- **Actions:**
  - Add `UrlSegmentApiVersionReader` as a secondary reader alongside the existing `MediaTypeApiVersionReader`
  - Use `ApiVersionReader.Combine(new MediaTypeApiVersionReader("v"), new UrlSegmentApiVersionReader())`
  - Add a second `[Route]` attribute to all controllers: `[Route("api/v{version:apiVersion}/[controller]")]`
  - Alternatively: use a convention to apply the versioned route globally without modifying 58 controller files
- **Recommended approach:** Add versioned route as a `IApplicationModelConvention` so no controller files need modification:
  ```csharp
  services.AddControllers(options =>
      options.Conventions.Add(new RouteVersionConvention()));
  ```
- **Acceptance Criteria:**
  - [ ] `GET /api/v0.1/actor` returns same result as `GET /api/actor` with `Accept: application/json;v=0.1`
  - [ ] Existing media-type versioning still works (no breaking change)
  - [ ] OpenAPI spec documents both routes
  - [ ] No changes required in 58 controller files (use convention)
- **Effort:** M
- **Skills:** `clean-architecture-rules`

---

### Task 4.3 — Migrate from Swashbuckle to native ASP.NET Core OpenAPI + Scalar
- **Files:**
  - `Explore.API/Program.cs`
  - `Explore.API/Extensions/ServiceCollectionExtensions.cs`
  - `Explore.API/Extensions/ExceptionHandlingExtensions.cs`
  - `Explore.API.csproj` (remove Swashbuckle packages)
  - `Explore.API/BackgroundServices/OpenApiExportService.cs` (update endpoint path)
- **Actions:**
  - Remove `builder.Services.AddSwaggerGenWithAuth(...)` call from Program.cs
  - Remove `app.UseSwagger()` and `app.UseSwaggerUI(...)` calls
  - Keep and enhance `builder.Services.AddOpenApi("event-api", ...)` which is already configured
  - Keep `app.MapScalarApiReference()` — this becomes the sole documentation UI
  - Update `OpenApiExportService` to export from `/openapi/event-api.json` instead of `/swagger/v0.1/swagger.json`
  - Remove Swashbuckle NuGet packages: `Swashbuckle.AspNetCore`, `Swashbuckle.AspNetCore.Filters`
  - Keep `HALDtoSchemaTransformer` (it works with native OpenAPI)
  - Update `swagger.json` export to `openapi.json` in `Static/` or `wwwroot/`
- **Acceptance Criteria:**
  - [ ] `/openapi/event-api.json` serves valid OpenAPI 3.1 document
  - [ ] Scalar UI at `/scalar` serves complete, interactive API documentation
  - [ ] No Swashbuckle packages in `Explore.API.csproj`
  - [ ] All 100+ endpoints documented in the single OpenAPI document
  - [ ] All existing Blazor client API generation (if any) still works with new endpoint path
- **Effort:** M

---

### Task 4.4 — Add `GetActorsByTenant` pagination parameters
- **File:** `Explore.API/Controllers/ActorController.cs`
- **Actions:**
  - Add `int pageNumber = 1` and `int pageSize = 20` parameters to `GetByTenant`
  - Pass to `GetActorsByTenantRequest`
  - Verify `GetActorsByTenantRequest` handler supports pagination (add if missing)
- **Acceptance Criteria:**
  - [ ] `GET /api/actor/by-tenant/{tenantId}?pageNumber=2&pageSize=50` works
  - [ ] Response is `HalCollectionResource<ActorListDto>` with pagination metadata
  - [ ] Missing parameters default to `pageNumber=1, pageSize=20`
- **Effort:** S

---

### Task 4.5 — Add ABOUTME comments to all controller files missing them
- **Scope:** All 58 controllers in `Explore.API/Controllers/`
- **Actions:**
  - Audit each controller file for `// ABOUTME:` on lines 1–2
  - Add two-line ABOUTME comment to any missing it
  - Example: `// ABOUTME: REST controller for Actor (user/organization profile) management.`
    `// ABOUTME: Delegates all business logic to MediatR handlers; returns HAL resources.`
- **Acceptance Criteria:**
  - [ ] Every `.cs` file in `Controllers/` starts with `// ABOUTME:` on line 1
- **Effort:** S

---

## Phase 5 — Testing & Documentation

### Task 5.1 — Integration tests for unified error contract
- **Project:** `Event.API.IntegrationTests`
- **Tests to add:**
  - `GET /api/actor/{unknown-guid}` → 404 ProblemDetails with `type`, `title`, `detail`, `instance`, `traceId`, `correlationId`
  - `POST /api/actor` with invalid body → 400 ProblemDetails with `errors` dictionary
  - `PUT /api/actor/{id}` without auth → 401 (standard WWW-Authenticate challenge)
  - `PUT /api/actor/{id}` with insufficient permissions → 403 ProblemDetails
  - `POST /api/actor` (write) with `Idempotency-Key: test-key-123` → 201; replay with same key → 201 with `X-Idempotency-Replay: true`
- **Acceptance Criteria:**
  - [ ] All five test scenarios pass
  - [ ] No anonymous object error shapes in any test response
- **Effort:** M

---

### Task 5.2 — Update `docs/API.md` to reflect all changes
- **File:** `docs/API.md`
- **Sections to update:**
  - Add `Idempotency-Key` section
  - Update middleware pipeline order (Phase 4.1.3 inserts new middleware)
  - Update versioning section (URL versioning added)
  - Update caching section (new `PublicData` policy, auth-vary on ListData/DetailData)
  - Update error handling section (correlationId in ProblemDetails)
  - Update documentation section (Swagger removed, Scalar only)
- **Effort:** S

---

### Task 5.3 — Update `CLAUDE.md` per decisions made
- **File:** `CLAUDE.md`
- **Sections to update:**
  - If ValidationBehavior Option B chosen: update rule "Validators are manually instantiated (no DI)" → "Validators are DI-registered via `AddValidatorsFromAssembly`; ValidationBehavior runs them as a pipeline step"
- **Effort:** XS

---

### Task 5.4 — Record decisions in `dev/_journal/MAJOR_DECISIONS.md`
- **File:** `dev/_journal/MAJOR_DECISIONS.md`
- **Decisions to record:**
  - ValidationBehavior: Option A (delete) or Option B (enable) — rationale
  - Idempotency store: database-backed (chosen over Redis for auditability)
  - URL versioning via controller convention (not individual controller attributes)
  - Swashbuckle removal and migration to native OpenAPI
- **Effort:** XS

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `Thread.Sleep` removal changes graceful shutdown timing | Low | Medium | Verify `ShutdownTimeout` is set correctly; test SIGTERM behavior in Docker |
| Option B (ValidationBehavior) causes double validation in handlers that also call validators manually | Medium | Medium | Audit all handlers for manual validation calls before enabling the behavior |
| ETag `RecyclableMemoryStream` changes SHA256 computation on stream not-fully-read | Low | High | Unit test hash output matches original implementation |
| Output cache `SetVaryByHeader("Authorization")` reduces cache hit rates significantly | High | Low | Expected trade-off — only affects auth-aware endpoints, not lookup tables |
| Idempotency table migration fails on large `OutboxMessage` tables | Low | Medium | Run migration as separate deployment step; add `NOT NULL DEFAULT` carefully |
| Swashbuckle removal breaks any Blazor client code generation that reads `/swagger/v0.1/swagger.json` | Medium | High | Verify `EventApiClient.g.cs` generation source; update NSwag/Kiota config if needed |
| URL versioning convention modifies routes for all 58 controllers simultaneously | Medium | High | Test in staging first; verify no consumer depends on exact route format |

---

## Success Metrics

| Metric | Current | Target |
|--------|---------|--------|
| PII in application logs | JWT claims on every request | Zero claim values in logs |
| Error response consistency | ~60% ProblemDetails | 100% ProblemDetails |
| GC pressure (ETag) | MemoryStream + ToArray per GET | RecyclableMemoryStream, no ToArray |
| Authorization log volume | Information on every allow | Debug only; Warning on deny |
| Sync blocking calls at startup | 2 (Migrate, SeedAsync) | 0 |
| Dead code files | 1 (ValidationBehavior if unused) | 0 |
| Idempotency support | None | Full (POST/PUT/PATCH/DELETE) |
| API documentation systems | 2 (Swagger + native OpenAPI) | 1 (native OpenAPI + Scalar) |

---

## Effort Summary

| Phase | Tasks | Total Effort | Duration |
|-------|-------|-------------|----------|
| Phase 1 — Security Critical | 7 tasks | 2S + 4XS + 1S | 0.5 day |
| Phase 2 — API Contract | 6 tasks | 1S + 1S + 1L + 1M + 1M + 1M | 1.5 days |
| Phase 3 — Performance/Observability | 9 tasks | 1M + 1S + 1S + 1M + 1XS + 1S + 1S + 1M + 1S | 1.5 days |
| Phase 4 — Architecture | 5 tasks | 1L + 1M + 1M + 1S + 1S | 2 days |
| Phase 5 — Testing/Docs | 4 tasks | 1M + 1S + 1XS + 1XS | 0.5 day |
| **Total** | **32 tasks** | | **~6 working days** |

---

## Potential Risks & Unknowns

The single highest-risk item is **Task 2.6 (ValidationBehavior decision)**. If Option B is chosen (enable pipeline validation), there is a real risk that existing command handlers manually invoke validators inside their `Handle` method — meaning validation would run twice per request after the behavior is enabled. Every handler that calls `new SomeValidator().Validate(dto)` manually must be audited and the manual call removed before enabling the behavior. Given there are 617 handlers, this audit could uncover a significant number of touch points, making Option B a larger effort than it appears.

The second highest-risk item is **Task 4.3 (Swashbuckle migration)**. The `Explore.Blazor.Client/Clients/EventApiClient.g.cs` file (shown as modified in git status) is likely auto-generated from the OpenAPI spec — if its generator (NSwag or Kiota) is configured to read from `/swagger/v0.1/swagger.json`, the generation pipeline will break silently after Swashbuckle is removed. Verify the generation source before proceeding.
