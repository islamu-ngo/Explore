# Setup Secret Bootstrap Mode - Implementation Plan (v2)

> **v2**: Eliminated Discovery Mode per user feedback. Secret is ALWAYS required.
> Research sources: Keycloak bootstrap-admin pattern, Langfuse/Infisical self-hosted onboarding,
> ASP.NET Core rate limiting docs, .NET CryptographicOperations API, TypeFilterAttribute pattern.

## Executive Summary

Add an **always-required** `SETUP_SECRET` guard to the existing instance onboarding flow. There is no "open/discovery" mode — the secret is mandatory whether set via environment variable or auto-generated at boot.

- If `SETUP_SECRET` env var is set -> that value is used
- If `SETUP_SECRET` env var is missing -> API auto-generates a 32-char cryptographically random token via `RandomNumberGenerator` and logs it prominently to STDOUT
- Users can shut down, set the env var, restart, and the secret persists
- Without the env var, a new random secret is generated each restart

This follows the same pattern as Keycloak's `bootstrap-admin` command (secret required, can come from env var) and Langfuse's `NEXTAUTH_SECRET` (must be set, with generation instructions).

The codebase already has ~90% of the infrastructure. This feature adds the missing pre-authentication guard layer.

## Current State

### What Already Exists

| Component | Purpose | Status |
|-----------|---------|--------|
| `InstanceBootstrapState` entity | Tracks first-run completion (IsCompleted, CompletedByUserId, CreatedAt) | Working |
| `InstanceAdministrator` entity | Maps users to instance admin role (UserId, GrantedAt, GrantedBy) | Working |
| `InstanceOnboardingController` | API endpoints: GET /status, POST /complete, GET/PUT /settings, /storage-settings | Working |
| `CompleteInstanceOnboardingCommandHandler` | Creates admin + tenant + settings. Prevents double-completion. | Working |
| `IInstanceOnboardingService` | Blazor client calling onboarding API | Working |
| Root `/` redirect to `/startup` | Startup gate in Blazor middleware (Program.cs lines 517-527) | Working |
| `HasAnyInstanceAdministrator()` | Repository method to check admin existence | Working |
| `GetCurrent()` | Bootstrap state lookup | Working |

### Current Flow (No Setup Secret)

```
User visits / -> redirect /startup -> Blazor checks InstanceOnboarding/status
  -> Not completed? Show onboarding UI
  -> User must login via Keycloak ([Authorize] on /complete)
  -> First logged-in user to hit "Complete" becomes instance admin
  -> Once completed, /complete returns "already completed"
  -> /settings and /storage-settings return 403 to non-admins
```

## Proposed Future State

### Two-Mode Bootstrap (Always-Required Secret)

| Condition | Mode | Behavior |
|-----------|------|----------|
| `InstanceBootstrapState.IsCompleted == false` | **Setup Mode** | Secret ALWAYS required. API validates `X-Setup-Secret` header. Blazor shows secret input page. |
| `InstanceBootstrapState.IsCompleted == true` | **Locked Mode** | All setup-specific write endpoints return 410 Gone. `/setup` redirects to `/`. |

### Secret Resolution (Startup)

| Condition | Behavior |
|-----------|----------|
| `SETUP_SECRET` env var is set | Use that value. Log: `SETUP_SECRET loaded from environment variable.` |
| `SETUP_SECRET` env var is missing | Generate 32-char crypto-random token via `RandomNumberGenerator`. Log formatted box with the secret. |

### Enhanced Flow

```
Boot: API starts -> Resolve ISetupSecretProvider (Singleton)
  -> Check SETUP_SECRET env var
  -> Present? Use it. Log confirmation.
  -> Missing? Generate 32-char random token. Log:

     +==============================================================+
     |  SETUP SECRET (auto-generated, not persisted across          |
     |  restarts unless you set the SETUP_SECRET env var):          |
     |                                                              |
     |  aB3xK9mQ7nR2wT5yH8jL4pF6vD1cG0s                           |
     |                                                              |
     |  Use this at /setup to claim this instance.                  |
     +==============================================================+

User visits / -> redirect /setup
  -> Blazor calls GET /api/v1/InstanceOnboarding/status
  -> Response: { isCompleted: false, isSetupModeActive: true, setupSecretFromEnvironment: false }
  -> Blazor shows "Enter setup secret" form
  -> Help text: "Check the SETUP_SECRET env var, or check the API server logs."
  -> User enters secret -> POST /bff/validate-setup-secret
  -> Valid? Store in sessionStorage (persists across Keycloak OIDC redirect)
  -> Show "Login to continue" button
  -> User clicks Login -> Keycloak OIDC redirect
  -> User returns authenticated (sessionStorage still has secret)
  -> Blazor forwards X-Setup-Secret header via YARP on API calls
  -> User completes onboarding -> SetupSecretProvider.Lock() called
  -> Audit log: "Instance claimed by admin (userId: X) from IP: Y. Bootstrap mode disabled."
  -> /setup now redirects to / (Locked Mode)
```

---

## Implementation Phases

### Phase 1: Core Infrastructure - SetupSecretProvider (1.5 hours)

Create the singleton service that manages the setup secret lifecycle.

#### Task 1.1: Create `ISetupSecretProvider` Interface

**Files:**
- `Explore.Application/Contracts/Services/ISetupSecretProvider.cs` - NEW

**Interface:**
```csharp
// ABOUTME: Interface for the setup secret provider that gates instance onboarding.
// ABOUTME: Implementation generates or reads secret, validates with timing-safe comparison, and locks after completion.

namespace Explore.Application.Contracts.Services;

public interface ISetupSecretProvider
{
    bool IsSetupModeActive { get; }
    bool IsFromEnvironmentVariable { get; }
    bool IsTimedOut { get; }
    DateTime InstanceStartedAt { get; }
    bool ValidateSecret(string? secret);
    void Lock();
}
```

**Design decisions:**
- Interface in Application layer (Clean Architecture — no infrastructure dependency)
- `ValidateSecret` uses `CryptographicOperations.FixedTimeEquals` internally (timing-safe, prevents side-channel attacks)
- `Lock()` called after successful onboarding completion — transitions to Locked Mode
- `IsFromEnvironmentVariable` tells the UI whether to say "check env var" or "check API logs"

**Acceptance:** Interface compiles, follows Clean Architecture dependency rules.

#### Task 1.2: Create `SetupSecretProvider` Implementation (Singleton)

**Files:**
- `Explore.Infrastructure/Services/SetupSecretProvider.cs` - NEW

**Logic:**
```csharp
// Registered as Singleton
// Constructor:
//   1. Capture _instanceStartedAt = DateTime.UtcNow (boot timestamp for timeout enforcement)
//   2. Read SETUP_SECRET from IConfiguration (covers env vars, appsettings, etc.)
//   3. If present -> use it, set IsFromEnvironmentVariable = true
//   4. If missing -> generate 32-char crypto-random token:
//      var bytes = RandomNumberGenerator.GetBytes(24); // 24 bytes = 32 base64 chars
//      _secret = Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
//   5. Lazy bootstrap check: first call to IsSetupModeActive checks DB via IServiceProvider
//
// ValidateSecret(string? secret):
//   // REFINEMENT 1: Enforce 60-minute timeout IN the provider, not just the UI.
//   // This prevents API-level bypass by malicious actors who skip the UI.
//   if (IsTimedOut) return false;
//   if (secret is null) return false;
//   var expected = Encoding.UTF8.GetBytes(_secret);
//   var actual = Encoding.UTF8.GetBytes(secret);
//   return CryptographicOperations.FixedTimeEquals(expected, actual);
//   // FixedTimeEquals prevents timing attacks by always comparing full length
//
// Lock():
//   Set internal _isLocked = true (prevents further secret validation)
//
// New properties:
//   DateTime InstanceStartedAt { get; }  // Captured at construction = boot time
//   bool IsTimedOut { get; }             // DateTime.UtcNow - InstanceStartedAt > 60 min
```

**Key design: Lazy bootstrap state check**
The singleton can't inject scoped `IInstanceBootstrapStateRepository` directly. Instead, inject `IServiceProvider` and create a scope on first `IsSetupModeActive` access to check if bootstrap is already complete. Cache the result. Pattern follows `PdsSyncWorker` (uses `IServiceProvider.CreateAsyncScope()`) and `RuntimeAuthorizationProvider` (uses `IMemoryCache` with 5-min expiry for lazy DB lookups).

**Key design: Timeout enforcement at provider level (Refinement 1)**
The 60-minute timeout is enforced inside `ValidateSecret()`, NOT just in the UI/query handler. This is critical because a malicious actor could bypass the Blazor UI and call the API directly. If the timeout only lived in the query handler's DTO flag, the actual `SetupSecretRequiredAttribute` filter would still accept the correct secret after 60 minutes. By enforcing in the provider, the filter inherits the timeout protection automatically.

The boot timestamp (`_instanceStartedAt`) is captured in the constructor as `DateTime.UtcNow`. This is more reliable than reading from the database — the `InstanceBootstrapState.CreatedAt` is set when onboarding COMPLETES, not at boot time. The codebase uses `DateTime.UtcNow` throughout (69 occurrences found) — consistent with existing patterns.

**Reference:** `RandomNumberGenerator.GetBytes(int)` is the recommended .NET API for cryptographic randomness. `CryptographicOperations.FixedTimeEquals` prevents timing side-channel attacks on secret comparison.

**Acceptance:** Generates random secret when env var missing. Timing-safe comparison works. Lock() disables setup mode. ValidateSecret returns false after 60 minutes.

#### Task 1.3: Register SetupSecretProvider as Singleton

**Files:**
- `Explore.Infrastructure/InfrastructureServicesRegistration.cs`

```csharp
services.AddSingleton<ISetupSecretProvider, SetupSecretProvider>();
```

**Acceptance:** `ISetupSecretProvider` resolves from DI as singleton.

#### Task 1.4: Startup Logging (API)

**Files:**
- `Explore.API/Program.cs`

After building the host but before `app.Run()` (placement: after database migration block, ~line 448), resolve `ISetupSecretProvider` and log.

**REFINEMENT 2: Logging Visibility**
Use `Console.WriteLine` for the secret itself (guaranteed visibility in ALL environments including Kubernetes where log levels may be filtered). This matches the established Infisical bootstrap pattern in the codebase (`InfisicalConfigurationProvider.cs` lines 60-123 uses `Console.WriteLine` extensively for bootstrap output). Use `LogWarning` for structured context.

```csharp
var setupSecretProvider = app.Services.GetRequiredService<ISetupSecretProvider>();
if (setupSecretProvider.IsFromEnvironmentVariable)
{
    app.Logger.LogInformation("[SetupSecret] SETUP_SECRET loaded from environment variable.");
}
else if (setupSecretProvider.IsSetupModeActive)
{
    app.Logger.LogWarning("[SetupSecret] No SETUP_SECRET env var found. Auto-generated secret for bootstrap.");
    // Console.WriteLine guarantees visibility even with aggressive log filtering
    Console.WriteLine("+=============================================================+");
    Console.WriteLine("| SETUP SECRET (auto-generated, not persisted across restarts |");
    Console.WriteLine("| unless you set the SETUP_SECRET environment variable):      |");
    Console.WriteLine("|                                                             |");
    Console.WriteLine($"|  {setupSecretProvider.GetSecretForLogging()}");
    Console.WriteLine("|                                                             |");
    Console.WriteLine("| Use this at /setup to claim this instance.                  |");
    Console.WriteLine("+=============================================================+");
}
```

Note: `GetSecretForLogging()` is an internal method ONLY called during startup — never exposed via API. The interface does NOT expose the raw secret.

**Acceptance:** Auto-generated secret visible via `Console.WriteLine` in all environments. Env var mode shows confirmation via `LogInformation`.

#### Task 1.5: Startup Logging (Blazor BFF)

**Files:**
- `Explore.Blazor/Program.cs`

Same `Console.WriteLine` pattern as API (defense in depth — both need the secret for their own validation). Placed after `var app = builder.Build();` (~line 395).

**Acceptance:** Blazor console shows appropriate log with same visibility guarantees.

#### Task 1.6: Update InstanceOnboardingStatusDto

**Files:**
- `Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs`
- `Explore.Application/Serialization/ExploreJsonContext.cs` (verify AOT compat)
- `Explore.Blazor.Client/Serialization/AppJsonSerializerContext.cs` (verify AOT compat)

**New fields:**
```csharp
public bool IsSetupModeActive { get; set; }        // true when setup not complete and not locked
public bool SetupSecretFromEnvironment { get; set; } // true = "check env var", false = "check API logs"
public DateTime? InstanceStartedAt { get; set; }     // for proximity timer + system info display
```

**Acceptance:** DTO compiles, serialization contexts updated, no breaking changes.

---

### Phase 2: API Layer - Secret Validation & Rate Limiting (1.5 hours)

#### Task 2.1: Create `SetupSecretRequiredAttribute` Action Filter

**Files:**
- `Explore.API/Filters/SetupSecretRequiredAttribute.cs` - NEW

**Pattern:** `TypeFilterAttribute` with inner `IAsyncActionFilter` (ASP.NET Core pattern for DI-aware attribute filters):
```csharp
// Outer attribute (TypeFilterAttribute) enables DI resolution
// Inner filter (IAsyncActionFilter) contains the logic:
// 1. Resolve ISetupSecretProvider from services
// 2. If !IsSetupModeActive -> return 410 Gone { error: "Setup already completed" }
// 3. Read X-Setup-Secret header from request
// 4. If !ValidateSecret(header) -> return 403 Forbidden { error: "Invalid setup secret" }
// 5. Pass through to action
```

**Acceptance:** Filter compiles, can be applied as `[SetupSecretRequired]` attribute.

#### Task 2.2: Register Rate Limiter Policy + Apply to Endpoints

**Files:**
- `Explore.API/Program.cs` - Register rate limiter
- `Explore.API/Controllers/InstanceOnboardingController.cs` - Apply attributes

**Rate Limiting (per ASP.NET Core docs):**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("SetupSecret", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});
// ... later:
app.UseRateLimiter(); // after UseRouting, before UseAuthorization
```

**Apply to write endpoints:**
```csharp
[SetupSecretRequired]
[EnableRateLimiting("SetupSecret")]
[HttpPost("complete")]
[Authorize]
public async Task<ActionResult<BaseCommandResponse<Guid>>> Complete(...)
```

**Apply to:** POST /complete, PUT /settings, PUT /storage-settings, POST /test-storage
**Do NOT apply to:** GET /status, GET /settings (read endpoints remain open)

**Acceptance:** Write endpoints return 403 on wrong secret, 410 when locked, 429 after 5 attempts/min.

#### Task 2.3: Update GetInstanceOnboardingStatusQueryHandler

**Files:**
- `Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`

Inject `ISetupSecretProvider`, populate new DTO fields:
```csharp
dto.IsSetupModeActive = _setupSecretProvider.IsSetupModeActive;
dto.SetupSecretFromEnvironment = _setupSecretProvider.IsFromEnvironmentVariable;
dto.InstanceStartedAt = bootstrapState?.CreatedAt;
```

**Acceptance:** GET /status returns new fields correctly.

#### Task 2.4: Audit Logging on Instance Claim

**Files:**
- `Explore.API/Controllers/InstanceOnboardingController.cs`

In the Complete action, after successful MediatR call:
```csharp
_logger.LogWarning(
    "Instance claimed by admin (userId: {UserId}) from IP: {IpAddress}. Bootstrap mode disabled.",
    userId, HttpContext.Connection.RemoteIpAddress);
```

**Acceptance:** Audit log entry appears with userId + IP at Warning level.

#### Task 2.5: Call Lock() After Completion

**Files:**
- `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`

Inject `ISetupSecretProvider`, call `Lock()` after successful completion to transition singleton to locked state.

**Acceptance:** SetupSecretProvider transitions to locked; subsequent requests return 410.

---

### Phase 3: Blazor BFF Layer - Setup Route & UI (2.5 hours)

#### Task 3.1: Add `/bff/validate-setup-secret` Endpoint

**Files:**
- `Explore.Blazor/Program.cs`

```csharp
app.MapPost("/bff/validate-setup-secret", (SetupSecretRequest request, ISetupSecretProvider provider) =>
{
    if (!provider.IsSetupModeActive)
        return Results.Json(new { valid = false, error = "Setup already completed" }, statusCode: 410);

    var isValid = provider.ValidateSecret(request.Secret);
    return Results.Ok(new { valid = isValid });
})
.RequireRateLimiting("SetupSecret");
```

**Acceptance:** BFF endpoint validates secrets. Returns 410 when locked.

#### Task 3.2: Forward X-Setup-Secret via YARP (with Header Injection Prevention)

**Files:**
- `Explore.Blazor/Program.cs` - YARP transform configuration (lines 357-375)

**REFINEMENT 3: YARP Header Sanitization**
In the existing `AddRequestTransform` block, FIRST `Remove("X-Setup-Secret")` from the proxy request BEFORE adding it from the incoming request. This prevents "Header Injection" where an external user crafts a request with a spoofed `X-Setup-Secret` header that bypasses BFF validation.

```csharp
// Inside context.AddRequestTransform(async transformContext => { ... })
// STRIP first, then ADD — prevents header injection from external requests
transformContext.ProxyRequest.Headers.Remove("X-Setup-Secret");
var incomingSetupSecret = httpContext.Request.Headers["X-Setup-Secret"].FirstOrDefault();
if (!string.IsNullOrWhiteSpace(incomingSetupSecret))
{
    transformContext.ProxyRequest.Headers.Add("X-Setup-Secret", incomingSetupSecret);
}
```

**Why Remove() first:** Without this, a malicious user could send an `X-Setup-Secret` header directly to the BFF. The YARP proxy would forward it as-is to the API, potentially bypassing the BFF's validation step. By stripping first, only headers explicitly re-added by the BFF code are forwarded.

**Acceptance:** API receives `X-Setup-Secret` header when proxied through BFF. Externally-injected headers are stripped before forwarding.

#### Task 3.3: Update Startup Redirect Logic

**Files:**
- `Explore.Blazor/Program.cs` (currently lines 517-527)

- Change redirect from `/startup` to `/setup`
- Add middleware: if bootstrap complete and path is `/setup` -> redirect to `/`
- Preserve `returnUrl` query parameter through redirects

**Acceptance:** Root routes to `/setup` during setup. `/setup` redirects to `/` after completion.

#### Task 3.4: Update InstanceOnboardingStatusModel in Blazor Client

**Files:**
- `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`

Add to model (mirror DTO):
```csharp
public bool IsSetupModeActive { get; set; }
public bool SetupSecretFromEnvironment { get; set; }
public DateTime? InstanceStartedAt { get; set; }
```

**Acceptance:** Model matches API DTO.

#### Task 3.5: Create Setup.razor Page

**Files:**
- `Explore.Blazor.Client/Pages/Setup.razor` - NEW

**Flow:**
1. On load: call GET /api/v1/InstanceOnboarding/status
2. If `IsCompleted` -> redirect to `/`
3. Show "Enter setup secret" form with:
   - Secret input field (password type)
   - "Validate" button
   - Help text: "Check the `SETUP_SECRET` environment variable, or check the API server logs."
4. On validate: call POST /bff/validate-setup-secret
5. If valid: store secret in `sessionStorage` (persists across Keycloak OIDC redirect within same tab, more secure than localStorage — cleared on tab close)
6. After login: retrieve secret from `sessionStorage`, show onboarding completion form
7. On completion: forward `X-Setup-Secret` header with API calls

**Why sessionStorage:** The Keycloak OIDC redirect would lose any in-memory Blazor state. `sessionStorage` survives the redirect within the same browser tab but is cleared when the tab closes (better security posture than `localStorage`).

**Acceptance:** Full flow: secret entry -> OIDC login -> return -> complete onboarding.

---

### Phase 4: Safety & Polish (1 hour)

#### Task 4.1: 60-Minute Proximity Timer

**Files:**
- `Explore.Application/Features/InstanceOnboarding/Handlers/Queries/GetInstanceOnboardingStatusQueryHandler.cs`
- `Explore.Application/DTOs/Onboarding/InstanceOnboardingStatusDto.cs` - Add `SetupTimedOut` field

If setup mode is active and `InstanceStartedAt` is more than 60 minutes ago, set `SetupTimedOut = true`. The Blazor UI shows: "Setup window has expired. Please restart the application."

Safety rationale: if someone boots an instance and forgets about it, the auto-generated secret becomes stale. Rather than accepting it indefinitely, the timer forces a restart (which generates a new secret).

**Acceptance:** After 60 minutes with no admin, setup page shows timeout message.

#### Task 4.2: System Info on Setup Page

**Files:**
- `Explore.Blazor.Client/Pages/Setup.razor`

Display "Instance started at {InstanceStartedAt} UTC" and remaining setup time on the page, so the real admin can confirm it's their instance.

**Acceptance:** Boot timestamp and countdown visible on setup page.

---

### Phase 5: Testing & Verification (1.5 hours)

#### Task 5.1: Unit Tests for SetupSecretProvider

- Secret from env var is used correctly
- Auto-generated secret is 32 chars and crypto-random
- `ValidateSecret` returns true for correct secret, false for wrong
- `Lock()` transitions to locked mode
- `IsSetupModeActive` returns false after Lock()
- Lazy bootstrap check (already completed in DB -> locked)

#### Task 5.2: Unit Tests for SetupSecretRequiredAttribute

- Returns 410 when locked
- Returns 403 when secret missing or wrong
- Passes through when secret is correct

#### Task 5.3: Update Existing InstanceOnboardingService Tests

- Model correctly maps new DTO fields
- Existing tests still pass

#### Task 5.4: Full Build + Test Suite Pass

```bash
dotnet build --configuration Release --verbosity quiet
# Run all test projects individually per CLAUDE.md
```

**Acceptance:** Zero regressions, architecture tests pass.

---

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Auto-generated secret lost on restart (no env var) | Medium | Prominent log message explains this. Users can set env var and restart. |
| Timing attacks on secret validation | Low | `CryptographicOperations.FixedTimeEquals` prevents timing side-channels |
| Brute force on setup secret | Low | Rate limiting (5/min) + 32-char secret = computationally infeasible |
| OIDC redirect loses setup secret | Medium | `sessionStorage` persists across same-tab redirects |
| Breaking existing onboarding flow | High | All changes are additive. Existing complete/status logic preserved. |
| DI ordering (singleton needing scoped repo) | Medium | Lazy bootstrap check via IServiceProvider scope creation |

## Success Metrics

- [ ] `SETUP_SECRET` env var missing -> auto-generated 32-char secret logged in formatted box
- [ ] `SETUP_SECRET` env var set -> confirmation logged, that value used
- [ ] All onboarding write endpoints require `X-Setup-Secret` header
- [ ] `CryptographicOperations.FixedTimeEquals` used for comparison (timing-safe)
- [ ] Rate limiting: 5 requests/minute on setup endpoints (429 on exceed)
- [ ] `LogWarning` with userId + IP when instance is claimed
- [ ] `/setup` page: secret input -> validate -> login -> complete
- [ ] `sessionStorage` persists secret across Keycloak OIDC redirect
- [ ] 60-minute proximity timer on unclaimed instances
- [ ] Once onboarding complete -> `/setup` redirects, write endpoints return 410
- [ ] All existing tests pass (no regressions)
- [ ] Architecture tests pass (Clean Architecture rules)

## Time Estimate

| Phase | Estimate |
|-------|----------|
| Phase 1: Core Infrastructure (SetupSecretProvider) | 1.5 hours |
| Phase 2: API Layer (Filter + Rate Limiting) | 1.5 hours |
| Phase 3: Blazor BFF Layer (Setup Route & UI) | 2.5 hours |
| Phase 4: Safety & Polish | 1 hour |
| Phase 5: Testing & Verification | 1.5 hours |
| **Total** | **~8 hours** |
