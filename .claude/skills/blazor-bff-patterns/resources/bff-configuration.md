# BFF Configuration

This document details the configuration for the Backend-for-Frontend (BFF) pattern using YARP (Yet Another Reverse Proxy) in the `Explore.Blazor` project.

---

## 1. YARP Reverse Proxy Setup

YARP is used to proxy API calls from the Blazor client to the `Explore.API` backend. This is crucial for the BFF pattern as it allows for server-side token management.

**File**: `Explore.Blazor/Program.cs`

```csharp
// Define the base URL for the backend API
var exploreApiBaseUrl = builder.Configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/";

// Configure proxy routes: requests matching this path will be forwarded
var proxyRoutes = new[]
{
    new RouteConfig
    {
        RouteId = "explore-api", // Unique ID for this route
        ClusterId = "explore-api", // ID of the cluster to forward to
        Match = new RouteMatch
        {
            // All requests starting with /api/v1/ will be proxied
            Path = "/api/v1/{**catchall}"
        }
    }
};

// Configure proxy clusters: defines the actual backend services
var proxyClusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "explore-api", // Must match the ClusterId in RouteConfig
        Destinations = new Dictionary<string, DestinationConfig>
        {
            // Define one or more destinations for the cluster
            ["primary"] = new() { Address = exploreApiBaseUrl }
        }
    }
};

// Add YARP to the service collection and load configuration
builder.Services.AddReverseProxy()
    .LoadFromMemory(proxyRoutes, proxyClusters)
    .AddTransforms(context =>
    {
        // This transform is CRITICAL for forwarding the JWT
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            // Get the access token from the secure cookie
            var token = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(token))
            {
                // Attach the token as a Bearer header to the proxied request
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        });
    });
```

### Key Points

*   **`exploreApiBaseUrl`**: Should be configured in `appsettings.json` or environment variables.
*   **`Path = "/api/v1/{**catchall}"`**: This route captures all requests intended for the backend API.
*   **`AddTransforms`**: This is where the magic happens. It intercepts the outgoing request from the BFF to the API, extracts the JWT from the user's session cookie (which was obtained during OIDC login), and attaches it as an `Authorization: Bearer` header. This makes the backend API oblivious to the cookie-based authentication, only seeing standard JWTs.

---

## 2. Authentication Endpoint Mapping

These endpoints handle the login and logout flows, leveraging ASP.NET Core's built-in authentication handlers.

**File**: `Explore.Blazor/Program.cs`

```csharp
// Endpoint for initiating the login process
app.MapGet("/login", async ctx =>
{
    var returnUrl = ctx.Request.Query["returnUrl"].ToString();
    // Challenge the user with the OpenID Connect scheme, which redirects to Keycloak
    await ctx.ChallengeAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties
        {
            // After successful login, redirect to the original URL or home
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl
        }
    );
});

// Endpoint for initiating the logout process
app.MapGet("/logout", async ctx =>
{
    // Sign out from cookie authentication
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    // Sign out from OpenID Connect, which typically redirects to Keycloak's logout endpoint
    await ctx.SignOutAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" }
    );
});

// Optional: An endpoint to expose basic user information to the client
// This prevents the client from needing to parse claims from the cookie directly
app.MapGet("/bff/me", (HttpContext ctx) =>
{
    if (ctx.User.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized(); // Return 401 if not authenticated
    }

    // Return selected claims to the client
    return Results.Ok(new
    {
        Name = ctx.User.Identity?.Name,
        Claims = ctx.User.Claims.Select(c => new { c.Type, c.Value })
    });
});
```

---

## 3. NSwag API Client Configuration

NSwag generates C# clients for your OpenAPI/Swagger API. These clients need to be configured correctly in the BFF (`Explore.Blazor`) and the WASM client (`Explore.Blazor.Client`).

### BFF (Blazor Server) Configuration

In the Blazor Server component of the hybrid app, the NSwag client can directly access the backend API because it's running on the server. The `AccessTokenForwardingHandler` ensures JWTs are sent.

**File**: `Explore.Blazor/Program.cs`

```csharp
var exploreApiBaseUrl = builder.Configuration["ExploreApi:BaseUrl"] ?? "https://localhost:7039/";

// Register the NSwag generated API client
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri(exploreApiBaseUrl); // Direct API base URL
})
.AddHttpMessageHandler<AccessTokenForwardingHandler>() // Custom handler to attach JWT
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // Custom HttpClientHandler to allow self-signed certs in development
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback = 
            (message, cert, chain, errors) =>
            {
                var isLocalhost = message.RequestUri?.Host.Contains("localhost") ?? false;
                return isLocalhost || errors == System.Net.Security.SslPolicyErrors.None;
            };
    }
    return handler;
});
```

### WASM Client (`Explore.Blazor.Client`) Configuration

In the Blazor WebAssembly client, the NSwag client should **NOT** directly call the backend API. Instead, it calls the **BFF itself**, and the BFF's YARP proxy then forwards the request.

**File**: `Explore.Blazor.Client/Program.cs`

```csharp
// Register the NSwag generated API client for the WASM client
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    // Base address is the WASM app's own base address, which routes to the BFF
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BrowserCredentialsMessageHandler>() // Attaches cookies to outgoing requests
.AddHttpMessageHandler<BffUnauthorizedHandler>();           // Handles 401 Unauthorized responses
```

---

**Related Documentation**:
- [token-forwarding.md](token-forwarding.md) - Details on `AccessTokenForwardingHandler`.
- [auth-state-management.md](auth-state-management.md) - Context for `BrowserCredentialsMessageHandler` and `BffUnauthorizedHandler`.
