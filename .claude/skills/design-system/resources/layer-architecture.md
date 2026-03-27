ABOUTME: CSS @layer architecture with 6 ordered layers and MudBlazor override policy.
ABOUTME: Defines layer ordering, what goes where, and the override whitelist rules.

# CSS Layer Architecture

## Layer Order (Lowest → Highest Specificity)

| # | Layer | File | Purpose |
|---|-------|------|---------|
| 1 | reset | layers.css | Browser normalization |
| 2 | base | layers.css | HTML element defaults |
| 3 | tokens | tokens.css | Design token custom properties |
| 4 | mudblazor-overrides | mudblazor-overrides.css | Approved MudBlazor `.mud-*` overrides |
| 5 | components | *.razor.css (via @layer) | Component-scoped styles |
| 6 | utilities | utilities.css | Utility classes (.isl-typo-*, .sr-only) |

**Unlayered CSS wins over ALL layers.** This is by design:
- Blazor CSS isolation (`*.razor.css` without `@layer`) is unlayered → highest priority
- MudThemeProvider inline styles are unlayered → highest priority

## Layer Declaration (layers.css)

```css
@layer reset, base, tokens, mudblazor-overrides, components, utilities;
```

This single declaration at the top of `layers.css` establishes the order. All subsequent `@layer` blocks follow this hierarchy.

## MudBlazor Override Policy

Overrides to `.mud-*` classes are **only allowed** in `mudblazor-overrides.css`. Rules:

1. Each override must have a comment explaining **why** it is needed.
2. Only structural/layout overrides are permitted (z-index, container sizing).
3. No color, typography, or spacing overrides — use MudTheme or design tokens.
4. Never add bare `.mud-*` selectors outside `mudblazor-overrides.css`.

Current approved overrides:
- Drawer container width/positioning
- Overlay z-index stacking

## Precedence Summary

```
MudThemeProvider (unlayered) > Blazor CSS isolation (unlayered) > utilities > components > mudblazor-overrides > tokens > base > reset
```

## Related

- `resources/token-system.md` — what goes in the tokens layer
- `.claude/skills/blazor-css-isolation/SKILL.md` — component CSS isolation rules
