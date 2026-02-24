ABOUTME: Routing rules when using Blazouter in this project.
ABOUTME: Focuses on route config, guards, and auth shims.

# Blazouter Routing Patterns

Use this when a Blazor app uses Blazouter instead of, or alongside, native `@page` routing.

## Core Rules
- Centralize routes (ex: `Routes.razor`) with `RouteConfig` entries.
- Attach guards in route definitions (auth/authz boundaries).
- Stable param names; read via `RouterStateService.GetParam(...)`.
- Register Blazouter in DI + endpoint mapping before using guards.
- BFF login/logout shims: `/login` → `/auth/challenge`, `/logout` → `/auth/signout`.

## Guards
- Use `IRouteGuard` for auth/admin routes.
- Single responsibility per guard; predictable redirects.
- Prefer `AuthenticationStateProvider` for cross-render-mode compatibility.

## Interop
- If `@page` exists, document authoritative router per route set.
- Do not duplicate the same logical route in both systems.
