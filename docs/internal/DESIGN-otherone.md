<!-- ABOUTME: Design-token reference for agents approximating the Explore visual language. -->
<!-- ABOUTME: Documents palette, typography, spacing, and wrapper-component defaults. -->

# Explore Design Tokens — Reference for Design Agents

> **Scope note:** This is a **tokens/style reference only** — there is no compiled component bundle behind it.
> `Explore.Blazor.Client` is a Blazor WebAssembly app (MudBlazor v9, `.razor` components compiled to .NET IL/WASM).
> It has no JS/React/Storybook layer, so its real components cannot be rendered by Claude Design.
> Use the values below to **approximate the look** with generic components — colors, type scale, spacing,
> and radii will match; exact component structure will not.

## Wrapping and setup

Every screen sits on a MudBlazor `MudThemeProvider` fed by a single `MudTheme` (light + dark palette,
typography, layout properties). Without that provider, none of the colors below apply — components fall
back to browser defaults. There is no per-component theme prop; theme is global and swapped wholesale
between light/dark, so a design should pick ONE mode and apply its palette consistently rather than mixing
light and dark tokens on the same screen.

Border radius is global too: **12px** on all surfaces via `LayoutProperties.DefaultBorderRadius` (not
per-component overrides).

## Color palette (real hex values)

### Light

| Role | Value |
|---|---|
| Primary | `#18181B` |
| Primary contrast text | `#FFFFFF` |
| Secondary | `#52525B` |
| Background | `#F5F5F7` |
| Surface | `#FFFFFF` |
| Appbar background | `#FFFFFF` |
| Text primary | `#18181B` |
| Text secondary | `#404040` |
| Success | `#047857` |
| Warning | `#B45309` |
| Error | `#DC2626` |
| Info | `#52525B` |
| Divider / lines | `#E4E4E7` / `#A1A1AA` |

### Dark

| Role | Value |
|---|---|
| Primary | `#FAFAFA` |
| Primary contrast text | `#1A1A1A` |
| Secondary | `#A1A1AA` |
| Background | `#1A1A1A` |
| Surface | `#242424` |
| Appbar background | `rgba(26,26,26,0.92)` |
| Text primary | `#FAFAFA` |
| Text secondary | `#A1A1AA` |
| Success | `#34D399` |
| Warning | `#FBBF24` |
| Error | `#F87171` |
| Info | `#A1A1AA` |
| Divider / lines | `#2E2E2E` / `#3F3F46` |

This is a near-monochrome palette (zinc/gray scale for primary, not a saturated brand blue) with color
reserved for status states only (success/warning/error/info). Don't introduce a saturated brand color for
primary actions — buttons and active states are ink-on-white / white-on-ink, not blue-on-white.

## Typography

Single font family throughout: **Inter**, falling back to `system-ui, -apple-system, sans-serif`. No
secondary/display font.

| Level | Size | Weight | Line height | Letter spacing |
|---|---|---|---|---|
| Body (default) | 15px (`.9375rem`) | 400 | 1.5 | -0.011em |
| H1 | fluid 30–40px | 700 | 1.2 | -0.022em |
| H2 | fluid 26–32px | 600 | 1.3 | -0.017em |
| H3 | fluid 24–28px | 600 | 1.3 | -0.014em |
| H4 | fluid 20–24px | 600 | 1.4 | — |
| H5 | fluid 18–20px | 600 | 1.5 | — |
| H6 | 18px | 600 | 1.6 | — |
| Body2 | 14px | 400 | 1.5 | — |
| Button | 14px | 500 | 1.75 | -0.011em, no uppercase transform |
| Caption | 13px | 400 | 1.5 | — |
| Overline | 12px | 500 | 2.66 | 0.08em, uppercase |

Headings use fluid `clamp()` scaling between 320px and 1280px viewports — treat them as responsive, not
fixed pixel values, when composing layouts at different breakpoints. Buttons are **not** uppercase (a common
MudBlazor default elsewhere) — this system explicitly disables that transform.

## Spacing and radius vocabulary

4px-grid spacing scale, referenced semantically rather than as raw numbers:

| Semantic token | Value | Use |
|---|---|---|
| `space-inline` | 8px | inline gaps |
| `space-block` | 16px | vertical gaps |
| `space-page` | 24px | page padding |
| `space-section` | 32px | section spacing |

| Semantic token | Value | Use |
|---|---|---|
| `radius-input` / `radius-button` | 8px | form inputs, buttons |
| `radius-card` | 16px | card surfaces |
| global surface radius | 12px | MudTheme default (applies broadly) |

Shadows are soft and low-contrast (`oklch` alpha 0.05–0.2), never hard drop shadows — cards default to
**flat / no elevation** and only pick up a shadow on hover.

## Component idiom (wrapper components, not raw MudBlazor)

The real codebase never uses raw `MudButton`/`MudCard`/etc. directly — every screen composes small
wrapper components with fixed defaults. When approximating with generic components, mirror these defaults:

| Wrapper | Defaults |
|---|---|
| Button | Filled variant, Primary color, Medium size, **0 elevation** (flat, no shadow) |
| Card | **0 elevation**, not outlined by default — flat surface, border comes from `divider` color, not shadow |
| Text field | **Outlined** variant (never underline/filled), no outer margin |
| Icon button | Default color, Medium size |
| Dialog | Header / body / actions as three distinct visual bands, not one undifferentiated block |

Net effect: this is a **flat, low-elevation, outline-forward** design language — borders and dividers do
the separating work, not drop shadows. Reserve shadow for hover/active feedback, not resting state.

## One idiomatic composition

A typical card in this system: flat white/dark surface, 16px radius, 1px divider-color border, 24px inner
padding, heading in H4/H5 weight 600, body copy at 15px/1.5 line-height, actions rendered as flat Primary
buttons with no shadow — color is used only if the card communicates a status (success/warning/error), never
for decoration.

## Where the full truth lives (for future reference, not consumable by this agent)

- `src/Explore.Blazor/wwwroot/css/tokens.css` — the primitive/semantic/component CSS variable tiers.
- `src/Explore.Blazor.Client/Services/AppearanceThemeService.cs` — the MudBlazor `MudTheme` C# source (palette, typography, layout).
- `docs/DESIGN_SYSTEM.md` — CSS layer architecture and wrapper-component catalogue.
