# CSS Modernization Plan

> Last Updated: 2026-03-24 (v2 — revised per architectural review)

## Executive Summary

Modernize the ISLAMU Event Blazor application's CSS architecture to leverage 2026-era CSS features, enforce proper MudBlazor V9 customization patterns, and establish a sustainable design token system.

The current codebase has a 662-line monolithic `StyleGlobal.css` with anti-pattern global MudBlazor overrides, no `@layer` specificity management, and no wrapper components (a V9 migration gap since `MudGlobal` was removed). This plan restructures CSS into a layered architecture, introduces a 3-tier design token system, creates wrapper components, and adopts modern CSS features.

**Critical design principle**: Mechanical architecture changes (file splitting, `@layer`, token reorganization) are separated from visual value changes (`oklch`, `clamp()`, border-radius) to reduce debugging scope and prevent a long "CSS limbo" period.

---

## Current State Analysis

### What Works Well
- **Custom property system**: Extensive `--isl-*` tokens for spacing, radius, shadows, typography, buttons, cards, overlays
- **BEM adoption**: Footer block, form components, drawer containers follow BEM naming
- **Theme service**: `AppearanceThemeService.cs` centralizes MudTheme with light/dark palettes, typography, layout
- **CSS isolation**: 75+ `.razor.css` files with component-scoped styles
- **Dark mode**: Full theme persistence flow (cookie -> cascading param -> BFF endpoint -> system fallback)

### What Needs Fixing

| Problem | Location | Severity |
|---|---|---|
| Global `.mud-*` overrides without scoping | `StyleGlobal.css` lines 347-371 | **High** — fragile on MudBlazor updates |
| `!important` in `.isl-popover-menu` | `StyleGlobal.css` lines 426-448 | **Medium** — anti-pattern |
| No `@layer` — specificity conflicts | All CSS files | **High** — no cascade control |
| No wrapper components (MudGlobal gap) | Entire Blazor app | **High** — V9 removed MudGlobal |
| Monolithic StyleGlobal.css (662 lines) | Single file | **Medium** — mixed concerns |
| Two separate `:root` blocks | `StyleGlobal.css` lines 1-99, 313 | **Low** — needs consolidation |
| Border radius mismatch | Skill says 12px, code has 8px | **Medium** — inconsistency |
| No container queries | All components | **Medium** — no component-level adaptation |
| Duplicate typography definitions | MudTheme + `--isl-typography-*` + `.isl-typo-*` | **Medium** — three sources of truth |
| Drawer `!important` overrides | `StyleGlobal.css` lines 456-596 | **Justified** — MudDrawer renders outside Blazor scope |

### Component Usage Inventory (Baseline)

| Component | Usages | Files | Top File |
|---|---|---|---|
| `<MudButton>` | 383 | 87 | EventList.razor (20) |
| `<MudTextField>` | 156 | 56 | OrganizationProfileSection.razor (9) |
| `<MudCard>` | 126 | 15 | EventList.razor (16) |
| `<MudIconButton>` | 68 | 25 | EventList.razor (6) |
| `DialogService` calls | 90 | 46 | EventDetail.razor.cs (10) |
| `IDialogService` injections | 46 | 46 | 1 per file |
| `MudDialogProvider` | 2 | 2 | MainLayout, SetupLayout |

---

## Architecture Decisions

### 1. `@layer` as Foundation
Cascade layers control specificity order: `@layer reset, base, tokens, mudblazor-overrides, components, utilities;`. First declared = lowest priority. **Unlayered author styles (Blazor CSS isolation, MudThemeProvider injection) beat layered styles** — this is an explicit contract, not a flaw. MudThemeProvider's `:root` variables and Blazor scoped `.razor.css` naturally take precedence.

### 2. 3-Tier Design Token System
Primitive → Semantic → Component. Current `--isl-*` tokens are flat (~40 variables). Organized tiers enable dark mode by changing only the semantic layer and prevent hardcoded values.

**Scope rules for component-tier tokens:**
- **Primitive tokens**: global (in `tokens.css`)
- **Semantic tokens**: global (in `tokens.css`)
- **Component tokens**: global ONLY when used in 3+ places; otherwise local to the component's `.razor.css`

This prevents a second global API surface from growing uncontrolled.

### 3. Wrapper Components (MudGlobal Replacement)
**AppButton**, **AppCard**, **AppTextField**, **AppIconButton** — composition wrappers using `[Parameter(CaptureUnmatchedValues = true)]` + `@attributes` splatting. NOT inheritance from MudBlazor base classes.

**AppDialog is NOT a visual wrapper.** Dialogs in MudBlazor V9 are provider + service + options concerns:
- Visual chrome → `AppDialogShell.razor` (optional frame for consistent header/footer)
- Behavior defaults → `MudDialogProvider` parameters (e.g., `DefaultFocus`)
- Per-call defaults → standardized `DialogOptions` factory
- Popover config → `PopoverOptions` in `AddMudServices()`

### 4. Exception Policy for Global `.mud-*` Overrides
**Zero unjustified** global `.mud-*` overrides — not zero absolute. Documented exceptions allowed for:
- **Provider-owned surfaces**: dialogs, popovers, overlays rendered via portals outside normal DOM hierarchy
- **Framework seams**: MudDrawerContainer, toast/snackbar containers rendering outside Blazor scope
- **Provider configuration**: behavior configured via `MudDialogProvider`, `PopoverOptions`, `SnackbarConfiguration`

Each exception requires an inline comment documenting the justification.

### 5. `oklch` as Enhancement, Not Gate
Token architecture ships first with existing hex values. Color conversion to `oklch()` happens in a later phase. No `@supports` fallbacks needed — 95%+ support and we don't target legacy browsers (development mode only). But architecture stability takes priority over color reprofiling.

### 6. Container Queries: Anonymous by Default
Use unnamed containers by default. Name containers only when nested query contexts would be ambiguous. Keep baseline flex/grid behavior outside `@container` — container queries are progressive enhancement.

### 7. `content-visibility`: Targeted Optimization Only
Apply only to: long read-heavy lists, below-fold regions, complex dashboards/tables. **Always** pair with `contain-intrinsic-size: auto [height]` to prevent layout shifts. **Never** apply to forms, dialogs, menus, or content needing immediate focus/validation. Safari caveat: Cmd+F won't find text in hidden elements.

---

## Implementation Phases

### Phase 0: Guardrails & Baselines
**Goal**: Establish safety net before any CSS architecture changes.

1. **Screenshot baselines** — Capture key pages/states (event list, event detail, create event, org profile, admin settings, landing pages, login, setup) in both light and dark mode
2. **Usage inventory** — ✅ Complete (see table above): 383 MudButton, 156 MudTextField, 126 MudCard, 68 MudIconButton, 90 DialogService calls
3. **Approved global `.mud-*` whitelist** — Document which global `.mud-*` selectors are allowed and where (drawer overrides only)
4. **CI lint rules** (manual enforcement initially, automated later):
   - No new bare `.mud-*` selectors outside approved files
   - No new `!important` outside approved files
   - No raw hex/rgb color values outside `tokens.css`

**Acceptance**: Screenshot baselines captured, inventory documented, whitelist defined, lint rules documented.

### Phase 1: Mechanical CSS Split + @layer
**Goal**: Restructure StyleGlobal.css into layered files. **Zero visual changes.** Values stay identical — only file organization and `@layer` wrapping changes.

1. Create `css/layers.css` with `@layer` declaration order
2. Extract into separate files, each wrapped in its `@layer` block:
   - `css/reset.css` — HTML reset (existing lines 101-129)
   - `css/base.css` — body defaults, scrollbar, scroll-locked
   - `css/tokens.css` — consolidated `:root` block (merge both blocks, keep all current values unchanged)
   - `css/mudblazor-overrides.css` — scoped `.mud-*` overrides + justified drawer overrides
   - `css/components.css` — `.isl-card`, `.isl-button-pill`, footer, `.isl-form-*`
   - `css/utilities.css` — `.isl-typo-*`, spacing helpers
3. Update `App.razor` / host page to import `css/layers.css`
4. Delete original `StyleGlobal.css`

**Acceptance**: All CSS split into 7 files, `@layer` order declared, build passes, screenshot diff shows **zero visual change** against Phase 0 baselines.

### Phase 2: Token Architecture + Semantic Mapping
**Goal**: Reorganize existing tokens into 3-tier structure and map to MudBlazor variables. Still **no visual value changes** — reorganization only.

1. Restructure `css/tokens.css` into three labeled sections:
   - `/* === TIER 1: PRIMITIVES === */` — raw values (`--isl-space-unit`, `--isl-radius-raw-*`, `--isl-shadow-*`)
   - `/* === TIER 2: SEMANTIC === */` — purpose aliases pointing to `--mud-palette-*` where overlapping
   - `/* === TIER 3: COMPONENT === */` — component-scoped tokens (only those used in 3+ places globally)
2. Create semantic aliases: `--isl-color-primary: var(--mud-palette-primary)`, etc.
3. Audit component `.razor.css` files — any hardcoded `--mud-palette-*` references should go through semantic tokens
4. Remove duplicate typography definitions — single source of truth through MudTheme → CSS variables

**Acceptance**: Tokens organized in 3 tiers, semantic aliases created, no hardcoded colors in component CSS, build passes, zero visual change.

### Phase 3: Wrapper Components + First Migrations
**Goal**: Create composition wrappers that absorb global `.mud-*` overrides. Begin migrating high-concentration files.

#### 3A: Wrapper Components
1. **`AppButton.razor`** — wraps `<MudButton>`, defaults: `Variant=Filled`, `Color=Primary`, `Elevation=0`. Uses `CaptureUnmatchedValues` + `@attributes`. Scoped `.razor.css` with BEM `.app-button` block + `::deep .mud-button-root` absorbing current global overrides.
2. **`AppCard.razor`** — wraps `<MudCard>`, defaults: `Elevation=0`, border. Scoped CSS with `.app-card`.
3. **`AppTextField.razor`** — generic `<T>`, wraps `<MudTextField<T>>`, defaults: `Variant=Outlined`. Composition only — no `MudFormComponent` inheritance. Audit covers: `For`, `Value`, `ValueChanged`, `Immediate`, `DebounceInterval`, `Adornment`, validation attributes.
4. **`AppIconButton.razor`** — wraps `<MudIconButton>`, defaults: `Elevation=0`, consistent sizing.
5. **Dialog convention** (not a wrapper):
   - Configure `MudDialogProvider` with `DefaultFocus=DefaultFocus.FirstChild` in MainLayout
   - Configure `PopoverOptions` in `AddMudServices()`: `TransitionDuration = 300`
   - Create `DialogOptionsFactory` static class for standardized `DialogOptions` presets (small, medium, confirmation, etc.)
   - Optional `AppDialogShell.razor` for consistent dialog chrome (header + close button + content slot + footer slot)

#### 3B: Global Override Migration
1. Move `.mud-button-root/filled/outlined` from `mudblazor-overrides.css` into `AppButton.razor.css` via `::deep`
2. Refactor `.isl-popover-menu` — remove `!important`, use MudBlazor `Class` parameter or wrapper
3. Delete migrated overrides from `mudblazor-overrides.css`
4. Document remaining justified overrides (drawer exceptions) with inline comments

#### 3C: First File Migrations (Tier 1 only)
- Migrate `EventList.razor` to use `AppButton`, `AppCard`, `AppIconButton`
- Migrate `EventDetail.razor` dialog calls to use `DialogOptionsFactory`
- Migrate `OrganizationDetails.razor` to wrapper components

**Acceptance**: 4 wrapper components + dialog convention created. Global `.mud-*` button overrides eliminated. 3 Tier-1 files migrated. Build passes. Screenshot diff within approved threshold.

### Phase 4: Visual Refinements
**Goal**: Now that architecture is stable, introduce visual value changes one concern at a time.

#### 4A: oklch Color Migration
1. Convert hex color primitives in Tier 1 tokens to `oklch()` notation
2. Replace `color-mix` in `--isl-button-ring` with `oklch`-based mixing
3. Add hover/active/disabled state tokens using `color-mix(in oklch, ...)`
4. Verify dark mode works correctly with new color values

#### 4B: Fluid Typography
1. Replace fixed `--isl-typography-*` sizes with `clamp()` values
2. Ensure MudTheme Typography in `AppearanceThemeService.cs` aligns
3. Remove breakpoint-specific typography media queries if fully replaced by `clamp()`

#### 4C: Border Radius Standardization
1. Update `AppearanceThemeService.cs` `DefaultBorderRadius` from `"8px"` to `"12px"`
2. Add `--isl-radius-input: 8px` semantic token for form elements
3. Verify MudBlazor components pick up new 12px default
4. Visual check: larger radius = larger surface (cards 12px, inputs 8px)

**Acceptance**: Colors in oklch, fluid typography, 12px/8px radius split. Screenshot diff reviewed and approved for intentional visual changes.

### Phase 5: Modern CSS Features
**Goal**: Adopt container queries, `:has()`, CSS nesting, and targeted performance features.

#### 5A: CSS Nesting
1. Refactor MainLayout.razor.css, NavMenu.razor.css — use `&` for pseudo-classes and media queries
2. Refactor 5-10 high-traffic component `.razor.css` files
3. Keep BEM selectors flat (no `&__element` — not supported natively)
4. Max nesting depth: 2 levels

#### 5B: Container Queries
1. Add anonymous `container-type: inline-size` to event list layout, form layouts, card grids
2. Convert component viewport media queries to container queries
3. Name containers only when nested contexts are ambiguous
4. Keep baseline flex/grid outside `@container` (progressive enhancement)

#### 5C: `:has()` Patterns
1. Form validation: style parent when child has `.mud-input-error`
2. Card hover: `.app-card:has(a:focus-visible)`
3. Empty state detection

#### 5D: Targeted Performance
1. `content-visibility: auto` + `contain-intrinsic-size: auto [height]` on long event lists (`:nth-child(n + 10)` pattern)
2. `contain: layout paint` on independent card components
3. Audit transitions — only `transform`/`opacity`/`background-color`
4. Remove stale `will-change` on non-animated elements
5. **Do NOT apply** to forms, dialogs, menus, or interactive content

**Acceptance**: CSS nesting in 10+ files, container queries on 5+ components, `:has()` on 3+ patterns, content-visibility on event lists. Build passes. No accessibility regressions on keyboard/focus flows.

### Phase 6: Documentation & Cleanup
**Goal**: Update project skills, docs, and decision records.

1. Update `.claude/skills/blazor-css-isolation/SKILL.md` — add `@layer`, container queries, nesting
2. Update `.claude/skills/blazor-ui-conventions/resources/theming.md` — add oklch tokens, 12px radius
3. Update `.claude/skills/blazor-ui-conventions/resources/mudblazor-usage.md` — document wrapper catalog + dialog convention
4. Update `docs/BLAZOR.md` — reflect new CSS architecture
5. Add decision record to `dev/_journal/MAJOR_DECISIONS.md`
6. Track remaining `<MudButton>` → `<AppButton>` migrations as separate follow-up work (not blocking this plan)

**Acceptance**: Skills updated, docs updated, decision journaled.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|---|---|---|
| `@layer` breaks existing specificity | High | Phase 1 is mechanical only — zero value changes, screenshot diff |
| Wrapper components miss pass-through params | Medium | `CaptureUnmatchedValues` splatting, audit existing usages per component |
| oklch conversion shifts perceived colors | Low | Phase 4 is after architecture is stable, visual review |
| Container query units differ from viewport | Low | Progressive enhancement — baseline layout works without `@container` |
| MudBlazor updates change internal CSS | Medium | `::deep` only in wrapper `.razor.css` — single update point |
| Large scope causes regressions | High | Phases strictly separated — architecture before visual changes |
| "CSS limbo" during long migration | Medium | Phase 0 guardrails + CI lint rules prevent drift |

## Success Metrics

- Zero **unjustified** global `.mud-*` overrides; documented exceptions for provider-owned/portal surfaces and framework seams
- Zero `!important` outside documented drawer exceptions
- `@layer` declaration covering all global CSS
- 4 wrapper components + dialog convention with full parameter pass-through
- 3-tier token system with component tokens scoped (global only for 3+ usages)
- Screenshot diff on defined page matrix: no critical regressions in theme switching, overlays, forms, nav, event listing
- No accessibility regressions on keyboard/focus flows
- StyleGlobal.css decomposed from 1 file (662 lines) to 7+ files (~80-100 lines each)

## Timeline Estimate

| Phase | Effort | Dependencies |
|---|---|---|
| Phase 0: Guardrails & Baselines | 1-2 hours | None |
| Phase 1: Mechanical Split + @layer | 3-4 hours | Phase 0 |
| Phase 2: Token Architecture | 3-4 hours | Phase 1 |
| Phase 3: Wrapper Components | 6-8 hours | Phase 2 |
| Phase 4: Visual Refinements | 3-4 hours | Phase 3 |
| Phase 5: Modern CSS Features | 5-7 hours | Phase 3 (can overlap Phase 4) |
| Phase 6: Documentation | 2-3 hours | Phase 4, 5 |
| **Total** | **23-32 hours** | |
