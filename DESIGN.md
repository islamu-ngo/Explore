ABOUTME: Root design-system contract for the ISLAMU Event Blazor experience.
ABOUTME: Codifies existing MudBlazor tokens, wrapper components, and support-access UI primitives.

# ISLAMU Event Design System

## 1. Atmosphere & Identity

ISLAMU Event is a quiet operational command center for event teams, platform administrators, and support staff. The signature is trustworthy density: clear surfaces, restrained status color, and explicit action affordances that make administrative work feel accountable rather than flashy.

## 2. Color

Colors flow through MudBlazor palette variables and the `--isl-*` semantic aliases defined in `Explore.Blazor/wwwroot/css/tokens.css`.

| Role | Token | Source | Usage |
|------|-------|--------|-------|
| Primary action | `--isl-color-primary` | `--mud-palette-primary` | Filled buttons, focus rings, links |
| Secondary action | `--isl-color-secondary` | `--mud-palette-secondary` | Secondary controls |
| Surface | `--isl-color-surface` | `--mud-palette-surface` | Cards, panels, dialogs |
| Background | `--isl-color-background` | `--mud-palette-background` | Page canvas |
| Text primary | `--isl-color-text` | `--mud-palette-text-primary` | Headings and body |
| Text secondary | `--isl-color-text-secondary` | `--mud-palette-text-secondary` | Metadata and supporting labels |
| Border | `--isl-color-border` | `--mud-palette-lines-default` | Card and panel outlines |
| Divider | `--isl-color-divider` | `--mud-palette-divider` | Section separators |
| Error | `--isl-color-error` | `--mud-palette-error` | Destructive states |
| Success | `--isl-color-success` | `--mud-palette-success` | Completed states |
| Warning | `--isl-color-warning` | `--mud-palette-warning` | Risk, support-access active state |
| Info | `--isl-color-info` | `--mud-palette-info` | Neutral informative states |

Rules:
- Use semantic tokens in component CSS; raw palette values live only in token/theme sources.
- Color cannot be the only signal for status. Pair warning/error/success with text and iconography.
- Support-access active UI uses warning semantics, not primary branding, because it represents elevated risk.

## 3. Typography

Typography follows the fluid scale in `tokens.css`.

| Level | Token | Usage |
|-------|-------|-------|
| H1 | `--isl-typography-h1-*` | Page titles |
| H2 | `--isl-typography-h2-*` | Major sections |
| H3 | `--isl-typography-h3-*` | Panel headings |
| H4-H6 | `--isl-typography-h4-*` through `--isl-typography-h6-*` | Compact section headings |
| Body | `--isl-typography-body1-*` | Primary copy |
| Body small | `--isl-typography-body2-*` | Secondary copy and dense rows |
| Button | `--isl-typography-button-*` | Button labels |
| Caption | `--isl-typography-caption-*` | Metadata |
| Overline | `--isl-typography-overline-*` | Rare labels |

Font stack:
- Primary: `--isl-font-family-primary`
- Secondary: `--isl-font-family-secondary`

Rules:
- Every page has one structural `h1`.
- Body text stays at or above the existing body scale.
- Overline labels are rare; status banners should use direct prose rather than decorative small caps.

## 4. Spacing & Layout

Spacing uses the 4px grid from `--isl-space-1` through `--isl-space-16`.

| Token | Value | Usage |
|-------|-------|-------|
| `--isl-space-1` | 4px | Tight icon or status gaps |
| `--isl-space-2` | 8px | Inline groups |
| `--isl-space-3` | 12px | Compact padding |
| `--isl-space-4` | 16px | Standard component padding |
| `--isl-space-5` | 20px | Comfortable inline blocks |
| `--isl-space-6` | 24px | Page and panel padding |
| `--isl-space-8` | 32px | Section spacing |
| `--isl-space-10` | 40px | Large section separation |
| `--isl-space-16` | 64px | Major page rhythm |

Layout rules:
- Use CSS Grid or MudBlazor layout primitives for multi-column work.
- Component CSS uses logical properties for RTL support.
- Page sections are full-width or constrained layouts; nested cards are avoided.

## 5. Components

### AppButton
- Structure: wrapper over `MudButton`.
- Variants: filled, outlined, text.
- Spacing: `--isl-button-padding-x`, `--isl-button-padding-y`.
- States: default, hover, active, focus, disabled.
- Accessibility: visible focus, real button/link semantics, optional icon text stays readable.
- Motion: tokenized background, border, shadow, and opacity transitions.

### AppCard
- Structure: wrapper over `MudCard`.
- Variants: flat default, outlined when separation is required.
- Spacing: caller-owned content padding via tokenized layout classes or component CSS.
- States: default, hover only when clickable.
- Accessibility: cards are not interactive unless rendered with real button/link semantics.
- Motion: no decorative movement.

### AppTextField
- Structure: wrapper over `MudTextField`.
- Variants: outlined default.
- Spacing: label above input, helper/error text below.
- States: default, focus, disabled, error.
- Accessibility: labels are mandatory; placeholder-only labels are not allowed.
- Motion: MudBlazor focus and validation transitions only.

### AppIconButton
- Structure: wrapper over `MudIconButton`.
- Variants: default, primary, error, warning.
- Spacing: minimum target size follows `--isl-target-min`.
- States: default, hover, active, focus, disabled.
- Accessibility: icon-only actions require `aria-label`.
- Motion: tokenized hover/active feedback.

### SupportAccessBanner
- Structure: shell-level status banner with warning icon, active target summary, expiry, mode, and stop action.
- Variants: read-only active, write active, stopping, error.
- Spacing: `--isl-space-3` mobile padding, `--isl-space-4` desktop padding, `--isl-space-2` inline gaps.
- States: active, loading stop, stop failed, expired/cleared.
- Accessibility: `role="status"` for normal updates, assertive announcement only when stop fails or session expires.
- Motion: opacity/transform entry only; no repeated animation.

### SupportAccessStatusChip
- Structure: compact icon/text chip used inside admin surfaces.
- Variants: read-only, write, expired, revoked.
- Spacing: `--isl-space-1` icon gap, `--isl-space-2` inline padding.
- States: default, focus when interactive, disabled.
- Accessibility: status text must remain visible; color is supplementary.
- Motion: none.

## 6. Motion & Interaction

| Type | Duration | Easing | Usage |
|------|----------|--------|-------|
| Micro | 150ms | ease | Button, chip, and hover feedback |
| Standard | 250-300ms | ease-in-out | Drawer/panel transitions |
| Dock | `--isl-dock-motion-duration` | `--isl-dock-motion-easing` | Shell dock panels |

Rules:
- Animate only `transform`, `opacity`, `filter`, color, border-color, or box-shadow.
- Respect `prefers-reduced-motion`.
- Motion must communicate state or affordance; support-access risk indicators do not pulse or distract.

## 7. Depth & Surface

Strategy: mixed, with flat cards by default and tokenized shadows only for overlays, menus, and genuinely elevated surfaces.

| Level | Token | Usage |
|-------|-------|-------|
| Border | `--isl-card-border` | Ordinary cards and panels |
| Hover border | `--isl-card-hover-border-color` | Clickable cards |
| Subtle shadow | `--isl-shadow-xs` / `--isl-shadow-sm` | Light elevation |
| Dialog/popover | `--isl-shadow-md` / `--isl-shadow-lg` | Overlays and menus |

Rules:
- Ordinary admin content should prefer borders and tonal separation over heavy shadows.
- Support-access banner uses warning-toned surface treatment plus border, not high elevation.
- MudBlazor `.mud-*` overrides remain limited to `mudblazor-overrides.css`.
