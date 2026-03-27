ABOUTME: 3-tier design token system (Primitives, Semantic, Component) with naming conventions.
ABOUTME: Defines token categories, sources, and accessibility-related tokens.

# Design Token System

## Three Tiers

### Tier 1: Primitives (Raw Values)

Direct values with no contextual meaning. Defined in `tokens.css`.

| Category | Pattern | Example | Notes |
|----------|---------|---------|-------|
| Spacing | `--isl-space-{1-16}` | `--isl-space-4` = 16px | 4px grid (N × 4px) |
| Radius | `--isl-radius-{sm\|md\|lg\|full}` | `--isl-radius-md` = 8px | |
| Shadows | `--isl-shadow-{xs\|sm\|md\|lg}` | `--isl-shadow-md` | oklch-based |
| Fonts | `--isl-font-family-{primary\|secondary}` | System font stack | |

### Tier 2: Semantic (Contextual Meaning)

Map to MudBlazor palette or carry layout/a11y meaning. Defined in `tokens.css`.

| Category | Pattern | Source | Example |
|----------|---------|--------|---------|
| Colors | `--isl-color-{primary\|secondary\|surface\|background\|text\|error\|success\|warning\|info}` | `--mud-palette-*` | `--isl-color-primary: var(--mud-palette-primary)` |
| Layout | `--isl-space-{inline\|block\|page\|section}` | Primitives | `--isl-space-page: var(--isl-space-8)` |
| Radius | `--isl-radius-{card\|input\|button}` | Primitives | `--isl-radius-card: var(--isl-radius-md)` |
| Typography | `--isl-h{1-6}-size` | `clamp()` | Fluid between 320px–1280px viewports |
| Overlays | `--isl-overlay-{text\|gradient\|scrim}` | Direct | rgba values |
| A11y | `--isl-target-min`, `--isl-focus-ring-width` | Direct | 24px (WCAG), 2px |
| States | `--isl-state-{hover\|active\|disabled}-opacity` | Direct | disabled = 0.38 |

### Tier 3: Component (Scoped)

Narrow tokens for specific components. Defined in `tokens.css` or component CSS.

| Token | Example | Scope |
|-------|---------|-------|
| `--isl-card-border-radius` | `var(--isl-radius-card)` | Cards |
| `--isl-card-hover-shadow` | `var(--isl-shadow-sm)` | Cards |
| `--isl-button-padding` | `var(--isl-space-2) var(--isl-space-4)` | Buttons |
| `--isl-button-font-size` | `0.875rem` | Buttons |

## Accessibility Tokens

```css
--isl-target-min: 24px;         /* WCAG 2.5.8 minimum target */
--isl-focus-ring-width: 2px;    /* Visible focus indicator */
```

Enhanced contrast media query strengthens shadow tokens:

```css
@media (prefers-contrast: more) {
  :root { --isl-shadow-md: /* stronger values */; }
}
```

## Naming Convention

`--isl-{tier}-{property}` where tier context is implicit:
- Primitives: `--isl-space-4`, `--isl-radius-sm`
- Semantic: `--isl-color-primary`, `--isl-target-min`
- Component: `--isl-card-border-radius`, `--isl-button-padding`

## Related

- `resources/layer-architecture.md` — where tokens sit in the cascade
- `resources/wrapper-components.md` — components that consume tokens
