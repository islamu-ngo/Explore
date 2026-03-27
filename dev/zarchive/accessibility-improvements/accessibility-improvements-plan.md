ABOUTME: Strategic plan for WCAG 2.2 AA accessibility compliance across the Blazor application.
ABOUTME: Covers foundation, platform rules, component bands, testing, RTL/i18n, and governance phases.

# Accessibility Improvements Plan (v2 — Revised)

## Executive Summary

The ISLAMU Event Blazor application currently has **~25-30% WCAG 2.2 AA compliance**. Accessibility is scattered — NavMenu has solid ARIA patterns, but the remaining 100+ components/pages have near-zero accessibility attributes, no automated testing, and no screen reader support infrastructure.

This plan brings the application to **WCAG 2.2 AA compliance** through six phased workstreams. WCAG 2.2 AA is the current W3C Recommendation and represents the industry-standard engineering target. The European Accessibility Act (effective 28 June 2025) applies to certain covered products and services in the EU — while the exact scope for this application has not been confirmed by legal counsel, targeting WCAG 2.2 AA is the correct engineering posture regardless of regulatory applicability.

**Target**: WCAG 2.2 Level AA conformance across all public-facing pages and critical admin workflows.

---

## Current State Assessment

### What Works (Keep and Extend)
- **NavMenu.razor**: Best accessibility in the codebase — `aria-label`, `aria-expanded`, `aria-haspopup`, `role="button"`, `tabindex="0"`, keyboard handlers (Ctrl+K, Enter)
- **Footer.razor**: Semantic `<footer>` element, `aria-label` on social links
- **S3Image.razor / ImageUpload.razor**: Alt text parameters
- **LoginPromptDialog.razor**: `FocusAsync` on input after render, Enter key handler
- **EventList.razor**: `aria-label` on icon buttons
- **MainLayout.razor**: `aria-label="Toggle dark mode"` on theme button
- **MudBlazor**: Built-in keyboard navigation, focus traps in MudDialog, ARIA on core components

### Critical Gaps (13 Issues)
| # | Gap | WCAG Criteria | Severity |
|---|-----|---------------|----------|
| 1 | No `FocusOnNavigate` — screen readers don't announce page changes | 2.4.3 Focus Order | **Critical** |
| 2 | Zero ARIA in `Explore.Blazor.Client` pages/components | 4.1.2 Name/Role/Value | **Critical** |
| 3 | No skip-to-content link | 2.4.1 Bypass Blocks | **High** |
| 4 | No `prefers-reduced-motion` / `prefers-contrast` CSS | 2.3.3 Animation from Interactions | **High** |
| 5 | No `sr-only` / `visually-hidden` CSS utilities | 1.3.1 Info and Relationships | **High** |
| 6 | Minimal keyboard navigation (only 3 components) | 2.1.1 Keyboard | **Critical** |
| 7 | No ARIA live regions for dynamic content | 4.1.3 Status Messages | **Critical** |
| 8 | No automated accessibility testing | N/A (process) | **High** |
| 9 | `lang="en"` hardcoded — no RTL/dynamic language | 3.1.1 Language of Page | **High** |
| 10 | Missing alt text on images in most pages | 1.1.1 Non-text Content | **High** |
| 11 | No semantic landmarks (`<main>`, `<nav>`, `<header>`) | 1.3.1 Info and Relationships | **Medium** |
| 12 | No focus management (modals, dialogs, drawers) | 2.4.3 Focus Order | **High** |
| 13 | No color contrast validation in theme | 1.4.3 Contrast (Minimum) | **Medium** |

---

## Platform Rules (Enforced from Day 1)

These are not tasks — they are **permanent conventions** that all phases must follow and all future work must respect. They prevent 120+ components from drifting once the first pass is done.

### PR-1: Page Shell Contract

Every routable page MUST follow this shell contract:

```razor
@* Every page renders this structure: *@
<PageTitle>Page Name — ISLAMU Events</PageTitle>

<MudText Typo="Typo.h1">Page Heading</MudText>
@* Exactly one visible h1 per page — FocusOnNavigate targets this *@

@* Content renders inside <main id="main-content" tabindex="-1"> (provided by MainLayout) *@
```

**Rules:**
- One visible `<h1>` (or `MudText Typo="Typo.h1"`) as the first heading
- `<main id="main-content" tabindex="-1">` is provided by MainLayout — pages render inside it
- `<PageTitle>` for browser tab + screen reader window title
- Optional: page-level status region hook via `AriaLiveService`

**Rationale**: `FocusOnNavigate` only works reliably when the `h1` target is consistently present and predictable. This is a contract, not just an audit.

### PR-2: Accessibility Service Contract

`AriaLiveService` and `FocusService` are shared platform services with narrow, stable APIs:

```csharp
// IAriaLiveService — screen reader announcements
void AnnouncePolite(string message);    // Non-urgent updates (list filtered, content loaded)
void AnnounceAssertive(string message); // Critical alerts (errors, required actions)

// IFocusService — programmatic focus management
Task FocusAsync(string elementId);
Task FocusAsync(ElementReference element);
Task FocusMainContent();               // Focus <main id="main-content">
Task FocusHeading();                   // Focus the page's h1
void SaveFocus();                      // Save current activeElement
Task RestoreFocusOrFallback();         // Restore saved focus, or fall back to h1, then <main>
```

**Critical**: `RestoreFocusOrFallback` must handle the case where the saved element no longer exists in the DOM (common after rerender or modal close). Fallback chain: saved element → page `h1` → `<main>` region.

### PR-3: Component Authoring Rules

These are **hard rules** — violations must be caught in code review and automated testing:

1. **Never use clickable `<div>` or `<span>` when a real `<button>` or `<a>` can be used.** Native elements provide keyboard, focus, and ARIA behavior for free.
2. **Prefer native HTML landmarks over explicit redundant roles.** `<main>` over `<div role="main">`. `<nav>` over `<div role="navigation">`.
3. **Use ARIA only when native semantics or MudBlazor do not already solve it.** Check native → check MudBlazor → then add custom ARIA. ("No ARIA is better than bad ARIA.")
4. **All meaningful images require caller-supplied alt text.** No default fallback alt text on shared image components. Decorative images use `alt=""`.
5. **Any dynamic status change must declare its announcement priority:**
   - **Silent** — no announcement (pure visual update)
   - **Polite** — `AnnouncePolite()` (list filtered, content loaded, non-critical state change)
   - **Assertive** — `AnnounceAssertive()` (error, required action, critical alert)
6. **New CSS must use logical properties only.** `margin-inline-start` not `margin-left`. Effective immediately — do not wait for the Phase 5 migration of existing CSS.
7. **Never bind booleans directly to ARIA attributes.** Use string values: `aria-checked="@(val ? "true" : "false")"`.
8. **All interactive elements must have a minimum 24×24px target size** (WCAG 2.5.8).

### PR-4: CSS Direction Ban (Immediate)

**Effective immediately**, all new CSS must use logical properties:
- `margin-inline-start/end` not `margin-left/right`
- `padding-inline-start/end` not `padding-left/right`
- `text-align: start/end` not `text-align: left/right`
- `inset-inline-start/end` not `left/right`

Existing physical-direction CSS is migrated in Phase 5. New code must not add more.

---

## Architecture Decisions

### AD-1: Accessibility Infrastructure Layer
Create a shared accessibility service layer in `Explore.Blazor.Client` that provides:
- **`AriaLiveService`** — Injectable service for screen reader announcements (see PR-2 for API)
- **`FocusService`** — Wrapper around `IJSRuntime` for programmatic focus management (see PR-2 for API)
- **`AccessibilityJsInterop`** — Single JS interop module for focus + active-element + motion detection
- **Shared CSS utilities** — `sr-only`, `skip-link`, reduced-motion, forced-colors in global CSS

**Rationale**: Centralizes accessibility concerns, prevents duplication, ensures consistency.

**Important**: This module does NOT include generic focus-trap JS. MudBlazor's `MudFocusTrap` (used inside `MudDialog`) already provides dialog focus trapping. Custom focus-trap JS would only be introduced if a concrete non-MudBlazor overlay requires it — not as a speculative foundation feature.

### AD-2: Component Enhancement Strategy
Enhance existing components in-place. Do NOT create "accessible" variants or wrapper components.

**Rationale**: Per CLAUDE.md — "Do not create V2, Enhanced, or duplicate files — refactor existing."

### AD-3: MudBlazor-First Approach
Leverage MudBlazor's built-in accessibility before adding custom ARIA. MudBlazor components already handle:
- Focus traps in dialogs (`MudFocusTrap`) — dialogs already contain a focus trap
- Keyboard navigation in menus, selects, autocompletes
- ARIA attributes on form inputs
- RTL support via `RightToLeft` cascading parameter

Only add custom ARIA where MudBlazor doesn't cover the need (custom markup, dynamic content, page-level landmarks).

### AD-4: "No ARIA is Better than Bad ARIA"
Follow MudBlazor core team's validated rule. Before adding any ARIA attribute:
1. Check if the native HTML element already conveys the semantics
2. Check if MudBlazor already handles it
3. Only then add custom ARIA
4. Use string values for `aria-checked`, `aria-selected` (never bind booleans directly)

### AD-5: Testing Strategy
Three-tier accessibility testing:
1. **Unit tests (bUnit)**: ARIA attributes, tabindex, aria-controls/id relationships — run on every build
2. **Integration tests (Playwright + axe-core)**: Automated WCAG scanning — run on every PR
3. **Manual testing**: Screen reader (NVDA) spot checks — run before major releases

**Important**: bUnit and Playwright are the enforcement mechanisms for Razor markup issues (missing alt, keyboard handlers, form labels). NetArchTest/ArchUnit is NOT appropriate for Razor template validation — it tests .NET type relationships, not markup.

### AD-6: RTL via CultureInfo (UI Culture)
Dynamic `lang` and `dir` attributes on `<html>` driven by `CultureInfo.CurrentUICulture`:
```razor
<html lang="@CultureInfo.CurrentUICulture.Name"
      dir="@(CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr")">
```

**Why `CurrentUICulture` not `CurrentCulture`**: Formatting culture (number/date formats) and UI language are separate concerns. The `lang` attribute describes the page's display language, which is the UI culture. Use the full culture name (e.g., `ar-SA`, `en-US`) not just the two-letter code, for more precise screen reader pronunciation.

MudBlazor reads `dir="rtl"` from the HTML element and handles component RTL automatically.

---

## Phase Breakdown

### Phase 1: Foundation Infrastructure (P0 — Critical)
**Goal**: Build the accessibility plumbing that all other phases depend on. Start governance conventions (PR-1 through PR-4) immediately.

#### Task 1.1: Add FocusOnNavigate to Router
- Add `<FocusOnNavigate RouteData="routeData" Selector="h1" />` in `Routes.razor`
- Ensure every page has an `<h1>` (or MudText Typo.h1) as the first heading (per Page Shell Contract PR-1)
- **Evidence**: dotnet/aspnetcore built-in component, handles enhanced navigation automatically
- **WCAG**: 2.4.3 Focus Order
- **Acceptance**: After navigation, screen reader announces the h1. No focus on body/random element.

#### Task 1.2: Create Accessibility JS Interop Module
- Create `wwwroot/js/accessibility.js` with functions:
  - `setFocus(elementId)` — programmatic focus with retry
  - `announceToScreenReader(message, priority)` — inject into live region
  - `saveActiveElement()` / `restoreActiveElement()` — active-element save/restore helpers
  - `getPreferredMotion()` — read `prefers-reduced-motion` media query
- Register in `App.razor` or via module loading
- **NOT included**: Generic `trapFocus` / `releaseFocusTrap` — MudBlazor's `MudFocusTrap` handles dialog focus trapping. Custom trap JS only introduced later if a concrete non-MudBlazor overlay requires it.
- **Acceptance**: All functions callable from C# via `IJSRuntime`. No console errors in Server and WASM modes.

#### Task 1.3: Create AriaLiveService
- Injectable `IAriaLiveService` implementing the Service Contract (PR-2):
  - `AnnouncePolite(string message)` — non-urgent updates
  - `AnnounceAssertive(string message)` — critical alerts
- Renders a permanent ARIA live region in `MainLayout.razor` (container always in DOM, content changes inside)
- Polite region: `aria-live="polite"` | Assertive region: `role="alert" aria-live="assertive"`
- **Evidence**: Radzen pattern — live region container must exist before content changes
- **WCAG**: 4.1.3 Status Messages
- **Acceptance**: Announcements appear in NVDA/Narrator speech output. Region never removed from DOM.

#### Task 1.4: Create FocusService
- Injectable `IFocusService` implementing the Service Contract (PR-2):
  - `FocusAsync(string elementId)`
  - `FocusAsync(ElementReference element)`
  - `FocusMainContent()` — focus `<main id="main-content">`
  - `FocusHeading()` — focus page `h1`
  - `SaveFocus()` — save current `document.activeElement`
  - `RestoreFocusOrFallback()` — restore saved focus; if element is gone → `h1` → `<main>`
- **Critical**: The fallback chain in `RestoreFocusOrFallback` is essential. In real apps, the saved element is often removed from DOM after rerender or modal close.
- **Acceptance**: Programmatic focus works. Save/restore round-trips correctly. Fallback gracefully degrades when saved element is gone.

#### Task 1.5: Add Global Accessibility CSS Utilities
- Add to `wwwroot/css/app.css` (or new `accessibility.css` imported in app.css):
  - `.sr-only` class (visually hidden but screen-reader accessible)
  - `.skip-link` class (visible on focus, positioned offscreen otherwise)
  - `@media (prefers-reduced-motion: reduce)` — disable/reduce all CSS transitions and animations
  - `@media (prefers-contrast: more)` — increase border visibility, remove subtle backgrounds
  - `@media (forced-colors: active)` — ensure custom focus indicators work in Windows High Contrast
- **WCAG**: 2.3.3, 1.4.12, 1.4.1
- **Acceptance**: `.sr-only` hides visually but read by screen reader. Animations respect user preference.

#### Task 1.6: Add Skip Navigation Link
- Add skip-to-content link as first focusable element in `MainLayout.razor`
- Target: `<main id="main-content" tabindex="-1">` — the `tabindex="-1"` ensures reliable programmatic focus when the skip link is activated (browsers scroll but may not move keyboard focus without it)
- Pattern: `<a href="#main-content" class="skip-link">Skip to main content</a>`
- **WCAG**: 2.4.1 Bypass Blocks
- **Acceptance**: Tab from page load focuses skip link first. Activating it moves focus AND keyboard position to main content.

#### Task 1.7: Establish Governance Conventions Early
- Document Platform Rules PR-1 through PR-4 in `docs/ACCESSIBILITY.md` (first draft)
- Add CSS direction ban to team conventions immediately
- Create the AI agent accessibility skill (`.claude/skills/accessibility/SKILL.md`) with initial rules
- **Rationale**: Conventions should not wait until Phase 6. Start enforcing them from the first implementation task.
- **Acceptance**: `docs/ACCESSIBILITY.md` exists with platform rules. Skill file usable by AI agents.

---

### Phase 2: Semantic Structure & Landmarks (P1)
**Goal**: Proper HTML5 landmark structure and page shell contract enforcement.

#### Task 2.1: Add Semantic Landmarks to MainLayout
- Wrap top bar in `<header>`
- Wrap MudDrawer in `<nav aria-label="Main navigation">`
- Wrap main content area in `<main id="main-content" tabindex="-1">`
- Footer already uses `<footer>` — verify `role="contentinfo"` if missing
- Also update `SetupLayout.razor` with same landmark pattern
- **Note**: Prefer native landmark elements (`<main>`, `<nav>`, `<header>`) over ARIA role attributes on `<div>`. Only add explicit `role` if a landmark element cannot be used due to MudBlazor's DOM structure.
- **WCAG**: 1.3.1 Info and Relationships, 2.4.1 Bypass Blocks
- **Acceptance**: NVDA landmarks list shows: banner, navigation, main, contentinfo.

#### Task 2.2: Enforce Page Shell Contract
- Audit all ~50 pages against Page Shell Contract (PR-1):
  - Each page has exactly one visible `<h1>` (or `MudText Typo="Typo.h1"`)
  - Each page has `<PageTitle>` set
  - Heading levels do not skip (h1 → h3 without h2)
- This is not just an audit — formalize the pattern so new pages follow it automatically
- **WCAG**: 1.3.1 Info and Relationships, 2.4.6 Headings and Labels
- **Acceptance**: No heading level skips. axe-core reports zero heading-order violations. All pages have `<PageTitle>`.

#### Task 2.3: Dynamic `lang` and `dir` on HTML Element
- Replace hardcoded `lang="en"` in `App.razor` with dynamic values from `CultureInfo.CurrentUICulture`
- Use full culture name: `lang="@CultureInfo.CurrentUICulture.Name"` (e.g., `ar-SA`, `en-US`)
- Add `dir` attribute: `dir="@(CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "rtl" : "ltr")"`
- Wire to existing `LanguageProvider` / `LanguagePicker` component
- **WCAG**: 3.1.1 Language of Page
- **Acceptance**: Changing language to Arabic sets `lang="ar-SA" dir="rtl"`. MudBlazor components flip layout.

---

### Phase 3: Component-Level Accessibility (P1)
**Goal**: ARIA attributes, keyboard navigation, and focus management on all interactive components.

Phase 3 is split into three risk bands for safer delivery. Enterprise value comes from making the app **usable without a mouse first** (Band A), then **comprehensible** (Band B), then **polished** (Band C).

#### Band A: Navigation & Task Completion (Highest Priority)
> Make the app keyboard-usable. Users can complete core workflows without a mouse.

#### Task 3A.1: Dialog Accessibility Audit & Enhancement
- All MudDialog usages: verify MudBlazor's built-in focus trap is working (do NOT add custom focus trap JS)
- Verify `aria-labelledby` points to dialog title, `aria-describedby` to description
- On close: focus returns to trigger element (use `FocusService.SaveFocus` / `RestoreFocusOrFallback`)
- **Dialogs to audit**: LoginPromptDialog, ReviewDialog, all admin `*Dialog.razor` components (~16 total)
- **WCAG**: 2.4.3 Focus Order, 1.3.1 Info and Relationships
- **Acceptance**: Tab cannot escape dialog. Escape closes. Focus returns to trigger (or falls back gracefully).

#### Task 3A.2: Keyboard Navigation for Custom Interactive Elements
- Prefer replacing clickable `<div>`/`<span>` with native `<button>` or `<a>` where possible (PR-3 Rule 1)
- Where native elements cannot be used: add `role="button"`, `tabindex="0"`, `@onkeydown` (Enter/Space)
- Interactive cards (event cards, org cards): keyboard activatable
- Dropdown menus: Arrow key navigation within, Escape to close
- **WCAG**: 2.1.1 Keyboard, 4.1.2 Name, Role, Value
- **Acceptance**: Every interactive element reachable and activatable via keyboard only.

#### Task 3A.3: Form Accessibility Enhancement
- Verify MudTextField `Label` renders as accessible label (it should — verify)
- Add `aria-required="true"` where MudBlazor doesn't add it
- Validation errors: `aria-describedby` pointing to error message, `aria-invalid="true"` on invalid fields
- Form groups: use `<fieldset>` + `<legend>` for related inputs (date range, address)
- Validation summaries: wrap in `role="alert"` container
- **Evidence**: dotnet/aspnetcore Identity UI uses explicit `for`/`id` label association + `role="alert"` on validation
- **WCAG**: 1.3.1, 3.3.1 Error Identification, 3.3.2 Labels or Instructions
- **Acceptance**: Screen reader announces field label, required state, and error messages on validation.

#### Task 3A.4: Dynamic Content Announcements
- Page load spinners: announce "Loading..." and "Content loaded" via AriaLiveService
- Toast notifications (MudSnackbar): verify screen reader announcement (MudBlazor may handle this)
- Form submission results: announce success/failure
- List filtering/sorting: announce result count changes ("Showing 5 of 23 events")
- Each announcement must declare its priority per PR-3 Rule 5 (silent/polite/assertive)
- **WCAG**: 4.1.3 Status Messages
- **Acceptance**: NVDA announces loading states, toast messages, form results, and filter count changes.

#### Band B: Content Comprehension (Second Priority)
> Make the app understandable. Screen reader users can comprehend all content.

#### Task 3B.1: Image Accessibility Audit
- All `<img>`, `<MudImage>`, `<MudAvatar>` must have meaningful alt text or `alt=""` (decorative)
- S3Image.razor: remove default alt "Image" — callers must pass contextual alt text (PR-3 Rule 4)
- Event banners, org logos, user avatars: descriptive alt text from entity data
- **WCAG**: 1.1.1 Non-text Content
- **Acceptance**: axe-core reports zero `image-alt` violations. No default "Image" alt on meaningful images.

#### Task 3B.2: Status Messages and Live Region Integration
- Ensure all error states use `role="alert"` or `AnnounceAssertive()`
- Loading components use `AnnouncePolite()` for state transitions
- Data table empty states and no-results messages are announced
- **WCAG**: 4.1.3 Status Messages
- **Acceptance**: All dynamic status changes are announced per their declared priority.

#### Band C: Polish & Visual Parity (Third Priority)
> Make the app visually accessible. Color, contrast, focus, and target size compliance.

#### Task 3C.1: Theme Token Contrast Validation
- Validate MudBlazor **theme tokens** (palette) against WCAG 2.2 AA contrast requirements first
  - Text contrast: ≥ 4.5:1 (normal text), ≥ 3:1 (large text)
  - UI component contrast: ≥ 3:1 (borders, focus indicators)
- Fix at the token level — this fixes contrast everywhere at once instead of fighting it page by page
- **Rationale**: Token-level contrast checks are highest-leverage. If the palette is wrong, you fight the same issue 50 times.
- **WCAG**: 1.4.3 Contrast (Minimum), 1.4.11 Non-text Contrast
- **Acceptance**: All theme palette tokens pass AA contrast checks.

#### Task 3C.2: Visual Focus and Target Size
- Ensure color is never the sole indicator of state (add icons/text alongside color cues)
- Focus indicators: visible on all interactive elements (2px solid outline minimum)
- Target size: minimum 24×24px for all interactive targets (WCAG 2.5.8)
- Custom CSS colors in `.razor.css` files: verify contrast ratios against theme tokens
- **WCAG**: 1.4.1 Use of Color, 2.4.7 Focus Visible, 2.5.8 Target Size
- **Acceptance**: Focus ring visible on every interactive element. No color-only indicators. All targets ≥ 24×24px.

---

### Phase 4: Automated Accessibility Testing (P1 — Starts in Parallel)
**Goal**: Prevent regressions with automated WCAG scanning in CI. Starts as soon as the shell and first few core pages are fixed (does not wait for all of Phase 3).

#### Task 4.1: Add axe-core Playwright Integration Tests
- Add `Deque.AxeCore.Playwright` NuGet package to `Event.API.IntegrationTests`
- Create `AccessibilityTests.cs` with parameterized tests covering:
  - **Anonymous public pages**: Home, Events listing, Event detail, Organizations
  - **Authenticated pages**: User profile, User settings
  - **Admin workflow**: At least one admin dashboard page
  - **Dialog-heavy workflow**: Event creation or edit (with dialogs)
  - **Form-heavy workflow**: Onboarding or event creation form
  - **Dark mode**: Run a subset of pages in dark theme
  - **RTL**: Run a subset of pages with Arabic locale (Phase 5 onward)
  - **Reduced motion**: Run with `prefers-reduced-motion: reduce` emulated
- Configure axe rules: `wcag2a`, `wcag2aa`, `wcag21a`, `wcag21aa`, `wcag22aa`
- Fail on any violation
- **Evidence**: Deque official package, catches ~57% of WCAG issues automatically
- **Acceptance**: `dotnet test` runs axe scans. Zero violations on all tested pages across all experience classes.

#### Task 4.2: Add bUnit Accessibility Unit Tests
- Create accessibility test classes in `Explore.Blazor.Client.Tests`
- **This is the enforcement mechanism for Razor markup rules** (not NetArchTest):
  - `aria-expanded` matches actual expanded state
  - `aria-controls` references actual DOM `id`
  - Disabled elements have `tabindex="-1"`
  - `aria-hidden="true"` on collapsed content
  - All interactive elements have accessible names
  - All `@onclick` on non-button elements have keyboard handler
  - All images have `alt` attribute
  - All forms have associated labels
- Pattern: Follow MudBlazor `NavigationAccessibilityTests.cs` style
- **Acceptance**: bUnit tests cover all custom interactive components. Tests enforce PR-3 authoring rules.

#### Task 4.3: CI Pipeline Integration
- Add accessibility test step to existing GitHub Actions workflow
- Run Playwright axe tests on every PR
- Run bUnit accessibility tests on every build
- Generate TRX report for accessibility test results
- **Acceptance**: PR with accessibility violations cannot merge (test failure blocks).

#### Task 4.4: CSS Logical Property Linting
- Add a simple repository check (grep/script in CI) to detect new physical-direction CSS properties (`margin-left`, `margin-right`, `padding-left`, `padding-right`, `text-align: left`, `text-align: right`, `float: left`, `float: right`) in `.razor.css` files
- Warn or fail on new additions (allows existing ones until Phase 5 migration)
- **Acceptance**: New CSS with physical-direction properties flagged in CI.

---

### Phase 5: RTL and Internationalization Accessibility (P2)
**Goal**: Full RTL support for Arabic/Urdu-speaking communities. New CSS already uses logical properties (PR-4 enforced since Phase 1).

#### Task 5.1: CSS Logical Properties Migration (Existing CSS)
- Migrate all ~70 existing `.razor.css` files:
  - `margin-left/right` → `margin-inline-start/end`
  - `padding-left/right` → `padding-inline-start/end`
  - `text-align: left/right` → `text-align: start/end`
  - `float: left/right` → `float: inline-start/inline-end`
  - `left/right` positioning → `inset-inline-start/end`
- **WCAG**: 1.3.4 Orientation
- **Acceptance**: RTL toggle — all layouts mirror correctly. Zero physical-direction properties in codebase.

#### Task 5.2: MudBlazor RTL Integration
- Wire `RightToLeft` cascading parameter to `CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft`
- Test all MudBlazor components in RTL mode (drawer, navigation, forms, tables, dialogs)
- Fix any MudBlazor components that don't properly flip
- **Acceptance**: Arabic locale renders fully mirrored UI. Navigation drawer on right side.

#### Task 5.3: Bidirectional Text Handling
- Ensure mixed LTR/RTL content renders correctly (e.g., English event names in Arabic UI)
- Add `unicode-bidi` CSS for edge cases
- Test with real Arabic content
- **Acceptance**: Mixed language content readable and properly aligned.

---

### Phase 6: Documentation & Governance Completion (P2 — Started Early in Phase 1)
**Goal**: Finalize and institutionalize accessibility standards. Initial governance (PR-1 through PR-4, docs, skill) starts in Phase 1. This phase completes and refines them based on lessons learned.

#### Task 6.1: Finalize Accessibility Conventions Document
- Update `docs/ACCESSIBILITY.md` (started in Task 1.7) with:
  - Complete WCAG 2.2 AA criteria mapping
  - Finalized component accessibility checklist
  - Testing requirements (bUnit + axe-core patterns with code examples)
  - "No ARIA is better than bad ARIA" rule with examples
  - MudBlazor-first approach with decision tree
  - Page shell contract with template
  - Service contract with usage examples
  - Component authoring rules with good/bad examples
- Reference from `CLAUDE.md` documentation index
- **Acceptance**: Document is complete, reviewed, and covers all patterns from Phases 1-5.

#### Task 6.2: Finalize Accessibility Skill for AI Agents
- Update `.claude/skills/accessibility/SKILL.md` (started in Task 1.7) with:
  - Complete WCAG criteria mapping relevant to Blazor
  - Component patterns with code examples
  - Testing patterns with bUnit and Playwright code
  - Common mistakes and how to avoid them
  - Decision tree: native → MudBlazor → custom ARIA
- Reference from `CLAUDE.md` skills section
- **Acceptance**: AI agents produce accessible code by default when using this skill.

#### Task 6.3: Accessibility Debt Register
- Create and maintain `dev/active/accessibility-improvements/a11y-debt-register.md`
- Track accepted temporary exceptions with:
  - Description of the exception
  - Rationale for accepting it
  - Owner responsible for resolution
  - Target fix date
  - WCAG criteria affected
- **Rationale**: Accessibility work on a live product always has carry-forwards. Making them explicit prevents silent regression.
- **Acceptance**: Register exists, is reviewed periodically, and has no items past their target date without documented extension.

#### Task 6.4: Public-Facing Accessibility Artifacts
- Create (timeline: before first public release):
  - **Accessibility statement** — public page describing commitment, target level, known limitations, contact for issues
  - **Test evidence summary** — axe-core scan results, manual testing results, AT tested
  - **Supported AT/browser matrix** — NVDA + Chrome, JAWS + Edge, VoiceOver + Safari (at minimum)
  - **Internal release gate checklist** — must-pass before any release
- **Rationale**: Enterprise-grade delivery requires demonstrable due diligence for customers, procurement, and compliance review.
- **Acceptance**: All four artifacts exist and are kept current.

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| MudBlazor component changes break custom ARIA | Medium | Pin MudBlazor version; test upgrades separately |
| RTL migration breaks existing LTR layouts | High | Phase 5 is isolated; thorough visual regression testing |
| axe-core false positives block PRs | Medium | Configure rule exclusions for known MudBlazor patterns |
| Performance impact from live regions/JS interop | Low | Minimal — single live region, lazy JS module loading |
| Incomplete WCAG coverage (automated catches ~57%) | Medium | Manual NVDA testing before major releases; debt register for known gaps |
| Accessibility drift as new components are added | High | Platform rules enforced from Day 1; bUnit tests catch regressions; AI skill guides new code |
| Two competing focus systems (MudBlazor + custom) | Medium | No custom focus trap JS in foundation; MudBlazor-first for all dialogs |
| Saved focus element gone after rerender | Medium | `RestoreFocusOrFallback` with graceful degradation chain |

## Execution Order

| Phase | Effort | Starts | Dependencies |
|-------|--------|--------|--------------|
| Phase 1: Foundation + Initial Governance | 3-4 days | Immediately | None |
| Phase 2: Semantic Structure | 1-2 days | After Phase 1 | Phase 1 (skip link needs `<main>`) |
| Phase 3 Band A: Navigation/Task Completion | 3-4 days | After Phase 1-2 | Phase 1 (services), Phase 2 (landmarks) |
| Phase 4: Testing (starts in parallel) | 2-3 days | After Phase 2 + 3A partial | Shell + first core pages fixed |
| Phase 3 Band B: Content Comprehension | 2-3 days | After Band A | Band A complete |
| Phase 3 Band C: Visual Polish | 1-2 days | After Band B | Band B complete, theme tokens validated |
| Phase 5: RTL/i18n | 3-4 days | After Phase 2 | Dynamic lang/dir in place |
| Phase 6: Governance Completion | 1-2 days | After Phase 1-5 | All patterns established |
| **Total** | **17-24 days** | | |

## References

- [WCAG 2.2 Quick Reference](https://www.w3.org/WAI/WCAG22/quickref/)
- [WAI-ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/)
- [ASP.NET Core Blazor Routing — FocusOnNavigate](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/routing)
- [MudBlazor Focus Trap](https://mudblazor.com/components/focustrap)
- [MudBlazor Accessibility Tests](https://github.com/MudBlazor/MudBlazor/blob/dev/src/MudBlazor.UnitTests/Components/NavigationAccessibilityTests.cs)
- [Deque axe-core Playwright NuGet](https://www.nuget.org/packages/Deque.AxeCore.Playwright)
- [Radzen Notification (ARIA live pattern)](https://github.com/radzenhq/radzen-blazor/blob/master/Radzen.Blazor/RadzenNotification.razor)
- [European Accessibility Act](https://commission.europa.eu/strategy-and-policy/policies/justice-and-fundamental-rights/disability/european-accessibility-act-eaa_en)
