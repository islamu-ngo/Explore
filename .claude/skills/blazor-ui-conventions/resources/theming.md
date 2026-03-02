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

```csharp
private MudTheme _theme = new()
{
    PaletteLight = new Palette()
    {
        Primary = "#1a1a2e",          // deep, calm primary
        Secondary = "#6366f1",        // subtle accent (indigo)
        Tertiary = "#10b981",         // success green
        Background = "#fafafa",       // off-white, not pure white
        Surface = "#ffffff",
        AppbarBackground = "#ffffff", // clean white appbar
        AppbarText = "#1a1a2e",
        DrawerBackground = "#ffffff",
        DrawerText = "#374151",
        TextPrimary = "#111827",      // near-black, not pure black
        TextSecondary = "#6b7280",    // muted gray
        ActionDefault = "#6b7280",
        Divider = "#e5e7eb",          // subtle divider
        LinesDefault = "#e5e7eb",
        TableHover = "#f9fafb",
        HoverOpacity = 0.04,
    },
    PaletteDark = new Palette()
    {
        Primary = "#818cf8",          // soft indigo
        Secondary = "#6366f1",
        Background = "#0f0f17",       // deep dark, not pure black
        Surface = "#1a1a2e",
        AppbarBackground = "#1a1a2e",
        TextPrimary = "#f3f4f6",
        TextSecondary = "#9ca3af",
        Divider = "#374151",
        LinesDefault = "#374151",
        TableHover = "rgba(255,255,255,0.03)",
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
            FontSize = "0.875rem",      // 14px base — compact, modern
            LineHeight = "1.5",
            LetterSpacing = "-0.011em", // subtle tightening
        },
        H1 = new H1Typography { FontSize = "2rem", FontWeight = 700, LetterSpacing = "-0.025em" },
        H2 = new H2Typography { FontSize = "1.5rem", FontWeight = 600, LetterSpacing = "-0.02em" },
        H3 = new H3Typography { FontSize = "1.25rem", FontWeight = 600, LetterSpacing = "-0.015em" },
        H4 = new H4Typography { FontSize = "1.125rem", FontWeight = 600 },
        H5 = new H5Typography { FontSize = "1rem", FontWeight = 600 },
        H6 = new H6Typography { FontSize = "0.875rem", FontWeight = 600 },
        Subtitle1 = new Subtitle1Typography { FontSize = "0.875rem", FontWeight = 500 },
        Body1 = new Body1Typography { FontSize = "0.875rem", LineHeight = "1.625" },
        Body2 = new Body2Typography { FontSize = "0.8125rem", LineHeight = "1.5" },
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

## CSS Variable Overrides

Use sparingly for fine-tuning beyond the theme:

```css
:root {
    --mud-palette-lines-default: #e5e7eb;
    --mud-palette-table-hover: #f9fafb;
    --mud-elevation-1: 0 1px 3px 0 rgba(0,0,0,0.06), 0 1px 2px -1px rgba(0,0,0,0.06);
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
