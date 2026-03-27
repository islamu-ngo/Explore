ABOUTME: CSS design system skill covering @layer architecture, design tokens, and wrapper components.
ABOUTME: Enforces layer ordering, token usage, and MudBlazor wrapper patterns.

---
name: design-system
description: CSS @layer architecture, 3-tier design tokens, and MudBlazor wrapper components.
type: domain
enforcement: enforce
priority: high
---

# Design System

## Keywords

css layers, @layer, design tokens, wrapper component, AppButton, AppCard, AppTextField, AppIconButton, AppDialogShell, DialogOptionsFactory, AppearanceStyleBuilder, mudblazor override, display contents, ::deep

## File Patterns

- `wwwroot/css/layers.css`
- `wwwroot/css/tokens.css`
- `wwwroot/css/mudblazor-overrides.css`
- `wwwroot/css/utilities.css`
- `**/Components/Common/App*.razor`
- `**/Components/Common/App*.razor.css`
- `**/Helpers/AppearanceStyleBuilder.cs`
- `**/Services/DialogOptionsFactory.cs`

## Non-Inferable Rules

1. CSS layers order (lowest→highest): `reset → base → tokens → mudblazor-overrides → components → utilities`. Unlayered CSS (Blazor CSS isolation, MudThemeProvider) beats all layered — by design.
2. MudBlazor `.mud-*` overrides are ONLY allowed in `mudblazor-overrides.css`. Each override must have a comment justifying it. No bare `.mud-*` selectors anywhere else.
3. All wrapper components use `display: contents` to be transparent in layout, then `::deep` to style MudBlazor internals via CSS isolation.
4. Design tokens follow 3 tiers: Primitives (raw values), Semantic (contextual meaning from `--mud-palette-*`), Component (scoped to specific components).
5. Spacing uses a 4px grid: `--isl-space-1` (4px) through `--isl-space-16` (64px).
6. Typography is fluid: H1–H6 use `clamp()` between 320px and 1280px viewport widths.
7. DialogOptionsFactory provides 4 standard presets: Small, Medium, Confirmation, Editor. Always use a preset — never create `DialogOptions` manually.
8. AppearanceStyleBuilder has 4 background effects: None, SoftOverlay (0.24), StrongOverlay (0.40), Blur (0.18). Methods: `BuildStyle`, `BuildHeroStyle`, `BuildBannerStyle`.

## Activation

Load this skill when:
- Creating or modifying CSS files in `wwwroot/css/`
- Working on wrapper components in `Components/Common/`
- Implementing appearance/theming features
- Overriding MudBlazor styles
- Creating or modifying dialog options

## Resources

- `resources/layer-architecture.md` — 6 CSS layers, ordering rules, override policy
- `resources/token-system.md` — 3-tier token definitions, naming conventions
- `resources/wrapper-components.md` — 5 wrappers, parameters, styling patterns
- `resources/appearance-builder.md` — AppearanceStyleBuilder, DialogOptionsFactory, effects

## Related

- `docs/DESIGN_SYSTEM.md`
- `.claude/skills/blazor-ui-conventions/SKILL.md`
- `.claude/skills/blazor-css-isolation/SKILL.md`
