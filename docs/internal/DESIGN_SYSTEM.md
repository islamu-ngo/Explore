ABOUTME: CSS architecture, design token system, MudBlazor wrapper components, and styling conventions.
ABOUTME: Covers @layer ordering, 3-tier tokens, component wrappers, dialogs, and appearance builder.

# Design System

## CSS Layer Architecture

CSS specificity is managed through `@layer` declarations. Layer order (lowest to highest priority):

```css
@layer reset, base, tokens, mudblazor-overrides, components, utilities;
```

| Layer | Purpose | File(s) |
|-------|---------|---------|
| `reset` | Normalize browser defaults | `reset.css` |
| `base` | HTML element base styles | `base.css` |
| `tokens` | Design token definitions | `tokens.css` |
| `mudblazor-overrides` | Approved MudBlazor style overrides | `mudblazor-overrides.css` |
| `components` | Component-specific styles | Various `component.razor.css` |
| `utilities` | Helper classes, typography, a11y | `utilities.css` |

**Unlayered CSS** (Blazor CSS isolation, MudThemeProvider) beats all layered styles. This is intentional — scoped component styles always win.

### MudBlazor Override Policy

Only drawer container and overlay z-index overrides are approved. Each override in `mudblazor-overrides.css` is documented with justification. **No bare `.mud-*` selectors outside this file.**

## Design Tokens

Three-tier token system in `tokens.css`:

### Tier 1: Primitives

Raw values with no semantic meaning.

| Category | Examples |
|----------|---------|
| Spacing | `--isl-space-1` through `--isl-space-16` (4px grid) |
| Radius | `--isl-radius-sm`, `--isl-radius-md`, `--isl-radius-lg`, `--isl-radius-full` |
| Shadows | `--isl-shadow-xs` through `--isl-shadow-lg` (oklch-based) |
| Typography | `--isl-font-family-primary`, `--isl-font-family-secondary` |

### Tier 2: Semantic

Purpose-driven tokens sourced from MudBlazor palette or computed.

| Category | Key Tokens |
|----------|------------|
| Colors | `--isl-color-primary`, `--isl-color-secondary`, `--isl-color-surface`, `--isl-color-background`, `--isl-color-text`, `--isl-color-error`, `--isl-color-success`, `--isl-color-warning`, `--isl-color-info` (sourced from `--mud-palette-*`) |
| Layout | `--isl-space-inline`, `--isl-space-block`, `--isl-space-page`, `--isl-space-section` |
| Radii | `--isl-radius-card`, `--isl-radius-input`, `--isl-radius-button` |
| Typography | Fluid `clamp()` scales H1–H6 (320px–1280px viewport) |
| Overlays | `--isl-overlay-text`, `--isl-overlay-gradient`, `--isl-overlay-scrim` |
| Accessibility | `--isl-target-min: 24px` (WCAG), `--isl-focus-ring-width: 2px` |
| States | `--isl-state-hover-opacity`, `--isl-state-active-opacity`, `--isl-state-disabled-opacity: 0.38` |

### Tier 3: Component

Scoped tokens for specific components, consuming semantic tokens.

| Token | Value Source |
|-------|-------------|
| `--isl-card-border-radius` | `--isl-radius-card` |
| `--isl-card-hover-shadow` | `--isl-shadow-md` |
| `--isl-button-border-radius` | `--isl-radius-button` |
| `--isl-button-padding` | Computed from spacing tokens |
| `--isl-button-transition` | Standard transition value |

### Accessibility Tokens

```css
@media (prefers-contrast: more) {
  /* Strengthened shadows and borders for high-contrast mode */
}
```

## Wrapper Components

Located in `Explore.Blazor.Client/Components/Common/`. Each wraps a MudBlazor component with project defaults.

Embedded control-plane pages use the local `ControlPlane*` primitives in `Explore.Blazor.Client/Components/ControlPlane/`. These primitives follow the same token, BEM, CSS isolation, and MudBlazor v9 conventions as the public and tenant surfaces.

### AppButton

Wraps `MudButton` with consistent defaults.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Variant` | `Filled` | MudBlazor variant |
| `Color` | `Primary` | Theme color |
| `Size` | `Medium` | Button size |
| `Elevation` | `0` | Shadow elevation |
| `OnClick` | — | Click handler |
| `Disabled` | `false` | Disabled state |
| `FullWidth` | `false` | Full-width mode |
| `ButtonType` | — | Submit/button/reset |
| `StartIcon` / `EndIcon` | — | Icon placement |
| `Href` | — | Navigation link |

### AppCard

Wraps `MudCard` with flat styling.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Elevation` | `0` | No shadow by default |
| `Outlined` | `false` | Border visibility |

### AppTextField\<T\>

Wraps `MudTextField` with outlined style.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Variant` | `Outlined` | Input style |
| `Margin` | `None` | Outer margin |
| `InputType` | `Text` | HTML input type |
| `Lines` | `1` | Textarea rows |
| `MaxLength` | `524288` | Character limit |
| `DebounceInterval` | `0` | Input throttle (ms) |
| `Immediate` | `false` | Bind on every keystroke |

### AppIconButton

Wraps `MudIconButton` with required icon.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Icon` | (required) | MudBlazor icon string |
| `Color` | `Default` | Theme color |
| `Size` | `Medium` | Button size |

### AppDialogShell

Standard dialog layout with BEM structure.

| Parameter | Description |
|-----------|-------------|
| `Title` | Dialog header text |
| `HeaderContent` | Custom header slot |
| `ChildContent` | Dialog body (required) |
| `ActionsContent` | Footer actions slot |

BEM classes: `__header`, `__body`, `__actions`.

### Styling Pattern

All wrappers use `display: contents` + `::deep` to style MudBlazor internals via CSS isolation. This preserves the MudBlazor DOM structure while allowing scoped CSS customization.

Control-plane primitives use the same isolation pattern where they wrap MudBlazor controls. Structural primitives such as `ControlPlanePageHeader` and `ControlPlanePanel` prefer plain semantic HTML so headings, landmarks, and HAL-gated action slots can be reused by both embedded and separate control-plane hosts.

## DialogOptionsFactory

Pre-configured dialog presets in `Explore.Blazor.Client/Services/`:

| Preset | MaxWidth | Behavior |
|--------|----------|----------|
| `Small` | `MaxWidth.Small` | FullWidth, CloseOnEscape |
| `Medium` | `MaxWidth.Medium` | FullWidth, CloseOnEscape |
| `Confirmation` | `MaxWidth.Small` | Center position |
| `Editor` | `MaxWidth.Medium` | CloseButton, BackdropClick |
| `ImageLightbox` | Unconstrained | NoHeader, BackdropClick, CloseOnEscape |

## AppearanceStyleBuilder

Generates inline CSS for actor/entity appearance customization. Located in `Explore.Blazor.Client/Helpers/`.

### AppearanceSettings Model

| Property | Type | Description |
|----------|------|-------------|
| `BackgroundColor` | `string?` | Hex color value |
| `ImageUri` | `string?` | Background image URL |
| `BackgroundEffect` | `string` | Effect type (default: `"None"`) |
| `IsEmpty` | `bool` | Computed — true if no customization |

### Builder Methods

| Method | Extra Behavior |
|--------|---------------|
| `BuildStyle(settings, fallbackHex, additionalCss?)` | Base style generation |
| `BuildHeroStyle(settings, fallbackHex)` | Adds `aspect-ratio: 16/9` |
| `BuildBannerStyle(settings, fallbackHex)` | Banner-specific layout |

### Background Effects

| Effect | Overlay Opacity |
|--------|----------------|
| `None` | No overlay |
| `SoftOverlay` | `rgba(0,0,0,0.24)` |
| `StrongOverlay` | `rgba(0,0,0,0.40)` |
| `Blur` | `rgba(0,0,0,0.18)` + blur filter |

### AppearanceEditor Component

Located in `Explore.Blazor.Client/Shared/`. Two-way bindable component for editing appearance settings.

| Parameter | Default | Description |
|-----------|---------|-------------|
| `BackgroundColor` | — | Two-way bindable color |
| `BackgroundEffect` | — | Two-way bindable effect |
| `ImageUri` | — | Two-way bindable image URL |
| `ShowImageField` | `true` | Show/hide image URL input |
| `ShowPreview` | `true` | Show/hide live preview |
| `FallbackColor` | `#f5f5f5` | Default when no color set |

Controls: `MudColorPicker` (Spectrum mode, 100ms throttle), `AppTextField` (image URL), `MudSelect` (effects), `AppButton` (reset).

## Typography Utilities

Defined in `utilities.css`:

| Class | Purpose |
|-------|---------|
| `.isl-typo-h1` through `.isl-typo-h6` | Heading scales |
| `.isl-typo-body1`, `.isl-typo-body2` | Body text |
| `.isl-typo-button` | Button label style |
| `.isl-typo-caption`, `.isl-typo-overline` | Small text variants |

### Accessibility Utilities

| Class/Rule | Purpose |
|------------|---------|
| `.sr-only` | Screen-reader-only content |
| `.skip-link` | Skip navigation link |
| `:focus-visible` | Global focus ring |
| `@media (prefers-reduced-motion)` | Reduce animations |
| `@media (forced-colors)` | High-contrast mode support |

## Related

- [BLAZOR.md](BLAZOR.md) — component architecture and render strategy
- [ACCESSIBILITY.md](ACCESSIBILITY.md) — WCAG requirements and CSS direction rules
- [ADR-003](adr/ADR-003-css-layer-architecture.md) — CSS layer architecture decision
- [CONTRIBUTING.md](CONTRIBUTING.md) — CSS rules for contributors
