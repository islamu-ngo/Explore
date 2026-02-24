ABOUTME: Minimal theming rules for MudBlazor in this project.
ABOUTME: Covers dark/light toggle and where theme config lives.

# Theming (Lean)

## Rules
- Theme lives in a single `MudTheme` definition.
- `MainLayout` hosts `MudThemeProvider` and applies dark/light state.
- Persist theme preference via cookie/local storage (BFF-safe).

## CSS
- Use MudBlazor CSS variables for overrides.
- Keep global overrides minimal; prefer component `.razor.css`.

## Related
- [bem-methodology.md](bem-methodology.md)
