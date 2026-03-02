ABOUTME: State management rules for Blazor components (MudBlazor v9).
ABOUTME: Focuses on component state, parameters, services, and v9 ParameterState rules.

# State Management (v9)

## Default Pattern
- Component-local state for UI-only data.
- `[Parameter]` + `EventCallback<T>` for parent/child communication.

## Shared State
- Use scoped services for cross-component state (per user/session).
- Singletons only for immutable/global configuration.
- **v9**: `MudGlobal` theming defaults are removed — do not rely on static global state for component defaults.

## v9 ParameterState Rules (Custom MudBlazor Components Only)
- `ParameterState<T>` is internal to MudBlazor base components; app-level components use standard `[Parameter]` + `EventCallback`.
- Roslyn analyzers (MUD0010/0011/0012) enforce correct usage in custom MudBlazor components.
- Immutable types: `Range<T>` and `DateRange` are now immutable — create new instances instead of mutating `Start`/`End`.

## Auth State
- Use `AuthenticationStateProvider` or `AuthorizeView` for auth-aware UI.
- Do not bypass BFF boundaries with direct API calls from UI.

## Prerendering
- Guard side effects; lifecycle runs twice with prerender.

## Related
- [component-design.md](component-design.md)
- [render-modes.md](render-modes.md)
