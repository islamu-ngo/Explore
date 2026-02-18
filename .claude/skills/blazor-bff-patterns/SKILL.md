---
name: blazor-bff-patterns
description: Backend for Frontend (BFF) patterns for Blazor applications. Covers YARP proxy, token forwarding, cookie-based auth, and service layer integration.
type: domain
enforcement: suggest
priority: high
---

# Blazor BFF (Backend for Frontend) Patterns

> **Project-Agnostic BFF Patterns for Blazor Hybrid Applications**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Blazor` | Blazor Server (BFF) project | `Explore.Blazor` |
| `{Project}.Blazor.Client` | Blazor WASM project | `Explore.Blazor.Client` |
| `{Project}.API` | Backend API project | `Explore.API` |
| `{Entity}` | Main entity (singular) | `Event` |
| `{project}` | camelCase project name | `explore` |
| `{IdType}` | Primary key type | `Guid` |

---

## 🎯 Purpose

Provides patterns for implementing the BFF architecture in Blazor hybrid applications. Covers YARP reverse proxy, token forwarding, authentication state management, and service layer design.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "bff", "backend for frontend", "yarp", "proxy", "token forwarding", "cookie auth", "authentication state"
- File patterns: `**/*Blazor/Program.cs`, `**/*Blazor/Services/**/*.cs`, `**/*Blazor.Client/Services/**/*.cs`, `**/Extensions/**/*.cs`
- Content patterns: YARP configuration, authentication handlers, service registration

## 🏗️ BFF Architecture

**Generic Diagram**:

```mermaid
graph TD
    A[Browser (WASM)] -- HTTP + Cookies --> B[{Project}.Blazor (BFF)]
    B -- HTTP + Bearer Token --> C[{Project}.API]

    subgraph {Project}.Blazor (BFF)
        B1(OIDC Authentication)
        B2(Cookie-based session)
        B3(YARP reverse proxy)
        B4(Token extraction from cookie)
        B5(Bearer token attachment)
        B6(CSRF protection)
    end

    subgraph {Project}.API
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

*Substitute `{Project}` with your solution name (e.g., Explore, OrderSystem, MyApp).*

**Implementation Example: ISLAMU Event**:

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
| [interactiveauto-yarp-security.md](resources/interactiveauto-yarp-security.md) | InteractiveAuto + YARP/BFF production security and middleware ordering |

## ⚡ Quick Reference

### 1. YARP Reverse Proxy Configuration

**Purpose**: Forward API requests from BFF to backend API with token attachment.

**Generic Template**:

```csharp
// File: {Project}.Blazor/Program.cs
var {project}ApiBaseUrl = builder.Configuration["{Project}Api:BaseUrl"] ?? "https://localhost:7039/";

var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "{project}-api",
        ClusterId = "{project}-api",
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
        ClusterId = "{project}-api",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new() { Address = {project}ApiBaseUrl }
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

**Implementation Example: ISLAMU Event**:

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

**Purpose**: Configure OIDC authentication and cookie-based session management for the Blazor Server BFF.

**Generic Template** (works with any OIDC provider):

```csharp
// File: {Project}.Blazor/Program.cs
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
    options.Authority = builder.Configuration["OIDC:Authority"];
    options.ClientId = builder.Configuration["OIDC:ClientId"];
    options.ClientSecret = builder.Configuration["OIDC:ClientSecret"];
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

**Implementation Example: ISLAMU Event** (uses Keycloak):

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

**Generic Template**:

```csharp
// File: {Project}.Blazor/Services/CircuitAccessTokenService.cs (Scoped service)
public interface ICircuitAccessTokenService
{
    void SetAccessToken(string? token);
    string? GetAccessToken();
}

// File: {Project}.Blazor/Services/AccessTokenForwardingHandler.cs (DelegatingHandler)
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
builder.Services.AddHttpClient<I{Entity}ApiClient, {Entity}ApiClient>(client =>
{
    client.BaseAddress = new Uri({project}ApiBaseUrl);
})
.AddHttpMessageHandler<AccessTokenForwardingHandler>();  // Attach token
```

**Implementation Example: ISLAMU Event**:

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

**Generic Template**:

```csharp
// File: {Project}.Blazor.Client/Services/{Entity}Service.cs
public interface I{Entity}Service { /* ... */ }

public class {Entity}Service : I{Entity}Service
{
    private readonly I{Entity}ApiClient _apiClient; // NSwag-generated
    private readonly ILogger<{Entity}Service> _logger;

    public {Entity}Service(I{Entity}ApiClient apiClient, ILogger<{Entity}Service> logger) { /* ... */ }

    public async Task<ICollection<{Entity}ListDto>> GetAll{Entities}Async()
    {
        try
        {
            return await _apiClient.{Entity}AllAsync() ?? new List<{Entity}ListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "API error: {StatusCode}", ex.StatusCode);
            return new List<{Entity}ListDto>();  // Safe default
        }
    }
}
```

**Implementation Example: ISLAMU Event**:

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
-   ✅ **DO** enforce anti-forgery validation for state-changing requests.
-   ✅ **DO** configure forwarded headers correctly when running behind reverse proxies.

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
