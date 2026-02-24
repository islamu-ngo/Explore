ABOUTME: Minimal Blazor component design rules for this project.
ABOUTME: Focuses on structure, parameters, and lifecycle caveats.

# Component Design (Lean)

## Core Rules
- Keep component state private; expose only `[Parameter]` inputs and `EventCallback` outputs.
- Prefer composition (child components) over large monoliths.
- Use code-behind (`.razor.cs`) only when logic is non-trivial.
- Initialize lists/objects to avoid nulls; use `?` for optional data.

## Parameters & Events
- Parameters are one-way input; child never mutates parent state directly.
- Use `EventCallback<T>` for child → parent changes.
- Avoid two-way binding on complex models unless necessary.

## Lifecycle Notes
- `OnInitialized{Async}` can run twice with prerendering.
- Put side effects behind guards; prefer `OnAfterRenderAsync(firstRender)` when needed.

## Layout & CSS
- Use `.razor.css` (CSS isolation) for component-specific styles.
- Only use `::deep` for styling internal elements of MudBlazor components.

## Related
- [state-management.md](state-management.md)
- [render-modes.md](render-modes.md)
