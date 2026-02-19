# Authentication State Management

> **Project-Agnostic Authentication State Management**
>
> Placeholders use `{Placeholder}` syntax - see [../../../../docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).
>
> **Note**: Use generic templates first. Keep project-specific examples as optional references.

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Blazor` | Blazor Server (BFF) project | `Explore.Blazor` |
| `{Project}.Blazor.Client` | Blazor WASM project | `Explore.Blazor.Client` |

---

This document describes how authentication state is managed and shared across the Blazor Hybrid application, especially between the Blazor Server (BFF) and Blazor WebAssembly (WASM) components.

---

## 1. Authentication State Serialization

To seamlessly transition user authentication from the server-rendered part to the client-rendered part of the Blazor Hybrid app, the authentication state is serialized.

**File**: `{Project}.Blazor/Program.cs` (Server-side Blazor)

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options => 
        options.SerializeAllClaims = true);  // ✅ Serialize all claims for WASM
                                            // This makes user claims available client-side
builder.Services.AddCascadingAuthenticationState(); // Makes AuthenticationStateProvider injectable
```

**File**: `{Project}.Blazor.Client/Program.cs` (Blazor WebAssembly)

```csharp
builder.Services.AddAuthorizationCore(); // Basic authorization services
builder.Services.AddCascadingAuthenticationState(); // Enables CascadingAuthenticationState
builder.Services.AddAuthenticationStateDeserialization(); // Deserializes the auth state from the server
```

### Key Points

*   **`AddAuthenticationStateSerialization`**: On the server, this captures the authenticated user's claims and includes them in the rendered HTML output.
*   **`AddAuthenticationStateDeserialization`**: On the client, this reads the serialized claims from the HTML and re-establishes the authentication state.
*   **`AddCascadingAuthenticationState`**: Makes the `AuthenticationStateProvider` and `Task<AuthenticationState>` available via `CascadingParameter` in components.

---

## 2. Accessing Authentication State in Components

Once serialized, the authentication state can be accessed in any Blazor component.

### Using `AuthorizeView` (Declarative)

The `AuthorizeView` component selectively displays UI content based on the user's authentication and authorization status.

```razor
<CascadingAuthenticationState>
    <AuthorizeView>
        <Authorized>
            <MudText>Welcome, @context.User.Identity?.Name!</MudText>
            <MudButton OnClick="CreateEvent">Create Event</MudButton>
        </Authorized>
        <NotAuthorized>
            <MudButton Href="/login">Login to Create Events</MudButton>
        </NotAuthorized>
        <Authorizing>
            <MudProgressCircular Indeterminate="true" Size="Size.Small" /> Loading authentication...
        </Authorizing>
    </AuthorizeView>
</CascadingAuthenticationState>
```

### Accessing Programmatically (Imperative)

For logic that depends on the authentication state, inject `AuthenticationStateProvider`.

```csharp
@inject AuthenticationStateProvider AuthenticationStateProvider

@code {
    private string? _currentUserName;
    private bool _isAuthenticated;
    private Guid? _currentUserId;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        _isAuthenticated = user.Identity?.IsAuthenticated == true;
        _currentUserName = user.Identity?.Name;
        _currentUserId = Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : (Guid?)null;

        // You can also access specific claims:
        var emailClaim = user.FindFirst("email")?.Value;
        var roles = user.FindAll("realm_access.roles").Select(c => c.Value).ToList();
    }
}
```

### Key Claims

When accessing claims, be aware of the common types and the fallback pattern for User ID:

*   `context.User.Identity?.Name`: Typically the `preferred_username` or `name` claim.
*   `user.FindFirst("sub")?.Value`: The standard OIDC subject (user ID) claim.
*   `user.FindFirst("email")?.Value`: The user's email address.
*   `user.FindAll("realm_access.roles").Select(c => c.Value)`: Roles assigned by your identity provider.

*For the critical User ID extraction fallback pattern, refer to the `auth-patterns` skill.*

---

## 3. WASM Message Handlers for Authentication

In the Blazor WASM client (`{Project}.Blazor.Client`), custom `DelegatingHandler`s are used to manage credentials (cookies) and handle unauthorized responses.

### `BrowserCredentialsMessageHandler`

Ensures that credentials (the session cookie) are included with every HTTP request made by the WASM client to the BFF.

**File**: `{Project}.Blazor.Client/Services/BrowserCredentialsMessageHandler.cs`

```csharp
namespace {Project}.Blazor.Client.Services;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

public class BrowserCredentialsMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // ✅ CRITICAL: Include cookies with requests to the BFF
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.SetBrowserRequestMode(BrowserRequestMode.SameOrigin);
        return base.SendAsync(request, cancellationToken);
    }
}
```

### `BffUnauthorizedHandler`

Intercepts `401 Unauthorized` responses from the BFF and redirects the user to the login page while avoiding redirect loops.

**File**: `{Project}.Blazor.Client/Services/BffUnauthorizedHandler.cs`

```csharp
namespace {Project}.Blazor.Client.Services;

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

public class BffUnauthorizedHandler : DelegatingHandler
{
    private readonly NavigationManager _navigationManager;

    public BffUnauthorizedHandler(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var currentRelativePath = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);

            // Avoid loops on auth endpoints and login page
            if (path.StartsWith("/login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/logout", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) ||
                currentRelativePath.StartsWith("login", StringComparison.OrdinalIgnoreCase))
            {
                return response;
            }

            // Optionally allow selected anonymous endpoints to stay non-interruptive
            var isAnonymousApi = path.StartsWith("/api/public", StringComparison.OrdinalIgnoreCase);
            if (!isAnonymousApi)
            {
                _navigationManager.NavigateTo(
                    "/login?returnUrl=" + Uri.EscapeDataString(_navigationManager.Uri),
                    forceLoad: true);
            }
        }

        return response;
    }
}
```

### Registration of WASM Message Handlers

**File**: `{Project}.Blazor.Client/Program.cs`

```csharp
builder.Services.AddTransient<BrowserCredentialsMessageHandler>();
builder.Services.AddTransient<BffUnauthorizedHandler>();

builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<BrowserCredentialsMessageHandler>()  // Attaches cookies
.AddHttpMessageHandler<BffUnauthorizedHandler>();           // Handles 401 redirects
```

---

**Related Documentation**:
- [token-forwarding.md](token-forwarding.md) - Details on server-side token management.
- [`auth-patterns`](../../auth-patterns/SKILL.md) - Comprehensive authentication patterns.
