ABOUTME: Minimal BEM naming rules for scoped CSS in Blazor.
ABOUTME: Preserves block/element/modifier conventions only.

# BEM Methodology (Lean)

## Naming
- Block: `.block`
- Element: `.block__element`
- Modifier: `.block--modifier` or `.block__element--modifier`

## Rules
- Keep selectors flat; avoid deep nesting.
- Prefer scoped CSS (`.razor.css`) for component styles.
- Use `::deep` only to reach internal MudBlazor elements.

## Related
- [theming.md](theming.md)
