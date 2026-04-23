# Onboarding "Session Expired" Challenging Bug — Comprehensive Report

> **Status**: ONGOING — user still gets "Your session expired. Please sign in again to complete setup." after all fixes below.  
> **Last Updated**: 2026-04-22  
> **Affected Flow**: `POST /api/InstanceOnboarding/complete` in InteractiveServer render mode  
> **Files Touched**: `CircuitAccessTokenService.cs`, `TokenCircuitHandler.cs`, `CircuitUserContext.cs`, `BffAuthCookieStore.cs`, `BffCookieForwardingHandler.cs`, `ServiceRegistrationExtensions.cs`, `HttpClientExtensions.cs`, `TokenRefreshCookieEvents.cs`, `DynamicAuthSchemeManager.cs`, `InstanceOnboardingService.cs`, multiple test files

---

## 1. Executive Summary

The instance onboarding flow (`/setup`) fails at the final step with "Your session expired" when the user clicks **Complete Setup**. The page uses `@rendermode InteractiveServer` and must remain server-side only per product requirements. The root cause is a chain of auth token propagation failures in Blazor Server's circuit-dispatched events, combined with a broken `/bff/auth/refresh-session` mechanism when called from server-side code.

**Multiple fixes have been applied. The user STILL experiences the error.** This document records every hypothesis, attempted fix, code change, and log finding so that a fresh set of eyes (or a new session) can identify what was missed.

---

## 2. The Error Path (User-Facing)

1. User navigates to `/setup` (fresh instance, no onboarding completed)
2. User configures auth provider (Keycloak)
3. User logs in via OIDC
4. User fills onboarding form (instance name, admin details, etc.)
5. User clicks **Complete Setup**
6. UI shows: *"Your session expired. Please sign in again to complete setup."*
7. User is stuck in a loop

The error message comes from `InstanceOnboarding.razor:503`:
```csharp
// In InstanceOnboarding.razor CompleteAsync
var preRefreshed = await InstanceOnboardingService.RefreshAuthSessionAsync();
var result = await InstanceOnboardingService.CompleteAsync(completion);
if (result.StatusCode == HttpStatusCode.Unauthorized)
{
    // Retry once with forced refresh
    var refreshed = await InstanceOnboardingService.RefreshAuthSessionAsync(forceRefresh: true);
    result = await InstanceOnboardingService.CompleteAsync(completion);
    if (result.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Shows "Your session expired..."
    }
}
```

---

## 3. Request Chain Architecture

```
User Browser
    │ POST /api/InstanceOnboarding/complete (JSON body)
    │ ↓
Blazor Server Circuit (InteractiveServer)
    │ InstanceOnboardingService.CompleteAsync()
    │ calls _httpClientFactory.CreateClient("BffClient")
    │ ↓
BFF (YARP Proxy)
    │ AccessTokenForwardingHandler adds Authorization: Bearer <token>
    │ YARP forwards to API
    │ ↓
API
    │ JWT middleware validates token
    │ If valid → 200 + redirect to root
    │ If invalid/expired → 401
    │ ↓
Back to Blazor
    │ Sees 401 → shows "session expired"
```

**Two separate clients are used:**
- **`BffClient`** (named client) — forwards API calls from Blazor to BFF → YARP → API. Has `AccessTokenForwardingHandler`.
- **`BffSelfClient`** (named client) — calls BFF-internal endpoints like `/bff/auth/refresh-session`. Has `BffCookieForwardingHandler` (added during this fix).

---

## 4. Phase 1: Initial Investigation (Log Analysis)

### 4.1 First Log Evidence

The user provided `temporary.txt` logs. Key findings:

- **Lines 1168, 1183, 1759, 1774**: `POST /api/InstanceOnboarding/complete` → 401, `User="-"`
- **Line 937**: `[AccessTokenForwardingHandler] No token available for current user at "/api/tenant/navigation"` — proves handler IS active
- **No warning log** for `complete` request — handler found a token from SOME source
- **Line 1189**: "Complete returned 401 after pre-refresh. Retrying once with forced refresh."
- **No `/bff/auth/refresh-session` requests** visible in logs
- **No "Pre-complete auth session refresh failed"** log visible

### 4.2 First Hypothesis: Token Not Found in Circuit Scope

`AccessTokenForwardingHandler` had 3 strategies:

1. **Strategy 1**: `HttpContext.GetTokenAsync("access_token")` — fails when `IHttpContextAccessor.HttpContext` is null (Blazor circuit-dispatched events)
2. **Strategy 2**: Static store keyed by `HttpContext.User.FindFirst("sub")?.Value` — fails when `HttpContext.User` is null
3. **Strategy 3**: Scoped `ICircuitAccessTokenService` — `SetToken` called by middleware, but `AccessTokenForwardingHandler` runs in a DIFFERENT scope due to `IHttpClientFactory` handler pooling

The static `_tokenStore` DID have the token (populated by `CaptureAccessTokenAsync` middleware), but Strategy 2 couldn't retrieve it without the userId.

---

## 5. Phase 2: AuthenticationStateProvider Attempt (FAILED)

### 5.1 The Fix

Added `AuthenticationStateProvider?` as optional 4th constructor parameter to `AccessTokenForwardingHandler`.

**Strategy 4**: When `HttpContext` is null and `ICircuitAccessTokenService` has no token, call `authenticationStateProvider.GetAuthenticationStateAsync()` to get user claims, extract `sub`, and look up token in static `_tokenStore`.

### 5.2 Why It Failed

```
System.InvalidOperationException: Do not call GetAuthenticationStateAsync outside of the DI scope for a Razor component.
```

`ServerAuthenticationStateProvider` requires the circuit synchronization context, which is lost when `HttpClient.SendAsync` resumes on a different thread. HTTP message handlers run outside the Razor component DI scope.

**Reverted.**

---

## 6. Phase 3: TokenCircuitHandler (Partial — Scope Gap Remained)

### 6.1 The Fix

Created `TokenCircuitHandler` (`Explore.Blazor/Services/TokenCircuitHandler.cs`) — a `CircuitHandler` that captures the access token during `OnCircuitOpenedAsync`.

```csharp
public class TokenCircuitHandler : CircuitHandler
{
    private readonly ICircuitAccessTokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var token = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                _tokenService.SetToken(token);
            }
        }
    }
}
```

Registered alongside `TenantCircuitHandler`.

### 6.2 Why It Was Insufficient

Logs showed `IHttpContextAccessor.HttpContext` IS available during circuit open AND user is authenticated. Yet the 401 persisted.

**The real problem**: `IHttpClientFactory` pools `HttpMessageHandler` instances with `SetHandlerLifetime(TimeSpan.FromMinutes(5))` in `ConfigureDevCertBypass`. Handlers are created in a scope managed by the factory, NOT the caller's circuit scope. So `TokenCircuitHandler` sets the token on the circuit-scoped `ICircuitAccessTokenService`, but `AccessTokenForwardingHandler` (created by the factory in a DIFFERENT scope) reads from a different instance → `_localToken` is null.

This is fundamentally different from `TenantHeaderForwardingHandler` which works because `ITenantRouteContextAccessor` is resolved from the caller's scope somehow (or the factory uses caller scope in some cases).

---

## 7. Phase 4: ICircuitUserContext + IBffAuthCookieStore (CURRENT STATE)

### 7.1 New Hypothesis: Two Distinct Failures

**Failure 1 — AccessTokenForwardingHandler scope gap:**
- Static `_tokenStore` HAS the token (keyed by userId)
- But handler can't get userId without `HttpContext`
- Need a way to get userId across scope boundaries

**Failure 2 — BffSelfClient has no cookies:**
- `InstanceOnboardingService.RefreshAuthSessionAsync()` uses `CreateBffSelfClient()` when `_bffClient` is null (server-side)
- `BffSelfClient` is configured with `SocketsHttpHandler { UseCookies = false }` via `ConfigureDevCertBypass`
- `/bff/auth/refresh-session` endpoint requires cookie auth → returns 401 without cookies
- Token can expire during form filling (Keycloak default ~5 min), and there's no working refresh mechanism

### 7.2 Fix 4A: ICircuitUserContext (AsyncLocal Bridge)

**File**: `Explore.Blazor/Services/CircuitUserContext.cs`

```csharp
public interface ICircuitUserContext
{
    string? UserId { get; }
    void SetUserId(string? userId);
}

public sealed class CircuitUserContext : ICircuitUserContext
{
    private static readonly AsyncLocal<string?> _currentUserId = new();
    public string? UserId => _currentUserId.Value;
    public void SetUserId(string? userId) => _currentUserId.Value = userId;
}
```

Registered as **singleton**.

**Strategy 4 in AccessTokenForwardingHandler:**
```csharp
// After Strategy 3 (circuit-scoped service) fails
var circuitUserId = _circuitUserContext?.UserId;
if (!string.IsNullOrEmpty(circuitUserId))
{
    if (_tokenStore.TryGetValue(circuitUserId, out var storedToken))
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", storedToken);
        return;
    }
}
```

### 7.3 Fix 4B: IBffAuthCookieStore + BffCookieForwardingHandler

**File**: `Explore.Blazor/Services/BffAuthCookieStore.cs`

```csharp
public interface IBffAuthCookieStore
{
    string? CookieHeader { get; }
    void SetCookieHeader(string? cookieHeader);
}

public sealed class BffAuthCookieStore : IBffAuthCookieStore
{
    private static readonly AsyncLocal<string?> _currentCookie = new();
    public string? CookieHeader => _currentCookie.Value;
    public void SetCookieHeader(string? cookieHeader) => _currentCookie.Value = cookieHeader;
}
```

**File**: `Explore.Blazor/Services/BffCookieForwardingHandler.cs`

```csharp
public class BffCookieForwardingHandler : DelegatingHandler
{
    private readonly IBffAuthCookieStore _bffAuthCookieStore;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, ...)
    {
        var cookie = _bffAuthCookieStore.CookieHeader;
        if (!string.IsNullOrEmpty(cookie) && !request.Headers.Contains("Cookie"))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
        }
        return base.SendAsync(request, cancellationToken);
    }
}
```

### 7.4 Fix 4C: TokenCircuitHandler Updated

Updated `TokenCircuitHandler` to capture userId and auth cookie alongside the token:

```csharp
public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
{
    var httpContext = _httpContextAccessor.HttpContext;
    if (httpContext is null) return;

    // Capture userId for AccessTokenForwardingHandler Strategy 4
    var userId = httpContext.User.FindFirst("sub")?.Value
              ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? httpContext.User.FindFirst("sid")?.Value;
    _circuitUserContext.SetUserId(userId);

    // Capture auth cookie for BffSelfClient refresh
    if (httpContext.Request.Headers.TryGetValue("Cookie", out var cookieHeader))
    {
        _bffAuthCookieStore.SetCookieHeader(cookieHeader.ToString());
    }

    // Existing token capture
    var token = await httpContext.GetTokenAsync("access_token");
    if (!string.IsNullOrEmpty(token))
    {
        _tokenService.SetToken(token);
    }
}
```

### 7.5 Fix 4D: HttpClientExtensions Updated

**File**: `Explore.Blazor/Extensions/HttpClientExtensions.cs`

Added `BffCookieForwardingHandler` to `BffSelfClient` pipeline:

```csharp
services.AddHttpClient("BffSelfClient", client => { ... })
    .ConfigurePrimaryHttpMessageHandler(ConfigureDevCertBypass)
    .AddHttpMessageHandler<BffCookieForwardingHandler>();
```

### 7.6 Fix 4E: ServiceRegistrationExtensions Updated

Registered new services:
```csharp
services.AddSingleton<ICircuitUserContext, CircuitUserContext>();
services.AddSingleton<IBffAuthCookieStore, BffAuthCookieStore>();
services.AddTransient<BffCookieForwardingHandler>();
```

---

## 8. Phase 5: TokenRefreshCookieEvents Graceful Degradation

### 8.1 Discovery: No Refresh Token Initially

Logs showed `secretLength=0` in OIDC token endpoint request, meaning Keycloak client secret was empty. Keycloak treats the client as public, and public clients don't receive refresh tokens by default.

Before `TokenRefreshCookieEvents` was added, the cookie remained valid for 7 days even with expired access tokens. YARP would forward expired tokens, API would 401, but user wouldn't be signed out. The new `TokenRefreshCookieEvents` aggressively signed out users when tokens couldn't be refreshed.

### 8.2 Fix Applied

**File**: `Explore.Blazor/Services/TokenRefreshCookieEvents.cs`

Changed `ValidatePrincipal` to NOT sign out when there's no refresh token:

```csharp
var refreshToken = context.Properties.GetTokenValue("refresh_token");
if (string.IsNullOrEmpty(refreshToken))
{
    _logger.LogWarning(
        "[TokenRefresh] Access token expired but no refresh_token available. " +
        "Token will be forwarded as-is; API may reject it with 401.");
    // Graceful degradation: do NOT sign out
    return;
}
```

**File**: `Explore.Blazor/Services/DynamicAuthSchemeManager.cs`

Added `refreshToken={HasRefreshToken}` diagnostic logging to `OnTokenResponseReceived`.

### 8.3 Later Discovery: Refresh Token IS Now Present

In subsequent logs, `refreshToken=True` appeared and `secretLength=32` confirmed the client secret is now being sent. So the "no refresh token" issue was resolved (likely by configuring the Keycloak client as confidential), but the 401 still occurs.

---

## 9. Phase 6: API Port Mismatch (Secondary Issue, Fixed)

### 9.1 Discovery

API was running on Aspire-assigned port 38455, but BFF was hardcoded to call port 7039 via `ExploreAPI__BaseUrl` in `AppHost.cs`.

All BFF→API requests timed out after 4 seconds (Polly timeout).

### 9.2 Fix Applied

**Files**: `Explore.AppHost/AppHost.cs`, `Explore.Blazor/Extensions/HttpClientExtensions.cs`, `Explore.Blazor/Extensions/YarpProxyExtensions.cs`

- Removed hardcoded `ExploreAPI__BaseUrl` from `AppHost.cs`
- Added `ResolveApiBaseUrl()` helper that checks:
  1. Explicit `ExploreApi:BaseUrl` config
  2. Aspire service discovery env vars (`services__explore-api__https__0`)
  3. Fallback to `https://localhost:7039/` for standalone dev

---

## 10. Current Code State

### 10.1 Files Modified (in order of modification)

1. **`Explore.Blazor/Services/CircuitAccessTokenService.cs`**
   - Added `ICircuitUserContext` as 4th constructor parameter
   - Added Strategy 4: read userId from `ICircuitUserContext`, look up token in static `_tokenStore`
   - Removed earlier failed Strategy 4 (`AuthenticationStateProvider`)

2. **`Explore.Blazor/Services/TokenCircuitHandler.cs`**
   - Captures userId and auth cookie in `OnCircuitOpenedAsync`
   - Sets `ICircuitUserContext.UserId` and `IBffAuthCookieStore.CookieHeader`

3. **`Explore.Blazor/Services/CircuitUserContext.cs`** (NEW)
   - Singleton using `AsyncLocal<string>` to bridge userId across DI scope boundaries

4. **`Explore.Blazor/Services/BffAuthCookieStore.cs`** (NEW)
   - Singleton using `AsyncLocal<string>` to store auth cookie from initial HTTP request

5. **`Explore.Blazor/Services/BffCookieForwardingHandler.cs`** (NEW)
   - HTTP message handler that adds `Cookie` header from `IBffAuthCookieStore` to BFF self-endpoint requests

6. **`Explore.Blazor/Extensions/HttpClientExtensions.cs`**
   - Added `BffCookieForwardingHandler` to `BffSelfClient` pipeline
   - Added `ResolveApiBaseUrl()` helper for Aspire service discovery

7. **`Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`**
   - Registered `ICircuitUserContext`, `IBffAuthCookieStore` as singletons
   - Registered `BffCookieForwardingHandler` as transient
   - Registered `TokenCircuitHandler` alongside `TenantCircuitHandler`

8. **`Explore.Blazor/Services/TokenRefreshCookieEvents.cs`**
   - Graceful degradation when no refresh token exists (don't sign out)
   - Added detailed diagnostic logging to `IsTokenExpiredOrNearExpiry`

9. **`Explore.Blazor/Services/DynamicAuthSchemeManager.cs`**
   - Added `refreshToken={HasRefreshToken}` diagnostic logging

10. **`Explore.Blazor.Client/Services/InstanceOnboardingService.cs`**
    - Uses `_httpClientFactory.CreateClient("BffClient")` for API calls
    - Uses `CreateBffSelfClient()` for refresh calls (now with cookie forwarding)

11. **Test files updated:**
    - `Explore.Blazor.Client.Tests/Services/CircuitAccessTokenServiceTests.cs`
    - `Explore.Blazor.IntegrationTests/Handlers/AccessTokenForwardingHandlerTests.cs`
    - `Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj`

### 10.2 Build & Test Results

- **Build**: 0 errors
- **Integration tests** (`Explore.Blazor.IntegrationTests`): 23/23 passed
- **Client tests** (`Explore.Blazor.Client.Tests`): 774 passed, 25 pre-existing failures (AnalyticsInitializer, MudBlazor v9 migration, AppearanceThemeService — none related to this change)

---

## 11. Why It Still Fails — Open Questions

Despite all fixes, the user **still** gets "Your session expired." We need to identify what was missed. Here are the remaining hypotheses:

### 11.1 Hypothesis A: AsyncLocal Does Not Flow as Expected

`CircuitUserContext` uses `AsyncLocal<string>`. Blazor Server circuits use a custom synchronization context (`CircuitSynchronizationContext`). It is NOT guaranteed that `AsyncLocal` values set in `OnCircuitOpenedAsync` will flow to the thread pool threads where `HttpClient.SendAsync` callbacks execute.

**Evidence needed**: Add explicit logging in `AccessTokenForwardingHandler` Strategy 4 to log whether `_circuitUserContext.UserId` is null or populated.

### 11.2 Hypothesis B: BffCookieForwardingHandler Doesn't Apply to the Right Client

`InstanceOnboardingService.RefreshAuthSessionAsync()` calls `CreateBffSelfClient()`. Need to verify:
1. Is `CreateBffSelfClient()` actually using the named client "BffSelfClient"?
2. Does the `BffCookieForwardingHandler` execute for that client?
3. Is the cookie header actually being sent in the `/bff/auth/refresh-session` request?

**Evidence needed**: Network trace or packet capture of the BFF self-call.

### 11.3 Hypothesis C: The `/bff/auth/refresh-session` Endpoint Is Broken

Even WITH cookies, the refresh endpoint might fail:
- `HandleRefreshSessionAsync` calls `ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)`
- Then calls `adminClaimsTransformation.EnrichPrincipalAsync` with `forceRefresh: true`
- Then re-signs the cookie
- It does NOT call the IdP token endpoint directly

It relies on `TokenRefreshCookieEvents.ValidatePrincipal` running during `AuthenticateAsync` to refresh the OIDC token. But:
- `ValidatePrincipal` only runs when the cookie is being validated (every request)
- If `ValidatePrincipal` detects an expired token AND has a refresh token, it calls the IdP
- But `ValidatePrincipal` runs in the ASP.NET Core auth pipeline, NOT in the Blazor circuit

The sequence in `RefreshAuthSessionAsync` is suspicious:
1. Call `/bff/auth/refresh-session` via `BffSelfClient`
2. BFF endpoint calls `AuthenticateAsync` → triggers `ValidatePrincipal` → MAY refresh token
3. Re-signs cookie
4. Extracts `access_token` from auth properties
5. Calls `tokenService?.SetToken(accessToken)`

But step 2 might NOT refresh the token if `ValidatePrincipal` decides the token is not expired yet (or is already expired beyond the buffer). And if it DOES refresh, the new token is stored in the scoped `ICircuitAccessTokenService` — but `AccessTokenForwardingHandler` might be in a different scope (see Hypothesis A).

### 11.4 Hypothesis D: Token Expires During Form Filling AND Refresh Also Fails

Keycloak default access token lifetime is 5 minutes. If the user takes >5 minutes to fill the form:
1. Token captured at circuit open is now expired
2. `RefreshAuthSessionAsync` is called before `CompleteAsync`
3. If refresh fails (for any reason), `preRefreshed` is `false`
4. `CompleteAsync` forwards the expired token
5. API returns 401
6. Retry with `forceRefresh: true` — if this ALSO fails, user sees "session expired"

**The real bug might be that `RefreshAuthSessionAsync` returns `true` even when it didn't actually refresh the token.** Need to check the implementation.

### 11.5 Hypothesis E: YARP Strips the Authorization Header

`YarpProxyExtensions.cs` has `ForwardBearerTokenAsync` which only sets `Authorization` when `context.HttpContext.GetTokenAsync("access_token")` returns a token. For `/api/InstanceOnboarding/complete`, `IsAnonymousOnboardingPath` returns false, so it doesn't strip the header.

But if `GetTokenAsync` returns null, it leaves the proxy request's `Authorization` header as-is (copied from incoming request by default transforms). This means if `AccessTokenForwardingHandler` sends a Bearer token, it SHOULD reach the API.

**However**, if YARP's default transforms DON'T copy the Authorization header from the incoming request (because it's a server-side HttpClient, not a browser request), then the token set by `AccessTokenForwardingHandler` might be lost.

### 11.6 Hypothesis F: The Token in `_tokenStore` Is Wrong or Expired

The static `_tokenStore` is populated by `CaptureAccessTokenAsync` middleware. This middleware runs on EVERY HTTP request. If the token was refreshed by `TokenRefreshCookieEvents.ValidatePrincipal` during a subsequent request, the store might have the OLD token.

**Wait** — `TokenRefreshCookieEvents.ValidatePrincipal` does update the store:
```csharp
var newAccessToken = result.Tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
if (!string.IsNullOrEmpty(newAccessToken))
{
    var tokenService = context.HttpContext.RequestServices.GetService<ICircuitAccessTokenService>();
    tokenService?.SetToken(newAccessToken);
}
```

But this calls `SetToken` on the HTTP-request-scoped `ICircuitAccessTokenService`, NOT the circuit-scoped one. The static store IS updated (via `SetToken`), so Strategy 4 SHOULD see the refreshed token.

Unless... `CaptureAccessTokenAsync` middleware runs AFTER `TokenRefreshCookieEvents.ValidatePrincipal` and OVERWRITES the refreshed token with the old one? No, `ValidatePrincipal` runs during `AuthenticateAsync`, which is before middleware.

### 11.7 Hypothesis G: Multiple 429 Rate Limits Break the Flow

Logs showed multiple `POST /api/InstanceOnboarding/validate-secret` returning 429 (rate limited). The user might be clicking submit multiple times. This is a UX issue but shouldn't cause the 401 on `complete`.

---

## 12. What We Know for Certain

1. **User IS authenticated at BFF level** — logs show `UserId="f02deef1-872a-44ae-bb9e-69b1638e9c99"` for both `/setup` and `/_blazor`
2. **Token IS available in static store** — `CaptureAccessTokenAsync` middleware populates it
3. **TokenRefreshCookieEvents IS registered** — and is now graceful when no refresh token
4. **Refresh token IS now present** — `secretLength=32`, `refreshToken=True` in logs
5. **Build and tests pass** — no compilation errors, no new test failures
6. **User still gets "session expired"** — the 401 on `complete` persists

---

## 13. Critical Gaps in Understanding

### 13.1 Is `AsyncLocal` Actually Working?

We ASSUME `AsyncLocal` set in `OnCircuitOpenedAsync` flows to `HttpClient.SendAsync` callback threads. This is NOT guaranteed in Blazor Server. The circuit's synchronization context might not flow to the thread pool.

**Test**: Add a log in `AccessTokenForwardingHandler`:
```csharp
_logger.LogDebug("[AccessTokenForwardingHandler] CircuitUserContext.UserId={UserId}, LocalToken={HasLocalToken}, StoreCount={StoreCount}",
    _circuitUserContext?.UserId ?? "NULL",
    !string.IsNullOrEmpty(_localToken),
    _tokenStore.Count);
```

### 13.2 What Does `RefreshAuthSessionAsync` Actually Do?

Read the current implementation and trace:
1. Does it use `BffSelfClient` with the cookie handler?
2. What does the BFF endpoint return?
3. Does `AuthenticateAsync` inside the endpoint trigger `ValidatePrincipal`?
4. Does `ValidatePrincipal` refresh the token?
5. Is the refreshed token propagated back?

### 13.3 Is the API Actually Receiving the Token?

Add logging on the API side:
- Log the `Authorization` header value on `InstanceOnboardingController.Complete`
- Check if it's present, malformed, or expired

### 13.4 Is There a Race Condition?

`InstanceOnboarding.razor` does:
```csharp
var preRefreshed = await InstanceOnboardingService.RefreshAuthSessionAsync();
var result = await InstanceOnboardingService.CompleteAsync(completion);
```

If `RefreshAuthSessionAsync` takes time and the token expires between these two calls, `CompleteAsync` gets an expired token. But `RefreshAuthSessionAsync` should have refreshed it...

Unless `RefreshAuthSessionAsync` doesn't actually refresh the OIDC token (see Hypothesis C).

---

## 14. Recommended Next Steps (In Priority Order)

### Step 1: Add Verbose Diagnostic Logging (30 min)
Add explicit logs to:
- `AccessTokenForwardingHandler` — log which strategy succeeded/failed, what userId was used, what token was forwarded
- `TokenCircuitHandler.OnCircuitOpenedAsync` — log what was captured (userId, cookie, token)
- `InstanceOnboardingService.RefreshAuthSessionAsync` — log request/response
- `InstanceOnboardingController.Complete` (API) — log Authorization header received

Run the app, reproduce the error, collect logs.

### Step 2: Verify AsyncLocal Flow (15 min)
In `AccessTokenForwardingHandler`, if `_circuitUserContext?.UserId` is null when it shouldn't be, `AsyncLocal` is not working. Alternative: use a **scoped** `ICircuitUserContext` (not singleton) and ensure `IHttpClientFactory` resolves it from the caller's scope. Or use a **hybrid** approach like `TenantRouteContextAccessor` (HttpContext.Items + instance field).

### Step 3: Verify BffSelfClient Cookie Forwarding (15 min)
Add logging in `BffCookieForwardingHandler` to confirm the cookie is being added. Or use a network interceptor.

### Step 4: Verify Refresh Endpoint Actually Refreshes (30 min)
Read `BffAuthEndpoints.cs` `HandleRefreshSessionAsync`. Trace the token refresh logic. Determine if it actually calls the IdP token endpoint or just re-signs the existing (possibly expired) cookie.

### Step 5: Consider Alternative Architecture (2 hours)
If all else fails, consider:
- **Server-side JWT caching**: Store the JWT in a server-side cache (Redis/memory) keyed by session ID, and have `AccessTokenForwardingHandler` look it up by session ID instead of userId.
- **Extend token lifetime**: Increase Keycloak access token lifetime to 30+ minutes for the setup flow only.
- **Use InteractiveWebAssembly for onboarding**: The user explicitly said NO, but if InteractiveServer auth is fundamentally broken for this flow, we may need to revisit.
- **Direct API call from Blazor**: Skip YARP for the complete call and call the API directly with a fresh token obtained via `GetTokenAsync` in the component (where `AuthenticationStateProvider` works).

---

## 15. Lessons Learned

1. **Blazor Server DI scopes are deceptively complex** — `IHttpClientFactory` handler pooling creates handlers in a factory-managed scope, not the caller's scope. This broke every assumption about scoped services.
2. **AsyncLocal is not a magic bullet** — it depends on the synchronization context. Blazor Server's `CircuitSynchronizationContext` might not flow to `HttpClient` callback threads.
3. **Token refresh in BFF is indirect** — `/bff/auth/refresh-session` doesn't call the IdP directly; it relies on `CookieAuthenticationEvents.ValidatePrincipal` being triggered by `AuthenticateAsync`. This is fragile.
4. **Logging is everything** — many hours were spent theorizing when a few strategic `LogDebug` calls would have revealed the actual state.
5. **Don't assume middleware runs in the same scope as the circuit** — `CaptureAccessTokenAsync` runs in the HTTP request scope; `TokenCircuitHandler` runs in the circuit scope; `AccessTokenForwardingHandler` runs in the factory scope.

---

## 16. Key Files Reference

| File | Purpose |
|------|---------|
| `Explore.Blazor/Services/CircuitAccessTokenService.cs` | AccessTokenForwardingHandler with 4 strategies |
| `Explore.Blazor/Services/TokenCircuitHandler.cs` | Captures token/userId/cookie on circuit open |
| `Explore.Blazor/Services/CircuitUserContext.cs` | AsyncLocal bridge for userId |
| `Explore.Blazor/Services/BffAuthCookieStore.cs` | AsyncLocal bridge for auth cookie |
| `Explore.Blazor/Services/BffCookieForwardingHandler.cs` | Adds cookie to BffSelfClient requests |
| `Explore.Blazor/Services/TokenRefreshCookieEvents.cs` | Cookie auth events, graceful when no refresh token |
| `Explore.Blazor/Services/DynamicAuthSchemeManager.cs` | OIDC scheme manager, diagnostic logging |
| `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` | Client service that calls complete + refresh |
| `Explore.Blazor/Pages/Setup/InstanceOnboarding.razor` | UI page with retry logic |
| `Explore.Blazor/Extensions/HttpClientExtensions.cs` | Client registrations, BffSelfClient setup |
| `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs` | DI registrations |
| `Explore.API/Controllers/InstanceOnboardingController.cs` | API endpoint that returns 401 |

---

*Report compiled from extensive multi-session investigation. All changes are in the working tree. Build passes. Tests pass (except pre-existing failures). User still experiences the error. This document is the handoff for the next investigation phase.*
