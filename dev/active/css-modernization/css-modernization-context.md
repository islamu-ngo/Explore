# CSS Modernization — Context

> Last Updated: 2026-03-24 (v2 — revised per architectural review)

## SESSION PROGRESS (2026-03-24)

### Completed
- Research: Modern CSS features (2026 browser support), BEM best practices, MudBlazor V9 theming docs
- Codebase analysis: Full CSS inventory (75+ .razor.css files, StyleGlobal.css 662 lines, theme service, token system)
- Component usage inventory: 383 MudButton, 156 MudTextField, 126 MudCard, 68 MudIconButton, 90 DialogService calls
- MudBlazor V9 migration guide analysis: MudGlobal removal, provider/options migration, wrapper patterns
- Plan v1 created, architecturally reviewed, revised to v2 with feedback integration

### In Progress
- Nothing yet — awaiting plan approval for implementation

### Blockers
- None

---

## Key Design Decisions

### 1. @layer for Specificity Management
**Decision**: `@layer reset, base, tokens, mudblazor-overrides, components, utilities;`
**Rationale**: Stops specificity drift. Layer order beats selector specificity. 95%+ support. **Explicit contract**: unlayered author styles (Blazor CSS isolation, MudThemeProvider) naturally outrank layered globals — this is by design, not a bug.

### 2. Mechanical Architecture Before Visual Changes
**Decision**: File splitting + `@layer` + token reorganization ship in separate phases from `oklch`/`clamp()`/radius changes.
**Rationale**: Combining structural + visual changes in one phase creates debugging scope explosion. If something looks wrong, you can't tell if it's the layer ordering or the color conversion. Separate phases = smaller diff surfaces.

### 3. 3-Tier Design Token System with Scope Rules
**Decision**: Primitive → Semantic → Component tiers. Component tokens are global ONLY when used in 3+ places; otherwise local to the component's `.razor.css`.
**Rationale**: Flat tokens → organized system. But unrestricted global component tokens create a second uncontrolled API surface. Strict scope rule prevents global sprawl.

### 4. oklch as Enhancement, Not Gate
**Decision**: Architecture ships first with existing hex values. oklch conversion is Phase 4 (after structure is stable).
**Rationale**: `oklch()` is 95%+ supported and the right direction, but color reprofiling should not block the architecture refactor. Token names stabilize first; values convert later.

### 5. Wrapper Components via Composition (Not Inheritance)
**Decision**: `AppButton`, `AppCard`, `AppTextField<T>`, `AppIconButton` — all using `[Parameter(CaptureUnmatchedValues = true)]` + `@attributes` splatting. No inheritance from `MudFormComponent`.
**Rationale**: MudBlazor V9's own migration guide recommends composition wrappers. Inheritance from `MudFormComponent` couples to MudBlazor internals and breaks on updates. Composition is safer, simpler, more maintainable.

### 6. AppDialog is NOT a Visual Wrapper
**Decision**: Dialogs are provider + service + options concerns, not wrapper component concerns.
**Rationale**: V9 moved dialog defaults to `MudDialogProvider` (e.g., `DefaultFocus`), popover config to `PopoverOptions`, and per-call behavior to `DialogOptions`. An `AppDialog` wrapper that tries to centralize all this would fight the framework.
**Implementation**:
- `MudDialogProvider` parameters for global behavior
- `PopoverOptions` via `AddMudServices()` for transitions
- `DialogOptionsFactory` static class for standardized presets
- Optional `AppDialogShell.razor` for visual chrome (header/footer) only

### 7. Exception Policy for Global .mud-* Selectors
**Decision**: Zero **unjustified** global `.mud-*` overrides. Documented exceptions for provider-owned/portal surfaces.
**Rationale**: Absolute "zero global overrides" is unrealistic for MudBlazor. Dialogs, popovers, overlays, and toast containers render via portals outside normal DOM hierarchy. These are framework seams, not anti-patterns — but each must be documented.
**Allowed exceptions**:
- MudDrawerContainer (renders outside Blazor scope boundary)
- Provider-rendered overlay surfaces (documented per case)

### 8. Container Queries: Anonymous by Default
**Decision**: Unnamed containers by default. Name only when nested contexts are ambiguous.
**Rationale**: Reduces naming overhead. Most components only need `container-type: inline-size` without a name. Named containers add complexity only justified for disambiguation. Baseline flex/grid stays outside `@container` — progressive enhancement.

### 9. content-visibility: Targeted, Not Broad
**Decision**: Apply only to long lists and below-fold regions. Always pair with `contain-intrinsic-size`.
**Rationale**: Excellent for rendering perf on long content, but Safari Cmd+F can't find text in hidden elements. Forms, dialogs, menus must never use it — breaks focus, validation, and interactivity. The `:nth-child(n + N)` pattern skips first visible items.

### 10. Realistic Success Metrics
**Decision**: Approved screenshot diff threshold on defined page matrix, not "zero visual regression."
**Rationale**: Pixel-perfect invariance is not an engineering criterion. Measurable: no critical regressions in theme switching, overlays, forms, nav, event listing. No accessibility regressions on keyboard/focus flows.

### 11. Border Radius: 12px Cards, 8px Inputs
**Decision**: Update MudTheme `DefaultBorderRadius` from 8px to 12px. Add `--isl-radius-input: 8px`.
**Rationale**: Skill `theming.md` specifies 12px. Visual hierarchy: larger radius = larger surface. Cards/surfaces get 12px, form inputs/buttons keep 8px for tighter feel. This is a Phase 4 visual change (after architecture stabilizes).

### 12. AppTextField<T> Parameter Audit
**Decision**: Composition wrapper must cover the parameters actually used across 156 usages in 56 files.
**Rationale**: MudTextField has many parameters. The wrapper must at minimum cover: `For`, `Value`, `ValueChanged`, `Immediate`, `DebounceInterval`, `Adornment`, `AdornmentIcon`, `AdornmentColor`, `Label`, `Placeholder`, `HelperText`, `Error`, `ErrorText`, `Required`, `Disabled`, `ReadOnly`, `InputType`, `Lines`, `MaxLength`, `Clearable`. All others pass through `@attributes`.

---

## Key Files (Existing — Will Modify)

| File | Purpose | Planned Change |
|---|---|---|
| `Explore.Blazor.Client/wwwroot/css/StyleGlobal.css` | Global CSS (662 lines) | Split into 7+ layered files, remove global .mud-* overrides |
| `Explore.Blazor.Client/Services/AppearanceThemeService.cs` | MudTheme composition | Update DefaultBorderRadius 8px -> 12px (Phase 4) |
| `Explore.Blazor.Client/Layout/MainLayout.razor` | Root layout | Configure MudDialogProvider defaults, update CSS import |
| `Explore.Blazor.Client/Layout/MainLayout.razor.css` | Layout scoped CSS | Adopt CSS nesting (Phase 5) |
| `Explore.Blazor.Client/Layout/NavMenu.razor.css` | Nav scoped CSS | Adopt CSS nesting (Phase 5) |
| `Explore.Blazor/Components/App.razor` | App root | Update stylesheet references |
| Both `Program.cs` files | Service registration | Configure `PopoverOptions` in `AddMudServices()` |

## Key Files (To Create)

| File | Phase | Purpose |
|---|---|---|
| `css/layers.css` | 1 | @layer declaration + imports entry point |
| `css/reset.css` | 1 | HTML reset layer |
| `css/base.css` | 1 | Body/scroll defaults layer |
| `css/tokens.css` | 1-2 | 3-tier design token system |
| `css/components.css` | 1 | .isl-* component styles |
| `css/utilities.css` | 1 | Typography + spacing utilities |
| `css/mudblazor-overrides.css` | 1 | Justified MudBlazor overrides only |
| `Components/Common/AppButton.razor` + `.razor.css` | 3 | MudButton composition wrapper |
| `Components/Common/AppCard.razor` + `.razor.css` | 3 | MudCard composition wrapper |
| `Components/Common/AppTextField.razor` + `.razor.css` | 3 | MudTextField composition wrapper |
| `Components/Common/AppIconButton.razor` + `.razor.css` | 3 | MudIconButton composition wrapper |
| `Components/Common/AppDialogShell.razor` + `.razor.css` | 3 | Optional dialog chrome frame |
| `Services/DialogOptionsFactory.cs` | 3 | Standardized DialogOptions presets |

---

## MudBlazor V9 Reference (Critical for Implementation)

### Theme Variable Namespaces (~196 total)
- `--mud-palette-{color}` + `-rgb`, `-text`, `-darken`, `-lighten`, `-hover` (~70 vars)
- `--mud-typography-{variant}-{property}` (~84 vars)
- `--mud-elevation-{0-25}` (26 vars)
- `--mud-zindex-{component}` (6 vars)
- Layout: `--mud-default-borderradius`, `--mud-appbar-height`

### V9 Breaking Changes (Relevant)
- `MudGlobal` **all theming properties removed**; retained non-theming: `DialogDefaults.DefaultFocus`, `MenuDefaults.HoverDelay`, `PopoverDefaults.ModalOverlay`, `TooltipDefaults`, `TransitionDefaults`
- `DefaultFocus` moved to `MudDialogProvider` parameter
- Popover transitions → `PopoverOptions` via `AddMudServices(config => { ... })`
- Dialog defaults → `DialogOptions` per call
- `PaletteLight`/`PaletteDark` both typed as `Palette` base
- Default component values changed: MudButton → `Color.Default` + `Variant.Text`, MudBaseInput → `Variant.Text` + `Margin.None`
- `GetDefaultConverter()` enables wrappers to pass `Converter = null` with automatic fallback

### Customization Priority (Official V9)
1. MudTheme C# object (controls CSS variable injection)
2. CSS variable overrides (`:root { --mud-palette-primary: ... }`)
3. `Class` parameter on components
4. Blazor CSS isolation + `::deep`
5. Global CSS class overrides (scoped, documented exceptions only)

---

## Modern CSS Features Reference

### @layer (95%+ support)
```css
@layer reset, base, tokens, mudblazor-overrides, components, utilities;
/* First declared = lowest priority. Unlayered styles win. */
```

### CSS Nesting (90%+ support)
```css
.event-card {
  border-radius: var(--isl-radius-card);
  &:hover { transform: translateY(-2px); }
  &--featured { border-color: var(--mud-palette-primary); }
}
/* NO &__element — flat BEM only. Nest pseudo-classes, modifiers, media/container. */
```

### Container Queries (93%+ support)
```css
.event-list { container-type: inline-size; }  /* anonymous by default */
@container (min-width: 600px) { .event-card { flex-direction: row; } }
```

### oklch + color-mix (95%+ support)
```css
--isl-color-primary: oklch(0.637 0.178 255);
--isl-color-primary-hover: color-mix(in oklch, var(--isl-color-primary), black 10%);
```

### content-visibility (85%+ support)
```css
.event-card:nth-child(n + 10) {
  content-visibility: auto;
  contain-intrinsic-size: auto 280px;  /* REQUIRED — prevents layout shift */
}
/* NEVER on forms, dialogs, menus. Safari: Cmd+F won't find hidden text. */
```

---

## Component Usage Inventory Detail

### Migration Priority Tiers

**Tier 1 (Highest Impact — migrate in Phase 3):**
- `EventList.razor` — 20 MudButton, 16 MudCard, 6 MudIconButton
- `EventDetail.razor.cs` — 10 DialogService calls
- `OrganizationDetails.razor` — 14 MudButton, 8 MudTextField, 14 MudCard

**Tier 2 (High Volume — track as follow-up):**
- `CreateEvent.razor` — 16 MudButton, 6 MudIconButton
- `EventEdit.razor` — 11 MudButton, 6 MudIconButton
- Admin config pages — InstanceOnboarding (14), TenantPolicySettings, GovernanceSection (12 each)

**Tier 3 (Moderate — track as follow-up):**
- Landing pages, user profile pages, organization pages
- Remaining 70+ files with 1-9 usages each

Full migration of all 383 MudButton usages across 87 files is tracked as follow-up work beyond this plan's scope. This plan creates the wrappers and migrates Tier 1 as proof of concept.
