ABOUTME: Theming rules for MudBlazor v9 — modern, clean aesthetic.
ABOUTME: Covers MudTheme config, palette, typography, layout, and CSS variable overrides.

# Theming (v9)

## Rules
- Theme lives in a single `MudTheme` definition.
- `MainLayout` hosts `MudThemeProvider` and applies dark/light state.
- Persist theme preference via cookie/local storage (BFF-safe).

## Modern Aesthetic Direction

Our UI follows a **neo-minimal** aesthetic inspired by modern design systems: clean surfaces, generous whitespace, soft rounded corners, subtle shadows, and purposeful color. Think calm, professional, and uncluttered.

### MudTheme Configuration

The actual theme is composed in `AppearanceThemeService.cs`. Key values:

```csharp
private MudTheme _theme = new()
{
    PaletteLight = new Palette()
    {
        Primary = "#3B82F6",
        Secondary = "#1E293B",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        AppbarBackground = "rgba(248,250,252,0.85)",
        TextPrimary = "#0F172A",
        TextSecondary = "#64748B",
        // ... see AppearanceThemeService.cs for full palette
    },
    PaletteDark = new Palette()
    {
        Primary = "#60A5FA",
        Background = "#0F172A",
        Surface = "#1E293B",
        TextPrimary = "#F1F5F9",
        TextSecondary = "#94A3B8",
    },
    LayoutProperties = new LayoutProperties
    {
        DefaultBorderRadius = "12px", // soft rounded corners everywhere
    },
    Typography = new Typography
    {
        Default = new DefaultTypography
        {
            FontFamily = new[] { "Inter", "system-ui", "-apple-system", "sans-serif" },
            FontSize = "0.9375rem",     // 15px base
            LineHeight = "1.5",
            LetterSpacing = "-0.011em",
        },
        // H1-H5 use fluid clamp() values for responsive scaling
        H1 = new H1Typography { FontSize = "clamp(1.875rem, 1.5rem + 1.04vw, 2.5rem)", FontWeight = 700 },
        H2 = new H2Typography { FontSize = "clamp(1.625rem, 1.375rem + 0.625vw, 2rem)", FontWeight = 600 },
        H3 = new H3Typography { FontSize = "clamp(1.5rem, 1.333rem + 0.42vw, 1.75rem)", FontWeight = 600 },
        H4 = new H4Typography { FontSize = "clamp(1.25rem, 1.083rem + 0.42vw, 1.5rem)", FontWeight = 600 },
        H5 = new H5Typography { FontSize = "clamp(1.125rem, 1.042rem + 0.21vw, 1.25rem)", FontWeight = 600 },
        H6 = new H6Typography { FontSize = "0.875rem", FontWeight = 600 },
        Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = 500, LetterSpacing = "0" },
        Caption = new CaptionTypography { FontSize = "0.75rem", LineHeight = "1.5" },
    },
};
```

### Key Aesthetic Principles

| Principle | Rule |
|-----------|------|
| **Elevation** | Prefer `Elevation="0"` with subtle `border` or `Outlined` variant. Use `Elevation="1"` max for floating elements (dropdowns, menus). |
| **Border radius** | `DefaultBorderRadius = "12px"` in theme. For pills/chips: `Class="rounded-pill"`. |
| **Spacing** | Generous padding — `pa-4` to `pa-6` for cards; `gap-4` between items. Never crowd elements. |
| **Color** | Muted neutrals for backgrounds; accent color used sparingly for CTAs and active states only. |
| **Borders** | `1px solid var(--mud-palette-lines-default)` — subtle, not heavy. |
| **Hover** | Subtle background shift (`HoverOpacity = 0.04`); avoid color jumps. |
| **Typography** | Clear hierarchy: large headings with tight letter-spacing; smaller body with relaxed line-height. |
| **Whitespace** | Leave breathing room. Prefer more whitespace over cramming content. |
| **Shadows** | Near-invisible shadow (`Elevation="0"` or `1`). Depth via borders, not heavy box-shadows. |
| **Transitions** | 150ms for hover/focus states. Smooth, not flashy. |

## v9 Palette Changes
- `PaletteLight` and `PaletteDark` property types are now **`Palette`** (unified type). Replace `new PaletteLight()` / `new PaletteDark()` with `new Palette()`.
- `MudThemeProvider` supports `@bind-CurrentPalette` for runtime palette binding.
- `ObserveSystemThemeChange` → **`ObserveSystemDarkModeChange`**.

## v9 MudGlobal Removal
- All `MudGlobal` theming properties are **removed** (e.g., `MudGlobal.ButtonDefaults`, `MudGlobal.InputDefaults`, `MudGlobal.Rounded`).
- Set component defaults explicitly on each component, via wrapper components, or via `MudTheme` tokens.
- Popover defaults now configured via `PopoverOptions` in `AddMudServices(config => { ... })`.

## 3-Tier Design Token System

See the `design-system` skill (`token-system.md`) for the complete token reference. Summary:

| Tier | Purpose | Example |
|------|---------|---------|
| **Primitives** | Raw values (spacing grid, radii, shadows, fonts) | `--isl-space-4`, `--isl-radius-md`, `--isl-shadow-sm` |
| **Semantic** | Purpose aliases pointing to MudBlazor or primitive tokens | `--isl-color-primary`, `--isl-space-page` |
| **Component** | Scoped tokens for specific components (3+ usage rule) | `--isl-card-border-radius`, `--isl-button-ring` |

Use `oklch()` and `color-mix(in oklch, ...)` for perceptually uniform color manipulation. Prefer over `rgba()` and `color-mix(in srgb, ...)`.

## CSS Variable Overrides

Use sparingly for fine-tuning beyond the theme:

```css
:root {
    --mud-palette-lines-default: #e5e7eb;
    --mud-palette-table-hover: #f9fafb;
    --mud-elevation-1: 0 1px 3px 0 oklch(0 0 0 / 0.06), 0 1px 2px -1px oklch(0 0 0 / 0.06);
}
```

Priority order for customization:
1. **MudTheme** properties (Palette, Typography, LayoutProperties)
2. **Component parameters** (`Color`, `Variant`, `Elevation`, `Class`)
3. **CSS variables** (`--mud-palette-*`, `--mud-elevation-*`)
4. **Wrapper components** for shared defaults (replaces MudGlobal)
5. **`::deep` in `.razor.css`** — last resort, fragile

## Related
- [bem-methodology.md](bem-methodology.md)
- [mudblazor-styling.md](../../../.claude/skills/blazor-css-isolation/resources/mudblazor-styling.md)
