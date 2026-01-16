name: blazor-bff-patterns
description: Backend for Frontend (BFF) patterns for ISLAMU Event Blazor. Covers YARP proxy, token forwarding, cookie-based auth, and service layer integration.
type: domain
enforcement: suggest
priority: high
---

# Blazor BFF (Backend for Frontend) Patterns

## 🎯 Purpose

Provides patterns for implementing the BFF architecture in ISLAMU Event's Blazor frontend. Covers YARP reverse proxy, token forwarding, authentication state management, and service layer design.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "bff", "backend for frontend", "yarp", "proxy", "token forwarding", "cookie auth", "authentication state"
- File patterns: `**/Explore.Blazor/Program.cs`, `**/Services/**/*.cs`, `**/Extensions/**/*.cs`
- Content patterns: YARP configuration, authentication handlers, service registration

## 🏗️ BFF Architecture

```mermaid
graph TD
    A[Browser (WASM)] -- HTTP + Cookies --> B[Explore.Blazor (BFF)]
    B -- HTTP + Bearer Token --> C[Explore.API]

    subgraph Explore.Blazor (BFF)
        B1(OIDC Authentication (Keycloak))
        B2(Cookie-based session)
        B3(YARP reverse proxy)
        B4(Token extraction from cookie)
        B5(Bearer token attachment)
        B6(CSRF protection)
    end

    subgraph Explore.API
        C1(JWT Bearer authentication)
        C2(MediatR CQRS handlers)
        C3(Returns DTOs)
    end

    B --- B1
    B --- B2
    B --- B3
    B --- B4
    B --- B5
    B --- B6
```

## 📚 Resources

*For more detailed examples, refer to the `resources/` folder within this skill.*

| Resource | Description |
|----------|-------------|
| [bff-configuration.md](resources/bff-configuration.md) | YARP setup, route configuration |
| [token-forwarding.md](resources/token-forwarding.md) | Access token extraction and forwarding |
| [auth-state-management.md](resources/auth-state-management.md) | Authentication state serialization |
| [service-layer-patterns.md](resources/service-layer-patterns.md) | Service wrappers for API clients |

## ⚡ Quick Reference

### 1. YARP Reverse Proxy Configuration

**Purpose**: Forward API requests from BFF to backend API with token attachment.

```csharp
// File: Explore.Blazor/Program.cs
var exploreApiBaseUrl = builder.Configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/";

var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "explore-api",
        ClusterId = "explore-api",
        Match = new RouteMatch
        {
            Path = "/api/v1/{**catchall}"  // Catch all API routes
        }
    }
};

var proxyClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "explore-api",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = exploreApiBaseUrl }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(proxyRoutes, proxyClusters)
    .AddTransforms(context =>
    {
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            var token = await httpContext.GetTokenAsync("access_token"); // Get token from cookie
            if (!string.IsNullOrEmpty(token))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token); // Attach as Bearer token
            }
        });
    });
```

*For more details, see [bff-configuration.md](resources/bff-configuration.md).*

### 2. Authentication Configuration (OIDC + Cookies)

**Purpose**: Configure OIDC authentication with Keycloak and cookie-based session management for the Blazor Server BFF.

```csharp
// File: Explore.Blazor/Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;  // Store tokens in cookie
    options.GetClaimsFromUserInfoEndpoint = true;
    
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("offline_access");  // Request refresh token
});
```

*For more details, see [auth-state-management.md](resources/auth-state-management.md).*

### 3. Token Forwarding for InteractiveServer Components

**Purpose**: In Blazor Hybrid apps, `HttpContext` is not always available in interactive components. Access tokens need to be explicitly forwarded to HTTP clients.

```csharp
// File: Explore.Blazor/Services/CircuitAccessTokenService.cs (Scoped service)
public interface ICircuitAccessTokenService
{
    void SetAccessToken(string? token);
    string? GetAccessToken();
}
// File: Explore.Blazor/Services/AccessTokenForwardingHandler.cs (DelegatingHandler)
public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly ICircuitAccessTokenService _tokenService;
    public AccessTokenForwardingHandler(ICircuitAccessTokenService tokenService) => _tokenService = tokenService;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _tokenService.GetAccessToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return await base.SendAsync(request, cancellationToken);
    }
}
// Registration in Program.cs
builder.Services.AddScoped<ICircuitAccessTokenService, CircuitAccessTokenService>();
builder.Services.AddTransient<AccessTokenForwardingHandler>();
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri(exploreApiBaseUrl);
})
.AddHttpMessageHandler<AccessTokenForwardingHandler>();  // Attach token
```

*For more details, see [token-forwarding.md](resources/token-forwarding.md).*

### 4. Service Layer Pattern

**Purpose**: Wrap NSwag-generated API clients with a service layer for error handling, logging, and providing safe defaults.

```csharp
// File: Explore.Blazor.Client/Services/EventService.cs
public interface IEventService { /* ... */ }

public class EventService : IEventService
{
    private readonly IEventApiClient _apiClient; // NSwag-generated
    private readonly ILogger<EventService> _logger;

    public EventService(IEventApiClient apiClient, ILogger<EventService> logger) { /* ... */ }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            return await _apiClient.EventAllAsync() ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();  // Safe default
        }
    }
}
```

*For more details, see [service-layer-patterns.md](resources/service-layer-patterns.md).*

## ✅ Do's

-   ✅ **DO** use YARP for API proxying (not custom middleware).
-   ✅ **DO** store tokens in server-side cookies (not `localStorage`).
-   ✅ **DO** forward Bearer token to API in YARP transform.
-   ✅ **DO** use a service layer to wrap API clients.
-   ✅ **DO** handle errors and return safe defaults in the service layer.
-   ✅ **DO** log API operations for debugging.
-   ✅ **DO** use `BrowserRequestCredentials.Include` for WASM requests.
-   ✅ **DO** redirect to login on `401 Unauthorized` responses.
-   ✅ **DO** serialize authentication state for WASM components.

## ❌ Don'ts

-   ❌ **DON'T** expose tokens to the browser.
-   ❌ **DON'T** call API directly from WASM; always go through the BFF.
-   ❌ **DON'T** store sensitive data in `localStorage`.
-   ❌ **DON'T** bypass the service layer; use services, not API client directly.
-   ❌ **DON'T** forget CSRF protection (use antiforgery tokens).
-   ❌ **DON'T** hardcode API URLs; use configuration.

---

**Related Documentation**:
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md) - Overall system architecture.
- [`auth-patterns`](../auth-patterns/SKILL.md) - Authentication and authorization patterns.