# Token Forwarding

> **Project-Agnostic Token Forwarding Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [../../../../docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).
>
> **Note**: Use generic templates first. Keep project-specific examples as optional references.

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Blazor` | Blazor Server (BFF) project | `Explore.Blazor` |
| `{Entity}` | Main entity | `Event` |

---

This document details how access tokens are forwarded from the Blazor BFF (Backend-for-Frontend) to the backend API, especially in the context of Blazor Hybrid rendering.

---

## 1. YARP Request Transform (BFF to API)

This is the primary mechanism for forwarding the JWT from the user's session cookie to the backend API. It occurs server-side within the BFF.

**File**: `{Project}.Blazor/Program.cs`

```csharp
builder.Services.AddReverseProxy()
    // ... other YARP configurations
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            // Extract the access token from the secure, HttpOnly cookie
            // This token was saved by the OIDC authentication middleware
            var token = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(token))
            {
                // Attach the access token as a Bearer header to the proxied request
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        });
    });
```

### Key Points

*   **`httpContext.GetTokenAsync("access_token")`**: This extension method from `Microsoft.AspNetCore.Authentication` securely retrieves the access token that was stored in the cookie by the OIDC middleware during login.
*   **`transformContext.ProxyRequest.Headers.Authorization`**: The extracted token is then added to the `Authorization` header of the request being forwarded to the `{Project}.API` backend.
*   This transform ensures that the `{Project}.API` always receives a standard JWT Bearer token, making it agnostic to the BFF's cookie-based session management.

---

## 2. Token Forwarding for InteractiveServer Components

In a Blazor Hybrid application, components running in `InteractiveServer` mode (which use a SignalR circuit) do not have direct access to `HttpContext`. Therefore, a separate mechanism is needed to ensure the access token is available to HTTP clients used by these components.

### `CircuitAccessTokenService` (Scoped Service)

This service stores token context for InteractiveServer calls. In robust implementations, it can include a per-user fallback cache for requests where `HttpContext` is unavailable.

**File**: `{Project}.Blazor/Services/CircuitAccessTokenService.cs`

```csharp
namespace {Project}.Blazor.Services;

public interface ICircuitAccessTokenService
{
    void SetToken(string? token);
    string? GetStoredToken();
}

public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    private string? _accessToken;

    public void SetToken(string? token) => _accessToken = token;
    public string? GetStoredToken() => _accessToken;

    // Optional for hybrid/SignalR-heavy apps:
    // static per-user token store keyed by user id (for HttpContext-null fallback).
}
```

### `AccessTokenForwardingHandler` (Delegating Handler)

This `DelegatingHandler` attaches the token to outgoing HTTP requests. In hybrid apps, use a fallback chain for InteractiveServer requests where `HttpContext` may be null.

**File**: `{Project}.Blazor/Services/CircuitAccessTokenService.cs` (same file in this repo)

```csharp
namespace {Project}.Blazor.Services;

using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly ICircuitAccessTokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccessTokenForwardingHandler(
        ICircuitAccessTokenService tokenService,
        IHttpContextAccessor httpContextAccessor)
    {
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // Strategy 1: try HttpContext token
        var token = _httpContextAccessor.HttpContext is not null
            ? await _httpContextAccessor.HttpContext.GetTokenAsync("access_token")
            : null;

        // Strategy 2: fallback to scoped circuit token
        if (string.IsNullOrEmpty(token))
        {
            token = _tokenService.GetStoredToken();
        }

        // Strategy 3 (optional): fallback to static per-user cache

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Registration and Usage

**File**: `{Project}.Blazor/Program.cs`

```csharp
// Register the scoped service for the circuit and the delegating handler
builder.Services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
builder.Services.AddTransient<AccessTokenForwardingHandler>();

// Configure the HTTP client to use the forwarding handler
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri(exploreApiBaseUrl);
})
.AddHttpMessageHandler<AccessTokenForwardingHandler>(); // Attach the handler
```

**File**: `{Project}.Blazor/Program.cs` (middleware stage)

```csharp
app.Use(async (ctx, next) =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var accessToken = await ctx.GetTokenAsync("access_token");
        if (!string.IsNullOrEmpty(accessToken))
        {
            var tokenService = ctx.RequestServices.GetService<ICircuitAccessTokenService>();
            tokenService?.SetToken(accessToken);
        }
    }

    await next();
});
```

---

**Related Documentation**:
- [bff-configuration.md](bff-configuration.md) - General YARP configuration.
- [auth-state-management.md](auth-state-management.md) - Authentication state serialization for WASM.
