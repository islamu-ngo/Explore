ABOUTME: Styling MudBlazor v9 components — modern aesthetic with CSS isolation.
ABOUTME: Covers customization strategies, CSS variables, wrapper components, and ::deep patterns.

# MudBlazor Styling with Isolation (v9)

Combine MudBlazor component `Class` parameters with isolated CSS and BEM names.

## Customization Priority Order

1. **Component parameters** first (`Color`, `Variant`, `Elevation`, `Size`, `Dense`).
2. **`Class` parameter** with MudBlazor utility classes (`pa-4`, `d-flex`, `rounded-lg`).
3. **MudTheme** for global palette, typography, and `LayoutProperties.DefaultBorderRadius`.
4. **CSS variables** (`--mud-palette-*`) for theme-level fine-tuning.
5. **Wrapper components** for shared defaults (replaces `MudGlobal`).
6. **`.razor.css` + `::deep`** for internal element styling — last resort, fragile.

## CSS Variable Override Examples

```css
/* In global app.css or site.css — use sparingly */
:root {
    --mud-palette-lines-default: #e5e7eb;
    --mud-palette-table-hover: #f9fafb;
    --mud-elevation-0: none;
    --mud-elevation-1: 0 1px 3px 0 rgba(0,0,0,0.06), 0 1px 2px -1px rgba(0,0,0,0.06);
}
```

## Styling MudBlazor Internals (::deep Pattern)

Blazor isolated CSS **cannot** directly style MudBlazor component internals (scoped attribute doesn't cross component boundaries).

### Correct Pattern: Wrapper div + ::deep

```razor
@* MyComponent.razor *@
<div class="my-table-wrapper">
    <MudTable Items="@_items" Hover>...</MudTable>
</div>
```

```css
/* MyComponent.razor.css */
.my-table-wrapper ::deep .mud-table-cell {
    padding: 8px 12px;
    font-size: 0.8125rem;
}

.my-table-wrapper ::deep .mud-table-head .mud-table-cell {
    font-weight: 600;
    color: var(--mud-palette-text-secondary);
    text-transform: uppercase;
    font-size: 0.75rem;
    letter-spacing: 0.05em;
}
```

### Rules for ::deep
- Always wrap MudBlazor components in a `<div>` with a BEM class before applying `::deep`.
- Target specific MudBlazor CSS classes (e.g., `.mud-table-cell`, `.mud-input-outlined`).
- `::deep` selectors are fragile — MudBlazor may rename internal classes across versions.
- Prefer component `Class` parameter over `::deep` when possible.

## Utility Classes for Modern Look

Common MudBlazor utility class combinations:

| Pattern | Classes |
|---------|---------|
| Outlined card | `Class="border rounded-lg"` + `Elevation="0"` |
| Rounded pill | `Class="rounded-pill"` |
| Muted text | `Color="Color.Secondary"` or `Class="mud-text-secondary"` |
| Centered empty state | `Class="d-flex flex-column align-center justify-center pa-8"` |
| Horizontal button group | `Class="d-flex gap-3 justify-end"` |
| Dense table wrapper | `Class="border rounded-lg overflow-hidden"` |
| Subtle divider | `<MudDivider Class="my-4" />` |

## v9 Specific CSS Notes
- **v9**: `MudGlobal` theming defaults removed — set `Variant`, `Color`, `Margin`, `ShrinkLabel` explicitly on components or use wrapper components for shared defaults.
- **v9**: MudTabs class parameters renamed (`TabPanelClass` → `TabButtonsClass`, `PanelClass` → `TabPanelsClass`). Verify custom CSS targets after upgrade.
- **v9**: MudSwitch/CheckBox/Radio render content inside `<span>` — update `::deep` selectors if targeting child text.
- **v9**: MudDrawer uses CSS `transition` instead of `animation` — custom animation overrides need updating.
- **v9**: `CssBuilder`/`StyleBuilder` are `readonly struct`. Use `new CssBuilder()` or `CssBuilder.Default()` — **never** `default(CssBuilder)` (throws NRE).

## Wrapper Components And DialogOptionsFactory

See [mudblazor-usage.md](../blazor-ui-conventions/resources/mudblazor-usage.md) for the wrapper component catalog and DialogOptionsFactory presets. Wrappers live in `Explore.Blazor.Client/Components/Common/` and use `display: contents` + `::deep` for CSS isolation.

Also see the `design-system` skill (`wrapper-components.md`) for detailed parameter tables and usage guidance.

## Global `.mud-*` Override Exception Policy

Global MudBlazor class overrides are tracked in `css/mudblazor-overrides.css` with a whitelist header comment. Each override block must include a `JUSTIFICATION` comment explaining why scoped CSS or wrapper components are insufficient.

**Currently approved exceptions**:
1. **Drawer/portal overrides** — MudDrawerContainer renders outside Blazor component scope boundary. Fixed positioning + z-index management cannot be achieved with CSS isolation.
2. **Overlay z-index** — Cross-component layering for modal overlays.

**Migration path**: Button overrides have been migrated from global to `AppButton.razor.css` via `::deep`. New global overrides require documented justification.

## oklch Color Functions

Prefer `oklch()` and `color-mix(in oklch, ...)` over `rgba()` and `color-mix(in srgb, ...)` for perceptually uniform color manipulation:

```css
/* Shadows — oklch for perceptual uniformity */
--isl-shadow-sm: 0 1px 2px oklch(0 0 0 / 0.05);

/* Color mixing — oklch produces better intermediate tones */
--isl-card-border: color-mix(in oklch, var(--mud-palette-lines-default) 80%, transparent);

/* Interaction states */
--isl-state-hover: color-mix(in oklch, var(--isl-color-primary) 8%, transparent);
```

## Anti-Patterns (Don't Do This)

- ❌ `Elevation="4"` or higher on content cards — too heavy.
- ❌ Inline `Style="..."` for repeating patterns — use `Class` + CSS.
- ❌ Global CSS overriding MudBlazor base classes without scoping — causes side effects.
- ❌ `!important` in isolated CSS — sign of a specificity problem.
- ❌ Coloring entire surfaces with `Color.Primary` — use sparingly for CTAs only.
- ❌ `color-mix(in srgb, ...)` — use `color-mix(in oklch, ...)` for perceptually uniform results.
- ❌ `transition: all` — specify exact properties (`background-color`, `box-shadow`, `border-color`, `opacity`).

## Related
- [v9-migration.md](../blazor-ui-conventions/resources/v9-migration.md) — v9 breaking API changes
