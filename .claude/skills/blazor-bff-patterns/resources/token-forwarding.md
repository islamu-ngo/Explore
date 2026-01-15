# Token Forwarding

This document details how access tokens are forwarded from the Blazor BFF (Backend-for-Frontend) to the backend API, especially in the context of Blazor Hybrid rendering.

---

## 1. YARP Request Transform (BFF to API)

This is the primary mechanism for forwarding the JWT from the user's session cookie to the backend API. It occurs server-side within the BFF.

**File**: `Explore.Blazor/Program.cs`

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
*   **`transformContext.ProxyRequest.Headers.Authorization`**: The extracted token is then added to the `Authorization` header of the request being forwarded to the `Explore.API` backend.
*   This transform ensures that the `Explore.API` always receives a standard JWT Bearer token, making it agnostic to the BFF's cookie-based session management.

---

## 2. Token Forwarding for InteractiveServer Components

In a Blazor Hybrid application, components running in `InteractiveServer` mode (which use a SignalR circuit) do not have direct access to `HttpContext`. Therefore, a separate mechanism is needed to ensure the access token is available to HTTP clients used by these components.

### `CircuitAccessTokenService` (Scoped Service)

This service holds the access token for the duration of a Blazor circuit.

**File**: `Explore.Blazor/Services/CircuitAccessTokenService.cs`

```csharp
namespace Explore.Blazor.Services;

public interface ICircuitAccessTokenService
{
    void SetAccessToken(string? token);
    string? GetAccessToken();
}

public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    private string? _accessToken; // Stores the token for the current circuit

    public void SetAccessToken(string? token)
    {
        _accessToken = token;
    }

    public string? GetAccessToken()
    {
        return _accessToken;
    }
}
```

### `AccessTokenForwardingHandler` (Delegating Handler)

This `DelegatingHandler` attaches the token from `CircuitAccessTokenService` to outgoing HTTP requests.

**File**: `Explore.Blazor/Services/AccessTokenForwardingHandler.cs`

```csharp
namespace Explore.Blazor.Services;

using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly ICircuitAccessTokenService _tokenService;

    public AccessTokenForwardingHandler(ICircuitAccessTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var token = _tokenService.GetAccessToken(); // Get token from the circuit service
        if (!string.IsNullOrEmpty(token))
        {
            // Attach the token as a Bearer header
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Registration and Usage

**File**: `Explore.Blazor/Program.cs`

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

**File**: `Explore.Blazor/Components/App.razor` (or a similar root component)

```razor
@inject IHttpContextAccessor HttpContextAccessor
@inject ICircuitAccessTokenService CircuitAccessTokenService // Inject the service

@code {
    protected override async Task OnInitializedAsync()
    {
        // Capture token during the initial HTTP request context (before SignalR circuit starts)
        var accessToken = await HttpContextAccessor.HttpContext?.GetTokenAsync("access_token");
        // Store it in the scoped service for later use by interactive components
        CircuitAccessTokenService.SetAccessToken(accessToken);
    }
}

@* This cascading value is no longer strictly needed for this pattern, but demonstrates one way to share state if desired *@
<CascadingValue Value="accessToken" Name="AccessToken">
    <Routes @rendermode="InteractiveAuto" />
</CascadingValue>
```

---

**Related Documentation**:
- [bff-configuration.md](bff-configuration.md) - General YARP configuration.
- [auth-state-management.md](auth-state-management.md) - Authentication state serialization for WASM.
