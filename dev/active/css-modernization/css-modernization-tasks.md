# CSS Modernization — Task Checklist

> Last Updated: 2026-03-25 (v3 — all implementation phases complete)

## Phase 0: Guardrails & Baselines

- [ ] **0.1** Capture screenshot baselines — SKIPPED (no headless browser available; manual visual verification used instead)
- [x] **0.2** Document component usage inventory in this context file (DONE — see context.md)
- [x] **0.3** Define approved global `.mud-*` whitelist: drawer overrides only (event-list, filter-bar). Document in `css/mudblazor-overrides.css` header comment
- [ ] **0.4** Document CI lint rules (manual enforcement): no bare `.mud-*` outside approved files, no `!important` outside approved files, no raw hex outside `tokens.css`

## Phase 1: Mechanical CSS Split + @layer (Zero Visual Changes) ✅

- [x] **1.1** Create `css/layers.css` with `@layer reset, base, tokens, mudblazor-overrides, components, utilities;` and `@import` statements for each layer file
- [x] **1.2** Extract HTML reset into `css/reset.css`
- [x] **1.3** Extract body/scroll defaults into `css/base.css`
- [x] **1.4** Extract and consolidate both `:root` blocks into `css/tokens.css`
- [x] **1.5** Extract .isl-card, .isl-button-pill, footer, .isl-form-* into `css/components.css`
- [x] **1.6** Extract .isl-typo-* utilities into `css/utilities.css`
- [x] **1.7** Move all .mud-* overrides + drawer overrides into `css/mudblazor-overrides.css` with justification comments
- [x] **1.8** Responsive media queries distributed to appropriate layer files
- [x] **1.9** Update `App.razor` to reference `css/layers.css` instead of `css/StyleGlobal.css`
- [x] **1.10** Delete original `StyleGlobal.css`
- [x] **1.11** Build verified — 0 errors

## Phase 2: Token Architecture + Semantic Mapping (Zero Visual Changes) ✅

- [x] **2.1** Restructure `css/tokens.css` into 3 labeled tiers: PRIMITIVES, SEMANTIC, COMPONENT
- [x] **2.2** Create 12 semantic color aliases pointing to `--mud-palette-*`
- [x] **2.3** Create semantic aliases for spacing and radius
- [x] **2.4** GroupProfile.razor.css migrated (8/13 refs → semantic aliases, 5 specialized kept)
- [x] **2.5** Typography duplication documented — explicit values mirror MudTheme for SSR reliability
- [x] **2.6** All global tokens used 3+ places — no moves needed
- [x] **2.7** Build verified — 0 errors, dark mode compatible

## Phase 3: Wrapper Components + First Migrations ✅

### 3A-D: Wrapper Components
- [x] **3A** `AppButton.razor` + `.razor.css` — wraps MudButton (Filled/Primary/Elevation=0)
- [x] **3B** `AppCard.razor` + `.razor.css` — wraps MudCard (Elevation=0/border)
- [x] **3C** `AppTextField.razor` + `.razor.css` — wraps MudTextField<T> (Outlined), composition only
- [x] **3D** `AppIconButton.razor` + `.razor.css` — wraps MudIconButton

### 3E: Dialog Convention
- [x] **3E.1** MudDialogProvider DefaultFocus=FirstChild in MainLayout + SetupLayout
- [x] **3E.2** PopoverOptions.Duration=300ms in both Program.cs
- [x] **3E.3** DialogOptionsFactory.cs — Small(), Medium(), Confirmation(), Editor()
- [x] **3E.4-5** AppDialogShell.razor + .razor.css

### 3F: Global Override Cleanup
- [x] **3F.1-2** Button overrides migrated from mudblazor-overrides.css → AppButton.razor.css
- [x] **3F.4** Whitelist header updated, justification comments on all remaining blocks

### 3G: Tier 1 File Migrations (DialogOptionsFactory)
- [x] **3G.1** EventList.razor.cs — 2 DialogOptions → factory calls
- [x] **3G.2** EventDetail.razor.cs — 5 DialogOptions → factory calls
- [x] **3G.3** CreateEvent.razor.cs + EventEdit.razor.cs — 1 each → Editor()
- [x] **3G.4** MyEvents.razor.cs — 3 DialogOptions → factory calls
- [x] Build verified — 0 errors

## Phase 4: Visual Refinements ✅

### 4A: oklch Color Migration
- [x] **4A.1** Shadows + overlays converted to oklch() (6 conversions in tokens.css)
- [x] **4A.2** All color-mix(in srgb,...) → color-mix(in oklch,...) (7 sites across tokens.css + components.css)
- [x] **4A.3** 4 interaction state tokens added (hover/active/disabled/focus-ring)

### 4B: Fluid Typography
- [x] **4B.1** H1-H5 clamp() values in tokens.css (5 sizes)
- [x] **4B.2** H1-H5 clamp() aligned in AppearanceThemeService.cs

### 4C: Border Radius Standardization
- [x] **4C.1** DefaultBorderRadius 8px → 12px in AppearanceThemeService.cs
- [x] Build verified — 0 errors

## Phase 5: Modern CSS Features ✅

### 5A: CSS Nesting
- [x] **5A.1** MainLayout.razor.css — media queries + modifier nesting + oklch upgrade
- [x] **5A.2** NavMenu.razor.css — 9 :hover pairs nested + 10 oklch upgrades
- [x] **5A.3** EventList.razor.css — nesting + container queries + oklch (6 sites) + content-visibility
- [x] **5A.4** CreateEvent.razor.css — nesting + oklch upgrade
- [x] **5A.5** components.css — 4 :hover pseudo-classes nested

### 5B: Container Queries
- [x] **5B.1** EventList container-type: inline-size on .event-list__main
- [x] **5B.2** 5 viewport media queries → @container queries in EventList

### 5C: :has() Patterns
- [x] **5C.1** .isl-card:has(:focus-visible) — focus outline in components.css
- [ ] **5C.2** Form validation :has(.mud-input-error) — deferred (no .isl-form-field wrapper exists yet)

### 5D: Targeted Performance
- [x] **5D.1** content-visibility: auto + contain-intrinsic-size for event grid items (:nth-child(n+10))
- [x] **5D.3** --isl-button-transition: all → specific properties (background-color, box-shadow, border-color, opacity)
- [x] Build verified — 0 errors

## Phase 6: Documentation & Cleanup ✅

- [x] **6.1** Updated `.claude/skills/blazor-css-isolation/SKILL.md` — @layer architecture, CSS nesting, container queries
- [x] **6.2** Updated `.claude/skills/blazor-css-isolation/resources/mudblazor-styling.md` — wrapper catalog, exception policy, oklch, DialogOptionsFactory
- [x] **6.3** Updated `.claude/skills/blazor-ui-conventions/resources/theming.md` — 3-tier tokens, oklch, fluid clamp(), interaction state tokens, actual palette values
- [x] **6.4** Updated `.claude/skills/blazor-ui-conventions/resources/mudblazor-usage.md` — wrapper catalog table, DialogOptionsFactory, MudDialogProvider config
- [x] **6.5** Updated `docs/BLAZOR.md` — new Styling Architecture section (layers, isolation, wrappers, override policy)
- [x] **6.6** Added decision record to `dev/_journal/MAJOR_DECISIONS.md`
- [ ] **6.7** Follow-up: remaining MudButton/Card/TextField/IconButton → wrapper migrations (~80 files, Tier 2+3)
