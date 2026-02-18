# Blazouter Routing Patterns

Use this when a Blazor app uses Blazouter instead of, or alongside, native `@page` routing.

## Core Principles

- Keep routes in one place (for example `Routes.razor`) via `RouteConfig` entries.
- Apply route guards at route definition time for authn/authz boundaries.
- Keep route parameter names stable and consume them via `RouterStateService.GetParam(...)`.
- Register routing in DI and endpoint mapping (`AddBlazouter()` + `AddBlazouterSupport()`) before expecting route transitions/guards to work.
- Keep login/logout as client shim routes that force-load server auth endpoints when using BFF (`/login` -> `/auth/challenge`, `/logout` -> `/auth/signout`).

## Required Setup (Blazor Web App Hybrid)

```csharp
// Server Program.cs
builder.Services.AddBlazouter();

app.MapRazorComponents<App>()
    .AddBlazouterSupport()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode();

// WASM Client Program.cs
builder.Services.AddBlazouter();
builder.Services.AddScoped<AuthenticatedRouteGuard>();
builder.Services.AddScoped<AdminRouteGuard>();
builder.Services.AddScoped<OrgAdminRouteGuard>();
```

## Route Configuration Pattern

```razor
<Blazouter.Components.Router Routes="@_routes" DefaultLayout="typeof(MainLayout)">
    <NotFound>
        <LayoutView Layout="typeof(MainLayout)">
            <p>Not found.</p>
        </LayoutView>
    </NotFound>
</Blazouter.Components.Router>

@code {
    private List<RouteConfig> _routes =
    [
        new RouteConfig { Path = "/events", Component = typeof(EventList), Guards = RequireAuthenticated(), Transition = RouteTransition.Fade },
        new RouteConfig { Path = "/event/detail/:eventId", Component = typeof(EventDetail), Guards = RequireAuthenticated(), Transition = RouteTransition.Fade },
        new RouteConfig { Path = "/startup", Component = typeof(StartupGate), Transition = RouteTransition.None, EnableCache = false }
    ];
}
```

## Parameter Access Pattern

```csharp
[Inject] private RouterStateService RouterState { get; set; } = default!;

var idRaw = RouterState.GetParam("eventId");
if (!Guid.TryParse(idRaw, out var eventId))
{
    // Handle bad route parameter
}
```

## Guard Pattern

- Use `IRouteGuard` implementations for authenticated/admin/org-admin routes.
- Keep guard logic focused (one responsibility per guard).
- Use predictable redirect behavior for unauthorized access.
- Prefer `AuthenticationStateProvider` in guards so logic works across InteractiveServer and InteractiveWebAssembly.

## Interop with Native Routing

- If `@page` directives exist, document which router is authoritative for each route set.
- Avoid duplicating the same logical route in both systems.
