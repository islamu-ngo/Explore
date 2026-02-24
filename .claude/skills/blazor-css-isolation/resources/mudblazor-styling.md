ABOUTME: Minimal pattern for styling MudBlazor with CSS isolation.
ABOUTME: Emphasizes using component props first, then classes.

# MudBlazor Styling with Isolation

Combine MudBlazor component `Class` parameters with isolated CSS and BEM names.

## Rules
- Prefer MudBlazor props first (`Color`, `Variant`, `Size`).
- Add classes for app-specific styling (BEM).
- Use `::deep` only for unreachable internals.
