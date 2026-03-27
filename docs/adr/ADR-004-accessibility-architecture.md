ABOUTME: Decision record for the accessibility-first architecture with service contracts and convention tests.
ABOUTME: Covers WCAG 2.2 AA compliance strategy, JS interop services, and automated enforcement.

# ADR-004: Accessibility Architecture

- **Status:** Accepted
- **Date:** 2026-02
- **Deciders:** Core team

## Context

The platform targets diverse global communities where assistive technology usage varies widely. WCAG 2.2 AA compliance is a hard requirement. Manual accessibility audits are expensive and catch issues late. The team needed an architecture that makes accessibility the default rather than an afterthought.

## Decision

Adopt a layered accessibility architecture enforced by automated convention tests:

### Service Contracts

Two scoped Blazor services provide accessibility primitives via JS interop:

- **IAccessibilityAnnouncerService** — `AnnouncePoliteAsync(message)`, `AnnounceAssertiveAsync(message)`. Manages ARIA live regions for dynamic content changes.
- **IAccessibilityFocusService** — `FocusAsync(selector)`, `FocusByIdAsync(elementId)`, `FocusMainContentAsync()`, `FocusOnNavigateAsync()`, `SaveFocusAsync()`, `RestoreFocusAsync(fallback?)`, `GetPreferredMotionAsync()`. Manages focus with `requestAnimationFrame` timing and `tabindex="-1"` injection.

Both services degrade gracefully — JS interop failures are logged as warnings, never thrown.

### Page Shell Contract

`MainLayout` provides the accessibility scaffold: skip-link to `#main-content`, `<main id="main-content" tabindex="-1">`, `<header>`, `<nav aria-label="Sidebar navigation">`, dual ARIA live regions (polite + assertive with `aria-atomic`). `FocusOnNavigateAsync()` fires on `LocationChanged`.

### Convention Tests

Architecture tests in `AccessibilityConventionTests` enforce at build time:

- Routable pages must contain `<h1>` (excludes wrapper pages).
- `MainLayout` must have skip-link, main landmark, header, nav, ARIA live regions.
- Scoped CSS must not use physical direction properties (`margin-left/right`, `left:/right:`) — advisory, Phase 5 RTL fix.

### CSS Direction Ban

Physical direction properties (`margin-left`, `padding-right`, `left:`, `right:`) are banned in scoped CSS to prepare for RTL support. Use logical properties (`margin-inline-start`, `inset-inline-start`) instead.

## Consequences

1. Accessibility violations are caught at build time, not in production.
2. New pages automatically inherit the accessibility scaffold from `MainLayout`.
3. The JS interop dependency means SSR-only pages cannot use announcer/focus services.
4. Convention tests must be maintained as new patterns emerge.
5. RTL readiness is enforced early, even before RTL locales are supported.

## Related

- [ACCESSIBILITY.md](../ACCESSIBILITY.md) — full accessibility reference
- [ACCESSIBILITY_ARTIFACTS.md](../ACCESSIBILITY_ARTIFACTS.md) — test evidence and release gates
- [ADR-003](ADR-003-css-layer-architecture.md) — CSS layer architecture
