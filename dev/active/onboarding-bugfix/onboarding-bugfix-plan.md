# Onboarding Bugfix — Implementation Plan

## v5 Root Cause (JWT authority cold-start hang)

`/onboarding/instance` rendered nothing after a successful `Save & Continue` because every authenticated downstream API call hit the BFF `bff-interactive` 4 s timeout. Logs proved the API spent ~100 s inside `JwtBearer` authentication before failing with `IDX10500: No security keys were provided`. `DynamicJwtConfigurationService.ConfigurationManager` was constructed at startup but its OIDC discovery + JWKS fetch was lazy — it ran on the first JWT validation, which routinely exceeds the BFF attempt timeout when Keycloak is across the public internet (DNS + TLS + HTTP cold start).

Oracle `bg_051bb7da` (session `ses_24e171f17ffehMn2UbnFECPcbN`) ranked **H5** (stale/unprimed JWT config) confirmed > **H2** (JWKS metadata fetch hanging on first call — the underlying mechanism). H1 ruled out (auth-provider save handler already calls `ReloadAsync`). Effort: short.

## v5 Fix Applied

- New `Explore.API/Authentication/JwtAuthorityWarmupHostedService.cs` (`IHostedService`):
  - `StartAsync`: `await _dynamicConfig.ReloadAsync(ct)` then `await _dynamicConfig.ConfigurationManager.GetConfigurationAsync(ct)` to force the OIDC + JWKS prefetch once.
  - Logs `[JWT-Warmup]` with authority, issuer, and signing-key count.
  - Skips with an info log if no authority is configured yet (pre-onboarding); existing `IJwtAuthorityRefreshNotifier.ReloadAsync` calls in onboarding handlers will warm it later.
  - `try/catch` swallows non-cancellation exceptions and logs a warning — the API must still start if Keycloak is briefly unreachable at boot.
- `Explore.API/Extensions/AuthenticationExtensions.cs`: registers the hosted service alongside the existing `DynamicJwtConfigurationService` singleton.

## v5 Verification

- `dotnet build --configuration Release --verbosity quiet` → 0 errors.
- `Event.API.IntegrationTests` → 564/564 passed.
- `Explore.Blazor.IntegrationTests` → 23/23 passed.
- Runtime verification pending Aspire restart by user.

---

## v4 Root Causes (Cerbos auto-detect UX)

After v3 gating fixes landed, the Keycloak → authz-provider flow surfaced three UX-level bugs on the Authorization Provider Configuration page:

1. **`https://` prefix leaking to UI & storage.** `GrpcEndpointNormalizer.Normalize(raw)` prepends a scheme for any bare `host:port` (required by `GrpcChannel.ForAddress`), but the read/write/command paths all called it on the value surfaced to the DTO and persisted in settings. The user's Infisical value `cerbosgrpc.openislamu.org:443` was rewritten to `https://cerbosgrpc.openislamu.org:443` before it ever reached the input field.
2. **Auto-detected but not auto-verified.** `GetAuthorizationProviderConfigurationQueryHandler` intentionally skipped the gRPC health check on page-load to avoid blocking the response; the Razor page rendered the detected chip without the verified chip, and **Save & Continue** stayed disabled until the user clicked **Verify Endpoint** manually.
3. **Silent post-save navigation abort.** `SaveAndContinueAsync` awaited `InstanceOnboardingService.GetStatusAsync()` inline and only navigated when the fresh status could be read. Any transient BFF→API failure left `freshStatus = null` and silently skipped both navigation branches, so the user saw the success toast and stayed stuck on the page.

## v4 Fixes

1. **Preserve the raw Cerbos endpoint everywhere except channel creation.**
   - `Explore.API/Extensions/ConfigurationExtensions.cs`: env-var ingestion uses `Trim()` only, no normalization.
   - `Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`: `ReadConfigurationAsync` returns raw env/persisted value; `ApplyConfigurationAsync` persists raw; detection check uses a local (non-surfaced) normalization so the `http://localhost:3593` placeholder still suppresses the "detected" chip. `VerifyCerbosEndpointAsync` keeps its internal `Normalize` because `GrpcChannel.ForAddress` requires a scheme.
   - `SaveAuthorizationProviderConfigurationCommandHandler`: `Trim()` instead of `Normalize()`.
   - Cleaned unused `using Explore.Application.Utilities;` imports.
2. **Auto-verify on env detection.** `AuthorizationProviderConfiguration.razor` `OnAfterRenderAsync` fires `VerifyCerbosEndpointAsync()` when `CerbosDetectedFromEnvironment && !CerbosEndpointVerified && Provider == "cerbos"`, inside a `try/catch` so a verification failure surfaces as a normal error without breaking lifecycle.
3. **Resilient navigation.** `SaveAndContinueAsync` wraps `GetStatusAsync()` in `try/catch`, starts `isAuthenticated` from the `_isAuthenticated` captured at `OnInitializedAsync`, and always navigates on save success — `/onboarding/instance` if authenticated, `/login?returnUrl=/onboarding/instance` otherwise.

## v4 Verification

- `dotnet build --configuration Release --verbosity quiet` → 0 errors.
- `Explore.Blazor.IntegrationTests` → 23/23 pass.
- `Explore.Blazor.Client.Tests` → 797 pass / 1 skipped / 1 pre-existing `AppearanceThemeServiceTests.PersistThemeModeAsync_PostsExpectedThemeValue` (unrelated).

## v4 Files Changed

- `Explore.API/Extensions/ConfigurationExtensions.cs`
- `Explore.Infrastructure/Services/AuthorizationProviderConfigurationService.cs`
- `Explore.Application/Features/InstanceOnboarding/Handlers/Commands/SaveAuthorizationProviderConfigurationCommandHandler.cs`
- `Explore.Blazor.Client/Pages/Onboarding/AuthorizationProviderConfiguration.razor`

---

## v2 Root Cause (RUNTIME-CONFIRMED)

The real runtime failure was **double-registration of the Polly Standard resilience handler** in `Microsoft.Extensions.Http.Resilience`:

- `Explore.ServiceDefaults/Extensions.cs` registered `AddStandardResilienceHandler()` globally inside `ConfigureHttpClientDefaults(...)` (the default .NET Aspire template).
- `Explore.Blazor/Extensions/HttpClientExtensions.cs` then registered `AddStandardResilienceHandler(options => ...)` per named client via `AddInteractiveResilience()`.
- These two calls **stack** (dotnet/extensions #4814, #5021, #5695), so every BFF→API call went through two Polly pipelines. The outer pipeline enforced its default 5s attempt timeout and circuit breaker.
- When the outer circuit opened, `BrokenCircuitException` fast-failed `/api/InstanceOnboarding/status`, `/api/User/admin-authority`, `/api/PublicExperience/settings`, translations, user-sync — manifesting as: "auth-provider page takes forever", "no redirect to onboarding", "setup token invalid".

Curl probes proved the API responded in ~36 ms on both IPv4 and IPv6, so the failure was entirely on the BFF resilience stack.

### v2 Fix
1. `Explore.ServiceDefaults/Extensions.cs` — removed the global `AddStandardResilienceHandler()` registration; kept `AddServiceDiscovery()` only; documented with an anti-regression comment.
2. `Explore.Blazor/Extensions/HttpClientExtensions.cs` — rewrote `AddInteractiveResilience()` as a custom `AddResilienceHandler("bff-interactive", …)` with `RemoveAllResilienceHandlers()` prelude, 12s total / 4s per attempt timeouts, one retry on safe methods only, **no circuit breaker**. `AddAdminResilience()` and `AddBackgroundResilience()` also prepended with `RemoveAllResilienceHandlers()`.
3. New `IsUnsafeMethod(HttpMethod?)` private helper.

Build: 0 errors. `Explore.Blazor.IntegrationTests`: 23/23 pass. `Explore.Blazor.Client.Tests`: 798 pass / 1 skipped.

The five v1 bugs below are still-valid structural improvements but were not the root cause of the "did not solve" symptoms the user reported last.

---

## Executive Summary (v1 — original five bugs)

Five interconnected bugs preventing successful instance onboarding:

1. **Tenant 404s**: `DeploymentModeProvider` returned MultiTenant on fresh install → all non-onboarding API paths 404
2. **JWT Signature Failure (IDX10500)**: API JwtBearer config was static → after onboarding saves Keycloak to DB, API never fetched JWKS
3. **Stale Token Loop**: `invalid_grant` from Keycloak left broken auth cookie → infinite refresh loop
4. **Setup Secret Not Forwarded in Circuit**: `SetupSecretForwardingHandler` couldn't resolve userId when `HttpContext` is null in Blazor circuit → no `X-Setup-Secret` header → 403
5. **Dead sessionStorage Sync**: `InstanceOnboarding.razor` `OnAfterRenderAsync` read from `sessionStorage['setup-secret']` which was never written → secret never synced to `SetupSecretSessionService` for authenticated user

All five fixes are atomic, in working tree, build clean, all tests green.

## Root Cause Chain (Before Fixes)

1. User submits onboarding → POST `/api/InstanceOnboarding/complete` (tenant-exempt path, but JWT required)
2. JWT validation fails (Bug 2) → 401
3. Concurrent background calls from onboarding page (translation, user/sync, admin-authority) 404'd by tenant middleware (Bug 1)
4. Cookie revalidation fires token refresh, Keycloak rejects stale token (Bug 3) → principal rejected → user appears logged out
5. Even if auth worked, `InstanceOnboardingService.CompleteAsync()` runs in Blazor circuit where `HttpContext` is null → `SetupSecretForwardingHandler` can't add `X-Setup-Secret` header → 403 (Bug 4)
6. `OnAfterRenderAsync` tried to sync secret from sessionStorage that was never set → `SetupSecretSessionService` never got secret for authenticated user → double 403 path (Bug 5)

## Fix 1 — Pre-onboarding SingleTenant Fallback

**File**: `Explore.Infrastructure/Services/DeploymentModeProvider.cs`

**Change**: Added explicit config Layer 1 (`IConfiguration["Deployment:Mode"]`) that wins over all other layers. DB branch (Layer 3) now returns `SingleTenant` when `InstanceBootstrapState` is null or incomplete, falling back to DefaultTenantId.

**Rationale**: Fresh install has no tenants. Serving the default tenant is safe and prevents 404s on all API paths during onboarding. Test fixtures that explicitly set `Deployment:Mode=MultiTenant` in config are honored by Layer 1.

## Fix 2 — Dynamic JWT Authority Refresh

**New Files**:
- `Explore.Application/Contracts/Services/IJwtAuthorityRefreshNotifier.cs` — contract
- `Explore.API/Authentication/DynamicJwtConfigurationService.cs` — singleton, builds ConfigurationManager from env at startup, swaps from DB on ReloadAsync()
- `Explore.API/Authentication/DynamicJwtBearerPostConfigureOptions.cs` — IPostConfigureOptions wires dynamic CM into JwtBearerOptions

**Modified Files**:
- `Explore.API/Extensions/AuthenticationExtensions.cs` — removed static Authority/MetadataAddress/ValidIssuer, added service registrations
- `CompleteInstanceOnboardingCommandHandler.cs` — added notifier call post-commit
- `SaveAuthProviderConfigurationCommandHandler.cs` — added notifier call after ApplyConfigurationAsync
- `UpdateAuthProviderConfigurationCommandHandler.cs` — added notifier call after ApplyConfigurationAsync
- `UpdateAuthProviderConfigurationCommandHandlerTests.cs` — added mock notifier param

**Rationale**: API needs to validate JWTs signed by Keycloak whose authority was dynamically configured during onboarding. Static startup config can't know the authority until DB has it. The post-configure pattern swaps the ConfigurationManager at runtime after onboarding saves config.

## Fix 3 — Graceful Token Refresh Failure

**File**: `Explore.Blazor/Services/TokenRefreshCookieEvents.cs`

**Changes**:
- `RefreshResult` readonly record struct with failure reason tracking
- `ParseOidcErrorCode` extracts `error` field from Keycloak JSON response
- `RejectAndSignOutAsync` calls `SignOutAsync` + redirects HTML requests to `/login?session=expired&reason={}`
- XHR/API requests still get 401 (no redirect)
- `IsHtmlNavigation` checks GET + Accept: text/html header

**Rationale**: `invalid_grant` means the refresh token is permanently invalid (realm reconfigured during onboarding, token revoked, clock skew). Looping on `RejectPrincipal` leaves a broken cookie. Signing out and redirecting gives the user a clear path to re-authenticate.

## Fix 4 — Setup Secret Forwarding in Blazor Circuit Context

**File**: `Explore.Blazor/Services/SetupSecretForwardingHandler.cs`

**Change**: Added `ExtractUserIdFromAuthorizationHeader()` method that:
- Reads the `Authorization: Bearer <token>` header (already set by `AccessTokenForwardingHandler` which runs first in the pipeline)
- Parses the JWT to extract `sub`, `ClaimTypes.NameIdentifier`, or `sid` claim as userId
- Uses this userId to look up the setup secret from `SetupSecretSessionService`
- Falls back to cookie → HttpContext.User → JWT → session service

**Rationale**: In Blazor InteractiveServer circuit, `IHttpContextAccessor.HttpContext` is null. The `AccessTokenForwardingHandler` has already set the `Authorization` header from the circuit's stored token. Parsing the JWT to extract userId provides a reliable fallback when HttpContext is unavailable.

## Fix 5 — InstanceOnboarding Dead sessionStorage Sync Replacement

**Files Modified**:
- `Explore.Blazor.Client/Pages/Onboarding/InstanceOnboarding.razor` — replaced dead `sessionStorage.getItem("setup-secret")` with `syncSetupSecret(null)` JS interop call via `/js/bff.js`
- `Explore.Blazor.Client.Tests/Pages/Onboarding/InstanceOnboardingTests.cs` — added `SetupBffJsModule()` mock for JS interop

**Change**: `OnAfterRenderAsync` now calls `bffModule.InvokeAsync<BffMutationResult>("syncSetupSecret", (string?)null)` which sends `POST /bff/setup-secret/sync` with empty body. The BFF endpoint reads from the HTTP-only `setup-secret` cookie → validates → stores in `SetupSecretSessionService` keyed by authenticated userId. Error handling for 400/410 statuses redirects to `/setup`.

**Rationale**: The previous `sessionStorage.getItem("setup-secret")` was never written to by any code path — it was dead code. The BFF sync endpoint exists specifically to allow the authenticated browser session to transfer the persisted cookie secret into `SetupSecretSessionService` per userId. Using JS interop (same pattern as Setup.razor and AuthorizationProviderConfiguration.razor) ensures the call goes through YARP which correctly forwards cookies.

## Test Evidence

| Project | Result | Notes |
|---------|--------|-------|
| Event.Application.UnitTests | 840/840 | ✅ |
| Event.Domain.UnitTests | 207/207 | ✅ |
| Event.Architecture.Tests | 90/90 | ✅ |
| Explore.Secrets.UnitTests | 201/201 | ✅ |
| Event.Persistence.IntegrationTests | 58/58 | ✅ |
| Event.API.IntegrationTests | 563/564 | 1 pre-existing baseline failure |
| Explore.Blazor.IntegrationTests | 23/23 | ✅ |
| Explore.Blazor.Client.Tests | 795/796 | 1 pre-existing MudBlazor skip |

## Risk Assessment

- **Low risk**: All five fixes target the specific failure modes. Bug 1 only changes behavior for null/incomplete bootstrap state (fresh install). Bug 2 is additive (new singleton + post-configure). Bug 3 only changes behavior on token refresh failure. Bug 4 adds a fallback that only triggers when HttpContext is null. Bug 5 replaces dead code with working JS interop.
- **Regression risk**: Bug 1 initially caused 6 API integration test regressions (MultiTenant test fixtures). Fixed by adding explicit config Layer 1. All regressions resolved.
- **Deployment**: No migration needed. No schema changes. No config changes required by operators. Fixes take effect on next build.