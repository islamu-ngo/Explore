ABOUTME: Lean state management rules for Blazor components in this project.
ABOUTME: Focuses on when to use component state, parameters, and services.

# State Management (Lean)

## Default Pattern
- Component-local state for UI-only data.
- `[Parameter]` + `EventCallback<T>` for parent/child communication.

## Shared State
- Use scoped services for cross-component state (per user/session).
- Singletons only for immutable/global configuration.

## Auth State
- Use `AuthenticationStateProvider` or `AuthorizeView` for auth-aware UI.
- Do not bypass BFF boundaries with direct API calls from UI.

## Prerendering
- Guard side effects; lifecycle runs twice with prerender.

## Related
- [component-design.md](component-design.md)
- [render-modes.md](render-modes.md)
