# Onboarding Bugfix — Context

Last Updated: 2026-04-21 Europe/Brussels

## SESSION PROGRESS (2026-04-21) — v5: JWT AUTHORITY STARTUP WARMUP

### 🎯 USER REPORT

After v4 Cerbos UX fixes, the Cerbos save flow worked end-to-end (Save & Continue navigated to `/onboarding/instance`), but the destination page hung indefinitely:

> "i click on save and continue button and i only see message saved but ain't getting redirected to the instance onboarding stepts ! use aspire cli to get logs."

User confirmed (m0249) the navigation fired but `/onboarding/instance` never finished rendering — every authenticated downstream API call (`/api/user/sync`, `/api/user`, `/api/InstanceOnboarding/status`, `/api/instance/settings/*`) hit the BFF `bff-interactive` 4 s attempt timeout, retried once, and gave up.

### 🎯 ROOT CAUSE (Oracle bg_051bb7da, ses_24e171f17ffehMn2UbnFECPcbN)

Smoking-gun evidence in `temporary.txt`:

```
[JWT] Authentication failed for "GET" "/api/InstanceOnboarding/status":
"IDX10500: Signature validation failed. No security keys were provided to validate the signature."
```

`/api/InstanceOnboarding/status` (anonymous = 36 ms) spent **100 070 ms inside the JwtBearer authentication handler** before failing. Anonymous calls return instantly because `OnMessageReceived` nulls the token for `/api/instanceonboarding/**`. Authenticated calls (`/api/user`, `/api/user/sync`, `/api/instance/settings/*`) all share the same path and all hang the same way.

Why: `DynamicJwtConfigurationService.ConfigurationManager` is built lazily and the OIDC discovery + JWKS fetch from Keycloak (`https://auth.openislamu.org/realms/ISLAMU/.well-known/openid-configuration` + JWKS endpoint) only happens **on first JWT validation**. That cold network call (DNS + TLS + HTTP across the public internet) routinely exceeds the BFF's 4 s `bff-interactive` per-attempt timeout, so every authenticated downstream call from the freshly-restarted BFF is reaped before JWKS finishes loading on the API side. The cached `_state.Manager` was built fine — but `GetConfigurationAsync()` had never been awaited.

Oracle ranking: **H5** (stale/unprimed JWT config) confirmed > **H2** (JWKS metadata fetch hanging on first call — this is the underlying mechanism). H1/H3/H4 ruled out.

### ✅ FIX APPLIED (v5)

**`Explore.API/Authentication/JwtAuthorityWarmupHostedService.cs`** (new, ~70 lines):
- `IHostedService.StartAsync` calls `DynamicJwtConfigurationService.ReloadAsync(ct)` to source the latest authority from DB-or-env, then awaits `ConfigurationManager.GetConfigurationAsync(ct)` to force the OIDC discovery + JWKS fetch ONCE during startup.
- Logs `[JWT-Warmup]` with authority + issuer + signing-key count on success.
- Wraps in `try/catch` — never throws back to the host. If Keycloak is briefly unreachable at boot, the API still starts; existing per-request lazy load + the existing `IJwtAuthorityRefreshNotifier.ReloadAsync()` calls on auth-provider mutations remain the safety nets.
- If no authority is configured yet (fresh instance pre-onboarding), warmup is skipped and a `[JWT-Warmup]` info line is logged — onboarding's existing `ReloadAsync` calls in `SaveAuthProviderConfigurationCommandHandler` / `UpdateAuthProviderConfigurationCommandHandler` / `CompleteInstanceOnboardingCommandHandler` will warm it then.

**`Explore.API/Extensions/AuthenticationExtensions.cs`** — registers the hosted service alongside the existing `DynamicJwtConfigurationService` singleton:

```csharp
services.AddSingleton<DynamicJwtConfigurationService>();
services.AddSingleton<IJwtAuthorityRefreshNotifier>(sp => sp.GetRequiredService<DynamicJwtConfigurationService>());
services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, DynamicJwtBearerPostConfigureOptions>();
services.AddHostedService<JwtAuthorityWarmupHostedService>();   // ← NEW
```

### ✅ VERIFICATION

- `dotnet build --configuration Release --verbosity quiet` → **0 errors** (3270 pre-existing warnings, all repo-wide and unrelated).
- `Event.API.IntegrationTests`: **564/564 passed** (37.8 s).
- `Explore.Blazor.IntegrationTests`: **23/23 passed** (3.1 s).

### 🔧 EXPECTED RUNTIME BEHAVIOR AFTER RESTART

1. API startup logs a single `[JWT-Warmup] OIDC metadata + JWKS prefetched. Authority=…, Issuer=…, SigningKeys=N` line within a few seconds of boot (or `[JWT-Warmup] No JWT authority configured yet …` if pre-onboarding).
2. First authenticated request from BFF (`/api/user/sync`, `/api/user`, etc.) finds JWKS cached and validates in milliseconds.
3. The `IDX10500: No security keys were provided` log line should disappear from steady-state operation.
4. `/onboarding/instance` finishes rendering instead of hanging on the cascade of timed-out downstream calls.

### Files changed (v5)

- `Explore.API/Authentication/JwtAuthorityWarmupHostedService.cs` (new)
- `Explore.API/Extensions/AuthenticationExtensions.cs` (one-line `AddHostedService` registration)

### Action required from user

- **Restart Aspire AppHost** so the new API binary loads with the warmup hosted service active.
- Refresh the page that was previously stuck on `/onboarding/instance` — it should now render.
- Watch the API console for the `[JWT-Warmup]` info line on startup and confirm no `IDX10500` warnings during normal use.

---

## SESSION PROGRESS (2026-04-21) — v4: CERBOS AUTO-DETECT UX FIXES

### 🎯 USER REPORT

After v3 gating fixes landed, Keycloak login successfully landed the user on the Authorization Provider Configuration page — but three UX regressions surfaced:

> "i see already selected the cerbos provider and message that it's been detected in environment variables… problem is that its not in green mode. i need to click on the test connection and then it shows green. but in infisical i only put value `cerbosgrpc.openislamu.org:443` and in the input field i see auto put from env vars `https://cerbosgrpc.openislamu.org:443` and that shouldn't be the case remove the `https://`. also i clicked on continue after having done the save and continue but wasn't redirected."

User also confirmed with `grpcurl`:

> "with grpcurl request only works without the https:// ! `grpcurl cerbosgrpc.openislamu.org:443 list` ok"

### 🎯 ROOT CAUSES

1. **URL prefix leak.** `GrpcEndpointNormalizer.Normalize()` prepends `https://` to any bare `host:port` and is called throughout the config read/write path. `GrpcChannel.ForAddress()` requires a scheme, but the value returned to the UI DTO (and persisted to settings) was also normalized. User's raw Infisical value `cerbosgrpc.openislamu.org:443` was transformed to `https://cerbosgrpc.openislamu.org:443` before it ever reached the input box.
2. **Detected ≠ verified.** When Cerbos was auto-detected from env vars, the query handler pre-selected Provider = `cerbos` but deliberately skipped verification to avoid a blocking gRPC health check on page load. The Razor page therefore rendered a pre-filled endpoint with the detected chip but no verified chip, forcing the user to click **Verify Endpoint** manually before **Save & Continue** became enabled.
3. **Silent navigation abort.** `SaveAndContinueAsync` awaited `InstanceOnboardingService.GetStatusAsync()` inline after a successful save. A transient BFF→API failure (the same cascade v2/v3 addressed can still flare on cold start) left `freshStatus = null` and silently skipped both navigation branches — the user saw the success message but stayed on the page.

### ✅ FIX APPLIED (v4)

1. **Preserve raw Cerbos endpoint end-to-end.**
   - `Explore.API/Extensions/ConfigurationExtensions.cs`: `Cerbos:GrpcEndpoint` env-var ingestion no longer runs through `GrpcEndpointNormalizer.Normalize(...)`. Stores `Trim()`-ed raw value so UI reflects exactly what was put in Infisical.
   - `Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`:
     - `ReadConfigurationAsync()` returns raw env/persisted value to the DTO. Detection of `CerbosDetectedFromEnvironment` still uses a *local* normalization (`Normalize(raw) != "http://localhost:3593"`) so the default-localhost placeholder still doesn't trigger the "detected from env" chip, but that normalized form never leaves the function.
     - `ApplyConfigurationAsync()` persists the raw trimmed value.
     - `VerifyCerbosEndpointAsync()` continues to normalize internally (required by `GrpcChannel.ForAddress`) — channel creation is the only layer that needs a scheme.
   - `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/SaveAuthorizationProviderConfigurationCommandHandler.cs`: no longer mutates `request.Configuration.CerbosGrpcEndpoint` with `Normalize`. Just `Trim()`-es. Downstream `VerifyCerbosEndpointAsync` handles scheme internally.
   - Removed now-unused `using Explore.Application.Utilities;` imports from the two files above.
   - `VerifyCerbosEndpointCommandHandler` unchanged — it still normalizes before calling the service because callers (client UI) send raw input and the gRPC channel requires a scheme.

2. **Auto-verify when detected from env.**
   - `Explore.Blazor.Client/Pages/Onboarding/AuthorizationProviderConfiguration.razor`: `OnAfterRenderAsync` now, right after the model loads, checks `CerbosDetectedFromEnvironment && !CerbosEndpointVerified && Provider == "cerbos"` and fires `VerifyCerbosEndpointAsync()` automatically. Wrapped in `try/catch` so a verification failure is logged and surfaces as the normal error path without throwing out of `OnAfterRenderAsync`. `StateHasChanged()` is called so the verified chip appears as soon as the health check returns.

3. **Resilient post-save navigation.**
   - Same Razor page: `SaveAndContinueAsync` now wraps `GetStatusAsync()` in `try/catch`, initializes `isAuthenticated` from the cached `_isAuthenticated` captured at `OnInitializedAsync`, and only overrides it when the fresh fetch succeeds. A transient status-fetch failure no longer blocks navigation — the user is always sent to `/onboarding/instance` (or `/login?returnUrl=/onboarding/instance` if not authenticated) after a successful save.

### ✅ VERIFICATION (v4)

- `dotnet build --configuration Release --verbosity quiet` → **0 errors**, warnings unchanged from baseline.
- `Explore.Blazor.IntegrationTests` → **23/23 pass** (same as v3 baseline).
- `Explore.Blazor.Client.Tests` → **797 pass / 1 skipped / 1 pre-existing failure** (`AppearanceThemeServiceTests.PersistThemeModeAsync_PostsExpectedThemeValue` — confirmed pre-existing in v3 baseline via `git stash`, unrelated to Cerbos fixes).

### 📁 FILES CHANGED (v4)

- `Explore.API/Extensions/ConfigurationExtensions.cs`
- `Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`
- `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/SaveAuthorizationProviderConfigurationCommandHandler.cs`
- `Explore.Blazor.Client/Pages/Onboarding/AuthorizationProviderConfiguration.razor`

---

## SESSION PROGRESS (2026-04-21) — v2: REAL RUNTIME FIX

### 🎯 ROOT CAUSE (v2, CONFIRMED FROM RUNTIME LOGS)

After user reported the v1 fix "did not solve" the runtime symptoms (login flows forever, no redirect after auth-provider continue, "setup token invalid" on return), live Aspire dashboard logs + curl probes revealed the **real** issue:

**DOUBLE RESILIENCE HANDLER STACKING** (dotnet/extensions #4814, #5021, #5695)

- `Explore.ServiceDefaults/Extensions.cs` had the stock Aspire template block:
  ```csharp
  builder.Services.ConfigureHttpClientDefaults(http =>
  {
      http.AddStandardResilienceHandler(); // <-- pipeline #1, DEFAULT 5s AttemptTimeout
      http.AddServiceDiscovery();
  });
  ```
- `Explore.Blazor/Extensions/HttpClientExtensions.cs` then called `AddInteractiveResilience()` per named client, which internally called `AddStandardResilienceHandler(options => {...})` — **pipeline #2**.
- Microsoft.Extensions.Http.Resilience **stacks** both handlers rather than replacing. The outer default pipeline enforces its own 5s attempt timeout and circuit-breaker, which fires on same-machine BFF→API requests even though the API responds in 36ms.
- Once the outer circuit opens, every downstream request (`/api/InstanceOnboarding/status`, `/api/User/admin-authority`, `/api/PublicExperience/settings`, translations, user-sync) fast-fails with `BrokenCircuitException`, breaking the entire onboarding redirect chain and producing the "setup token invalid" fallback.
- Curl evidence: API returned `/api/InstanceOnboarding/status` in **0.036s** over both IPv4 and IPv6 while the BFF was timing out at 5s → conclusively not an API/network issue.

### ✅ FIX APPLIED (v2)

1. **`Explore.ServiceDefaults/Extensions.cs`** — Removed the global `ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler())` block entirely. Replaced with a documented `// Intentionally not registering...` comment citing the three GitHub issues so a future template sync does not silently re-introduce the stacking bug. `AddServiceDiscovery()` is still registered globally because it does not stack harmfully.
2. **`Explore.Blazor/Extensions/HttpClientExtensions.cs`** — Rewrote the three resilience profiles as belt-and-suspenders single-pipeline builders:
   - `AddInteractiveResilience()` now uses a hand-tuned `AddResilienceHandler("bff-interactive", ...)` pipeline:
     * `RemoveAllResilienceHandlers()` (experimental `EXTEXP0001`, pragma-suppressed) clears any inherited handler.
     * Outer `AddTimeout(12s)` total budget.
     * `AddRetry(MaxRetryAttempts=1, Delay=200ms, Constant)` with a custom `ShouldHandle` that skips retries on `POST/PUT/PATCH/DELETE` (via new `IsUnsafeMethod` helper) and retries only on `HttpRequestException`, `TimeoutRejectedException`, and HTTP 408/502/503/504.
     * Inner `AddTimeout(4s)` per attempt.
     * **NO circuit breaker** — per Oracle guidance, circuit breakers on same-machine BFF→API traffic cause UI-wide cascades when a single endpoint stutters.
   - `AddAdminResilience()` and `AddBackgroundResilience()` retain their original `AddStandardResilienceHandler(options => {...})` shape but prepend `RemoveAllResilienceHandlers()` as belt-and-suspenders.
3. New `IsUnsafeMethod(HttpMethod?)` private helper.
4. Build + test verification:
   - Release build: **0 errors**.
   - `Explore.Blazor.IntegrationTests`: **23 passed / 0 failed**.
   - `Explore.Blazor.Client.Tests`: **798 passed / 0 failed / 1 skipped (pre-existing)**.

### ⚠️ PENDING (runtime re-verification)

- The running AppHost (PID 3933425) child processes still hold the **old Debug** DLLs because they were launched with `--no-build`. A clean restart of the AppHost is required to load the new binaries and verify the fix end-to-end:
  1. Stop the AppHost (Ctrl+C in its terminal — do NOT kill via tool; repo rules forbid destructive commands).
  2. `dotnet run --project Explore.AppHost --launch-profile https`.
  3. Walk through: `/` → `/setup` → enter setup secret → Keycloak login → `/onboarding/instance` completes without Polly `BrokenCircuitException` or `TimeoutRejectedException` in logs.
- Aspire MCP `list_resources` remains empty in this session (dashboard token flow not wired for the MCP tool); rely on the dashboard `/consolelogs` UI at `https://localhost:17071/login?t=<token>` for runtime observation.

---

## SESSION PROGRESS (2026-04-21) — v1 (prior round, kept for history)

### ✅ COMPLETED
- Bug 1: DeploymentModeProvider — pre-onboarding SingleTenant fallback + explicit config Layer 1
- Bug 2: Dynamic JWT authority refresh — IJwtAuthorityRefreshNotifier contract, DynamicJwtConfigurationService, DynamicJwtBearerPostConfigureOptions, 3 handler call sites
- Bug 3: TokenRefreshCookieEvents — invalid_grant detection, signout + HTML redirect on stale tokens
- **Bug 4: SetupSecretForwardingHandler — JWT-based userId extraction in Blazor circuit context**
  - Added `ExtractUserIdFromAuthorizationHeader()` method that parses the Bearer token set by `AccessTokenForwardingHandler` to extract `sub`/`NameIdentifier`/`sid` claims
  - Falls back to `SetupSecretSessionService.GetForUser(userId)` when `HttpContext` is null (Blazor circuit)
  - Handler resolution chain: try cookie → try HttpContext.User → try JWT from Authorization header → look up from session service
- **Bug 5: InstanceOnboarding.razor — Dead sessionStorage sync replaced with JS interop**
  - `OnAfterRenderAsync` now calls `syncSetupSecret(null)` via `/js/bff.js` JS interop module (same pattern as Setup.razor and AuthorizationProviderConfiguration.razor)
  - BFF endpoint reads from HTTP-only `setup-secret` cookie → validates → stores in `SetupSecretSessionService` keyed by authenticated userId
  - Error handling for 400/410 statuses redirects to `/setup`
- InstanceOnboardingTests.cs updated with `SetupBffJsModule()` JS interop mock
- Build: 0 errors, all warnings pre-existing
- Blazor Client Tests: 795 total, 0 failed, 794 passed, 1 skipped (pre-existing MudBlazor)
- Architecture Tests: 90 passed
- All unit + integration tests passing

### ⚠️ PENDING VERIFICATION
- Visual end-to-end test: rebuild Aspire AppHost, start fresh, walk through setup → Keycloak → onboarding → submit. The user's last-run logs were from a STALE binary (pre-fix log messages). Need a clean run to verify.
- Need to verify Bug 4+5 fix: after Keycloak login, InstanceOnboarding page can complete without 403 from setup-secret forwarding
- The `CreateBffSelfClient()` method and `IHttpClientFactory` injection still in InstanceOnboarding.razor (used for delete-on-error path). Could be replaced with JS interop but not critical.

### 🟡 KNOWN ISSUES (PRE-EXISTING, NOT OURS)
- `RouteNames_EveryConstantResolvesToExactlyOneEndpoint` (1 failure in API integration tests)
- `MudBlazor v9 skip` (1 skip in Blazor Client tests)
- Pre-existing modified files NOT from this session: `.claude/skills/auth-patterns/SKILL.md`, `Event.API.IntegrationTests/Features/LinkTableControllerTests.cs`, `Event.API.IntegrationTests/Features/StorageObjectControllerTests.cs`, `Event.API.IntegrationTests/Fixtures/ExternalApiPhase0WebApplicationFactory.cs`, `Explore.Blazor.Client.Tests/Pages/Onboarding/AuthorizationProviderConfigurationTests.cs`, `Explore.Blazor.Client/Pages/Events/Components/*`, `Explore.Blazor.Client/packages.lock.json`, `docs/API.md`, `docs/AUTHORIZATION.md`, `docs/CODEBASE_INSIGHTS.md`

## Root Causes

### Bug 1 — Tenant 404s during onboarding
`DeploymentModeProvider.GetCurrentModeAsync()` returned `DeploymentMode.MultiTenant` when `InstanceBootstrapState` was null/incomplete (fresh install). `ApiTenantResolutionMiddleware` only exempts `/api/InstanceOnboarding/*` paths. Onboarding page calls `/api/translation/en`, `/api/User/sync`, etc. — all 404'd because no tenant resolved. **Fix**: `DeploymentModeProvider` Layer 3 (DB) now returns `SingleTenant` when bootstrap is null/incomplete, so middleware falls back to `PlatformDefaults.DefaultTenantId`. Also added Layer 1 (explicit `IConfiguration["Deployment:Mode"]`) that wins for any mode when explicitly set in config.

### Bug 2 — API JWT signature validation failure (IDX10500)
API `JwtBearerOptions` were statically configured from env vars at startup. After onboarding saves Keycloak config to DB, the API never learned the JWKS URI → all authenticated endpoints 401'd. **Fix**: Created `DynamicJwtConfigurationService` singleton that builds `ConfigurationManager<OpenIdConnectConfiguration>` from env vars at startup, then swaps to DB-sourced config on `ReloadAsync()`. `DynamicJwtBearerPostConfigureOptions` (IPostConfigureOptions) injects dynamic ConfigurationManager into JwtBearerOptions at runtime. Three handlers call `IJwtAuthorityRefreshNotifier.ReloadAsync()` post-commit.

### Bug 3 — Stale refresh tokens infinite loop
On `invalid_grant` from Keycloak, `TokenRefreshCookieEvents` only called `context.RejectPrincipal()`, leaving a broken auth cookie that loops on every request. **Fix**: New `RefreshResult` struct with failure reasons, `ParseOidcErrorCode` to detect `invalid_grant`/`invalid_token`, `RejectAndSignOutAsync` that calls `SignOutAsync` + redirects HTML navigations to `/login?returnUrl={}&session=expired&reason={}`. XHR/API requests still get 401.

### Bug 4 — SetupSecretForwardingHandler can't resolve userId in Blazor circuit
When `InstanceOnboardingService.CompleteAsync()` runs in Blazor InteractiveServer circuit, `IHttpContextAccessor.HttpContext` is null. Handler couldn't get userId, couldn't look up secret from `SetupSecretSessionService`, so `X-Setup-Secret` header was never added → API returns 403 Forbidden. **Fix**: Added `ExtractUserIdFromAuthorizationHeader()` that parses the JWT Bearer token (already set by `AccessTokenForwardingHandler` which runs first in the pipeline) to extract userId claims (`sub`, `NameIdentifier`, `sid`). Falls back to `SetupSecretSessionService.GetForUser(userId)`.

### Bug 5 — InstanceOnboarding.razor dead sessionStorage sync
`OnAfterRenderAsync` read from `sessionStorage.getItem('setup-secret')` which was never written. The BFF sync (`POST /bff/setup-secret/sync`) that stores the secret in `SetupSecretSessionService` per user never fired. **Fix**: Replaced with `syncSetupSecret(null)` JS interop call via `/js/bff.js` module. BFF endpoint reads from HTTP-only cookie → validates → stores in `SetupSecretSessionService` keyed by authenticated userId.

### Combined Flow That Was Broken
1. User enters setup secret on `/setup` → persisted via HTTP-only cookie + `SetupSecretSessionService` (but user NOT authenticated yet, so no userId key)
2. User authenticates via Keycloak → redirected to `/onboarding/instance`
3. `InstanceOnboarding.razor` `OnAfterRenderAsync` reads dead `sessionStorage` → returns early → secret never synced to session service for the authenticated user
4. User clicks Complete → `InstanceOnboardingService.CompleteAsync()` calls API via BffClient → `SetupSecretForwardingHandler` can't access HttpContext (null in circuit) → can't get userId → can't look up secret → no `X-Setup-Secret` header → 403

## Key Files

### Bug 1 — Tenant Resolution
**`Explore.Infrastructure/Services/DeploymentModeProvider.cs`** (modified, ~160 lines)
- Added `IConfiguration` ctor param for explicit config Layer 1
- Layer 1: `IConfiguration["Deployment:Mode"]` wins for any explicitly-set mode
- Layer 3: returns `SingleTenant` when `bootstrap is null || !bootstrap.IsCompleted`

**`Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`** (referenced, NOT modified)
- Line 47-56: SingleTenant branch falls back to DefaultTenantId
- Line 145-148: 404 "Tenant not resolved" response
- Exempt paths: `/api/InstanceOnboarding/*`

### Bug 2 — Dynamic JWT
**`Explore.Application/Contracts/Services/IJwtAuthorityRefreshNotifier.cs`** (NEW, 8 lines)
- Single method: `Task ReloadAsync(CancellationToken ct = default)`

**`Explore.API/Authentication/DynamicJwtConfigurationService.cs`** (NEW, ~131 lines)
- Sealed singleton, IDisposable (SemaphoreSlim gate)
- `BuildFromEnvironment()`: reads `Keycloak:Authority` + `Keycloak:MetadataAddress` from IConfiguration
- `BuildFromDatabaseAsync()`: scoped `IAuthProviderConfigurationService.ReadConfigurationAsync()`, falls back to env
- `ReloadAsync()`: swaps `_state` (volatile `State(ConfigurationManager?, Authority?, Source)`)
- `State` record with `ConfigurationManager`, `Authority`, `Source` ("Environment" or "Database")

**`Explore.API/Authentication/DynamicJwtBearerPostConfigureOptions.cs`** (NEW, ~30 lines)
- `IPostConfigureOptions<JwtBearerOptions>` for scheme `JwtBearerDefaults.AuthenticationScheme`
- Sets `options.ConfigurationManager`, `options.Authority`, `options.TokenValidationParameters.ValidIssuer`, `options.TokenValidationParameters.ValidIssuers`

**`Explore.API/Extensions/AuthenticationExtensions.cs`** (modified)
- Removed static `options.Authority`, `options.MetadataAddress`, `options.TokenValidationParameters.ValidIssuer`
- Added DI registrations: `DynamicJwtConfigurationService` singleton, `IJwtAuthorityRefreshNotifier` alias, `IPostConfigureOptions<JwtBearerOptions>` registration
- Preserved: RequireHttpsMetadata, AudienceValidator, ValidateIssuer, ValidateLifetime, ClockSkew=5min, NameClaimType, OnMessageReceived onboarding bypass (line 109-119), OnAuthenticationFailed, OnChallenge

**`Explore.Application/Features/InstanceOnboarding/Handlers/Commands/CompleteInstanceOnboardingCommandHandler.cs`** (modified)
- Added `IJwtAuthorityRefreshNotifier _jwtAuthorityRefreshNotifier` field (14th ctor param)
- Called `_jwtAuthorityRefreshNotifier.ReloadAsync(cancellationToken)` in post-commit side effects block

**`Explore.Application/Features/InstanceOnboarding/Handlers/Commands/SaveAuthProviderConfigurationCommandHandler.cs`** (modified)
- Added `IJwtAuthorityRefreshNotifier` ctor param (3rd)
- Called after `_configurationService.ApplyConfigurationAsync(request.Configuration)`

**`Explore.Application/Features/InstanceOnboarding/Handlers/Commands/UpdateAuthProviderConfigurationCommandHandler.cs`** (modified)
- Added `IJwtAuthorityRefreshNotifier` ctor param (5th, last)
- Called after `_configurationService.ApplyConfigurationAsync(request.Configuration)`

**`Event.Application.UnitTests/Features/InstanceOnboarding/Commands/UpdateAuthProviderConfigurationCommandHandlerTests.cs`** (modified)
- Added `_jwtAuthorityRefreshNotifier = Substitute.For<IJwtAuthorityRefreshNotifier>()` field
- Added as 5th ctor param in handler construction

### Bug 3 — Token Refresh
**`Explore.Blazor/Services/TokenRefreshCookieEvents.cs`** (modified, ~280 lines)
- New `RefreshResult` readonly record struct: `(List<AuthenticationToken>? Tokens, string? FailureReason)` with `Success()` and `Failure(reason)` factories
- `ParseOidcErrorCode(string body)`: JSON-parses Keycloak error response, extracts `error` field
- `RefreshAccessTokenAsync` now returns `RefreshResult` instead of `List<AuthenticationToken>?`
- `ValidatePrincipal`: all failure branches now call `await RejectAndSignOutAsync(context, reason)`
- `RejectAndSignOutAsync`: calls `context.RejectPrincipal()` + `context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` + `IsHtmlNavigation` check → redirect to `/login?returnUrl={}&session=expired&reason={}`
- `IsHtmlNavigation(HttpRequest)`: GET + Accept header contains `text/html`

### Bug 4+5 — Setup Secret Forwarding + InstanceOnboarding Sync
**`Explore.Blazor/Services/SetupSecretForwardingHandler.cs`** (modified, 111 lines)
- Added `System.IdentityModel.Tokens.Jwt` using for `JwtSecurityTokenHandler`
- Added `System.Security.Claims` using for `ClaimTypes`
- New `ExtractUserIdFromAuthorizationHeader(HttpRequestMessage)` private static method
- Reads `Authorization: Bearer <token>` header set by `AccessTokenForwardingHandler` (first in pipeline)
- Parses JWT to extract `sub`, `ClaimTypes.NameIdentifier`, or `sid` claim as userId
- Falls back to `SetupSecretSessionService.GetForUser(userId)` when HttpContext is null
- Resolution chain: try cookie → try HttpContext.User claims → try JWT from Authorization header → look up from session service

**`Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor`** (modified)
- Added `using Explore.Blazor.Client.Models.Responses` for `BffMutationResult`
- Added `IJSObjectReference? _bffModule` field
- Added `GetBffModuleAsync()` method that imports `/js/bff.js` (same pattern as Setup.razor)
- Replaced dead `sessionStorage.getItem("setup-secret")` sync in `OnAfterRenderAsync` with:
  - `bffModule.InvokeAsync<BffMutationResult>("syncSetupSecret", (string?)null)`
  - On success: sets `_setupSecretSynced = true`
  - On 400/410: clears secret, redirects to `/setup`
  - On other failure: shows error message

**`Explore.Blazor.Client.Tests/Pages/Onboarding/InstanceOnboardingTests.cs`** (modified)
- Added `SetupBffJsModule()` method that creates `JSInterop.SetupModule("/js/bff.js")` with `syncSetupSecret` mock returning `BffMutationResult { Ok = true }`
- Called from constructor after service setup
- Added `using Explore.Blazor.Client.Models.Responses`

## Architecture Context

- `ApiTenantResolutionMiddleware` at Program.cs:388, BEFORE auth at :391
- `DeploymentModeProvider` 3 layers: explicit config `Deployment:Mode` → `DeploymentSettings.IsSingleTenant` → DB (SingleTenant on null/incomplete)
- `DynamicJwtConfigurationService` builds `ConfigurationManager<OpenIdConnectConfiguration>` at startup, swaps on `ReloadAsync()`
- `DynamicJwtBearerPostConfigureOptions` wires dynamic ConfigurationManager into JwtBearerOptions
- Token refresh failure: SignOutAsync + redirect for HTML, plain 401 for API/XHR
- `PlatformDefaults.DefaultTenantId = 018e4e5c-7f00-7000-8000-000000000001`
- `PlatformDefaults.DefaultTenantSlug = "default"`
- `GovernanceSettingKeys.Authentication.KeycloakEnabled/KeycloakAuthority/KeycloakClientId` for DB config keys
- **BffClient handler chain**: `AccessTokenForwardingHandler` → `TenantHeaderForwardingHandler` → `SetupSecretForwardingHandler` → primary handler
- **YARP proxy** handles: strips incoming `X-Setup-Secret`, resolves from (1) request header, (2) `setup-secret` cookie, (3) `SetupSecretSessionService.GetForUser()` → adds `X-Setup-Secret` on proxied requests
- **SetupSecretRequiredFilter** (API) reads ONLY from `Request.Headers["X-Setup-Secret"]`
- **SetupSecretSessionService** stores secrets per userId, populated by `BffSetupSecretEndpoints.PersistSetupSecret` (line 298-302) and sync endpoint

## Quick Resume

1. Read this file for complete context
2. Build: `dotnet build --configuration Release --verbosity quiet`
3. Run tests per CLAUDE.md (individual projects, not solution-level)
4. For visual verification: kill dotnet processes, rebuild, start Aspire AppHost, walk through setup flow
5. All 5 bug fixes are in the working tree but UNCOMMITTED — do NOT commit without explicit user request
6. Key verification: Bug 4+5 fix means InstanceOnboarding page should now successfully POST `/api/InstanceOnboarding/complete` with the setup-secret header present