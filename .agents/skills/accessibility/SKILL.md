ABOUTME: AI agent rules for writing accessible Blazor components (WCAG 2.2 AA).
ABOUTME: Read docs/ACCESSIBILITY.md for full platform rules before applying.

# Accessibility Rules for Blazor Components

> **Standard**: WCAG 2.2 Level AA.
> **Full reference**: [`docs/ACCESSIBILITY.md`](../../../docs/ACCESSIBILITY.md)

## Purpose
Ensures every AI-generated Blazor component meets accessibility requirements.

## When This Skill Activates
- Keywords: accessibility, a11y, aria, screen reader, keyboard, focus, wcag
- File patterns: `**/*.razor`, `**/*.razor.cs`, `**/*.razor.css`
- Any new component creation or UI modification

## Non-Inferable Rules (Must Follow)

### Page Shell (Provided by MainLayout — DO NOT duplicate)
- Skip-to-content link → already in MainLayout
- `<main id="main-content" tabindex="-1">` → already in MainLayout
- `<header>` landmark → already in MainLayout
- `<nav aria-label="Sidebar navigation">` → already in MainLayout
- ARIA live regions (polite + assertive) → already in MainLayout
- Focus-on-navigate → already in MainLayout code-behind
- `MudRTLProvider` → already wraps MudLayout in MainLayout

### Every Page Component MUST Have
- One `<h1>` element — use `<MudText Typo="Typo.h4" HtmlTag="h1">` to get `<h1>` tag with h4 visual styling
- `<PageTitle>` component for browser tab title
- Sequential heading hierarchy (h1 → h2 → h3, never skip levels)
- If no visible heading fits, use `<MudText HtmlTag="h1" Class="sr-only">Page Name</MudText>`

### Images
- `<img>` or `<MudImage>` MUST have `Alt` text describing the content
- Decorative images: `Alt=""` (empty string, not omitted)
- Never use `aria-hidden="true"` on informational images

### Interactive Elements
- Icon-only buttons: MUST have `aria-label` describing the **action**
- Form inputs: MUST have associated label (MudBlazor `Label` param or `<label for="">`)
- Custom interactive elements: MUST have `role`, `tabindex="0"`, and keyboard handlers (Enter/Space)
- Links that open new windows: add `sr-only` text "(opens in new tab)"

### Dynamic Content
- Content updating without navigation → use `IAccessibilityAnnouncerService`
  - Status/non-urgent: `AnnouncePoliteAsync(message)`
  - Errors/critical: `AnnounceAssertiveAsync(message)`
- Do NOT announce content that receives focus (focus already announces it)

### Dialogs and Modals
```csharp
// Before opening — save focus for restoration
await AccessibilityFocusService.SaveFocusAsync();
// After closing — restore focus
await AccessibilityFocusService.RestoreFocusAsync();
```
MudBlazor handles focus trap — do NOT add custom focus trap JS.

### Error and Status Messages
- Error displays → wrap in `<div role="alert">` for screen reader announcement
- Success messages → wrap in `<div role="status">` for polite announcement
- Data-loading pages → call `AnnouncerService.AnnouncePoliteAsync("{N} items loaded")` on completion
- Error states → call `AnnouncerService.AnnounceAssertiveAsync(errorMessage)` on failure

### CSS Rules
- **Banned**: `margin-left/right`, `padding-left/right`, `border-left/right`, `left/right` (positioning), `text-align: left/right`, `float: left/right`
- **Use instead**: Logical properties (`margin-inline-start/end`, `padding-inline-start/end`, `inset-inline-start/end`, `text-align: start/end`)
- **Never**: Remove focus indicators (`outline: none` without replacement)
- **Screen reader text**: Use `class="sr-only"` (defined in utilities.css)
- **Target size**: All interactive elements ≥ 24×24 CSS px (use `--isl-target-min: 1.5rem`)

### Color
- Never use color alone to convey information
- Text contrast: 4.5:1 minimum (3:1 for large text ≥24px or ≥18.67px bold)
- Non-text contrast: 3:1 minimum for UI components and graphical objects
- Palette colors are WCAG AA compliant — use `--mud-palette-*` CSS variables

### Keyboard
- All interactive elements must be reachable via Tab
- Activation: Enter and/or Space
- Dismissal: Escape key for dialogs, popovers, dropdowns
- Arrow keys for composite widgets (tabs, menus, radio groups)

### RTL Support
- `MudRTLProvider` in MainLayout handles all MudBlazor components — no per-component RTL code needed
- CSS: Use logical properties ONLY (PR-4) — they auto-flip in RTL
- `MudDrawer Anchor="Anchor.Start"` auto-flips (left in LTR, right in RTL)
- Direction preference: "auto" (language-based), "ltr", or "rtl" — user-configurable

## Anti-Patterns (Blocked)
- `aria-label` on non-interactive elements (divs, spans) — use `sr-only` text instead
- `role="button"` on `<a>` tags — use `<button>` or MudBlazor button components
- `tabindex` values > 0 — disrupts natural tab order
- `aria-hidden="true"` on focusable elements — creates invisible focus traps
- Autoplaying media without user consent
- `outline: none` without a visible replacement focus style

## Services Available (DI-registered, Scoped)
- `IAccessibilityAnnouncerService` — ARIA live region announcements (`AnnouncePoliteAsync`, `AnnounceAssertiveAsync`)
- `IAccessibilityFocusService` — Focus management (`FocusAsync`, `SaveFocusAsync`, `RestoreFocusAsync`, `FocusOnNavigateAsync`)

## Common Mistakes (From Implementation)
- Adding `role="banner"` when `<header>` exists → redundant ARIA. Use native elements.
- Adding `aria-label` to non-interactive elements → use `sr-only` text instead.
- Forgetting `SaveFocusAsync()` before dialog → focus lost on close.
- Using `MudText Typo="Typo.h4"` without `HtmlTag="h1"` → renders as `<p>`, not heading.
- Using `Alt="Image"` on content images → meaningless. Use entity-derived text like `Alt="@evt.Title"`.
- Physical CSS like `padding-left` → use `padding-inline-start` for RTL support.
- `tabindex="1"` or higher → disrupts tab order. Only use `0` (natural) or `-1` (programmatic).

## Resources
- [Full component checklist](../../../docs/ACCESSIBILITY.md#component-development-checklist)
- [WCAG 2.2 criteria mapping](../../../docs/ACCESSIBILITY.md#wcag-22-aa-criteria-mapping)

## Related Documentation
- [`docs/ACCESSIBILITY.md`](../../../docs/ACCESSIBILITY.md) — Full platform rules, contrast tables, testing
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md) — MudBlazor component patterns
- [`blazor-css-isolation`](../blazor-css-isolation/SKILL.md) — CSS scoping and BEM
