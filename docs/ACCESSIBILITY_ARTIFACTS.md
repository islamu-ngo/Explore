ABOUTME: Public-facing accessibility artifacts — statement, AT matrix, test evidence, release gate checklist.
ABOUTME: Templates to be finalized before first public release.

# Accessibility Artifacts

> **Audience:** Operators | Contributors | AI agents
> **Status:** Mixed
> **Owner:** Frontend
> **Last Verified:** 2026-05-06
> **Source Anchors:** `docs/ACCESSIBILITY.md`, `docs/TESTING.md`, `Event.Architecture.Tests/AccessibilityConventionTests.cs`, `Explore.Blazor.Client.Tests/Accessibility/SharedComponentAccessibilityTests.cs`

This page is a release-readiness artifact set. Keep the statement, assistive-technology matrix, evidence summary, and release gate checklist here, but do not publish the template sections as current conformance evidence until the release owner fills in the release-specific values.

---

## Accessibility Statement Template

> **Publication Status:** Unreleased template. Replace this line with a release date before publishing a public accessibility statement.

ISLAMU Event Platform is committed to ensuring digital accessibility for people with disabilities. We are continually improving the user experience for everyone, and applying the relevant accessibility standards.

### Conformance Status

We aim to conform to the Web Content Accessibility Guidelines (WCAG) 2.2, Level AA. These guidelines explain how to make web content more accessible for people with disabilities and more user-friendly for everyone.

### Measures Taken

- Automated accessibility testing integrated into CI pipeline (bUnit + architecture convention tests)
- WCAG AA compliant color contrast across light and dark themes
- Semantic HTML landmarks and heading structure on all pages
- Keyboard navigation support for all interactive elements
- Screen reader live region announcements for dynamic content
- Focus management for dialogs and modals (save/restore pattern)
- Full RTL (right-to-left) layout support for Arabic and other RTL languages
- Skip-to-content navigation link
- Reduced motion support via `prefers-reduced-motion` media query
- Windows High Contrast mode support via `forced-colors` media query

### Known Limitations

- Some advanced form validation states are not announced to screen readers (`aria-invalid` not yet set by MudBlazor)
- Browser-level accessibility testing (Playwright + axe-core) not yet automated
- Not all data-loading pages announce completion to screen readers

### Feedback

We welcome your feedback on the accessibility of ISLAMU Event Platform. Please let us know if you encounter accessibility barriers:
- Email: Configure the public accessibility support address before publishing.
- Contact form: Configure the public accessibility contact URL before publishing.

We try to respond to accessibility feedback within 5 business days.

### Technical Specifications

This website relies on the following technologies:
- HTML5
- CSS3 (with logical properties for RTL support)
- JavaScript (ES modules)
- Blazor (.NET 10) with MudBlazor v9 component library
- WAI-ARIA 1.2

---

## Supported Assistive Technology Matrix

| AT | Browser | OS | Status | Notes |
|----|---------|----|--------|-------|
| NVDA 2024+ | Chrome | Windows | Primary | Recommended screen reader for testing |
| NVDA 2024+ | Edge | Windows | Supported | Chromium-based, similar to Chrome |
| JAWS 2024+ | Edge | Windows | Supported | Enterprise screen reader |
| VoiceOver | Safari | macOS | Supported | macOS built-in screen reader |
| VoiceOver | Safari | iOS | Supported | Mobile screen reader |
| TalkBack | Chrome | Android | Best effort | Mobile screen reader |
| Narrator | Edge | Windows | Best effort | Windows built-in |

**Keyboard-only**: Fully supported in all modern browsers.
**Magnification**: Tested with Windows Magnifier and macOS Zoom (up to 400%).
**High Contrast**: Windows High Contrast mode supported via `forced-colors` CSS.

---

## Test Evidence Summary Template

The table below is a release evidence template. Replace the `Unverified template` entries with the actual release validation date and test output before using it as public evidence.

### Automated Test Results

| Test Suite | Pass | Fail | Skip | Date |
|------------|------|------|------|------|
| Architecture Convention Tests (a11y) | 8 | 0 | 0 | Unverified template |
| bUnit Component A11y Tests | 6 | 0 | 1 | Unverified template |

### Manual Test Results

| Flow | NVDA+Chrome | JAWS+Edge | VoiceOver+Safari | KB-only |
|------|-------------|-----------|------------------|---------|
| Browse events (anonymous) | — | — | — | — |
| Event registration | — | — | — | — |
| Dialog open/close/focus | — | — | — | — |
| User settings | — | — | — | — |
| Admin panel | — | — | — | — |
| Dark mode | — | — | — | — |
| RTL (Arabic) | — | — | — | — |

Legend: ✅ Pass | ⚠️ Minor issues | ❌ Blocking | — Not yet tested

### axe-core Scan Results

> Deferred until Playwright E2E infrastructure is established.

---

## Release Gate Checklist

Before every public release, verify:

### Automated (CI enforced)
- [ ] All architecture convention tests pass (h1, landmarks, skip-link, live regions)
- [ ] All bUnit accessibility tests pass
- [ ] Build succeeds with 0 errors

### Manual (Pre-release)
- [ ] Keyboard-only: Complete primary flow (browse → view → register) without mouse
- [ ] NVDA + Chrome: Navigate homepage, open event, interact with dialog
- [ ] Focus management: Dialog open/close restores focus to trigger element
- [ ] Color contrast: Spot-check in both light and dark mode
- [ ] RTL: Switch to Arabic, verify layout mirrors correctly
- [ ] Reduced motion: Enable in OS settings, verify no animations
- [ ] Skip link: Tab to first element, verify skip-to-content link appears and works

### Documentation
- [ ] Accessibility statement is current
- [ ] Debt register reviewed — no critical items past target date
- [ ] ACCESSIBILITY.md updated with any new patterns
- [ ] Known limitations section in statement reflects current state
