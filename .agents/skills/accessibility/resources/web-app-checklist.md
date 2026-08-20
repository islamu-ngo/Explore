<!-- ABOUTME: WCAG 2.2 AA implementation checklist for the project's responsive multilingual web application. -->
<!-- ABOUTME: Covers perceivable, operable, understandable, robust, RTL, and responsive behavior. -->

# Web App Accessibility Checklist

This checklist operationalizes the project's WCAG 2.2 AA target. It guides implementation and review; completing it does not certify conformance.

## Perceivable

- Give non-text content an equivalent: meaningful alternatives for content images, empty alternatives for decorative images, and captions/transcripts or audio descriptions for media as applicable.
- Encode headings, lists, tables, labels, instructions, and relationships semantically rather than through size, position, or color alone.
- Keep normal text at least `4.5:1`, large text at least `3:1`, and meaningful UI boundaries, states, focus indicators, and graphics at least `3:1` against adjacent colors.
- Do not use color, shape, location, orientation, or sound as the sole instruction or status cue.
- Support text resize and browser zoom without loss of content or function, and reflow narrow layouts without two-dimensional scrolling except where the content is inherently two-dimensional.
- Preserve content when users increase text spacing; avoid fixed heights and clipping around text.

## Operable

- Make every action available by keyboard in a logical order, with no keyboard trap and a visible focus indicator that author-created sticky or overlay content does not entirely obscure.
- Use `tabindex="0"` only to add an unavoidable custom control to natural order and `tabindex="-1"` for programmatic focus; never use a positive value.
- Meet the project minimum `24×24` CSS-pixel target using `--isl-target-min`; do not rely on WCAG spacing exceptions to shrink project controls.
- Provide a single-pointer alternative for drag-only reordering, resizing, sliders, carousels, maps, or dock layouts unless dragging is essential.
- Respect `prefers-reduced-motion`, provide controls for moving or auto-updating content, avoid unsafe flashing, and do not make hover the only way to reveal required content.
- Warn about timeouts and provide extension or recovery where the user can control the limit.

## Understandable

- Use descriptive page titles, headings, labels, instructions, button names, and link purposes that remain clear out of visual context.
- Keep navigation, help, and repeated controls consistent across pages and responsive variants.
- Identify errors in text, associate them with the relevant control, suggest correction when known, and require review or confirmation for irreversible legal, financial, privacy, or destructive submissions.
- Avoid requiring users to re-enter information already supplied in the same process unless necessary for security or the data is no longer valid.
- Do not make authentication depend on memory puzzles or disabled copy/paste; support password managers and an accessible alternative to cognitive tests.
- Localize visible labels, accessible names, error messages, and status announcements together.

## Robust

- Ensure each interactive component exposes an accurate accessible name, role, value, and state, including expanded, selected, checked, pressed, busy, invalid, and disabled where applicable.
- Prefer native HTML so the browser supplies baseline semantics and keyboard behavior; ARIA changes semantics but does not add behavior.
- Keep IDs unique and every `for`, `aria-labelledby`, `aria-describedby`, `aria-controls`, and error relationship valid after conditional rendering.
- Put status and alert content in a live region before updating it, or use the repository announcer service that owns the shell live regions.
- Test client-side updates, loading, empty, error, permission-denied, disabled, and stale-data states, not only the successful initial render.

## Responsive, Direction, And Preferences

- Keep `lang` and `dir` correct at the document level; `MainLayout`, `LanguageProvider`, and `MudRTLProvider` own the project direction flow.
- Use logical CSS properties and `start`/`end` values; do not encode physical left/right assumptions in component CSS or interaction instructions.
- Verify LTR and RTL with long translated text at narrow and wide breakpoints, including menus, dialogs, tables, rails, and focus order.
- Verify light and dark themes, `forced-colors: active`, `prefers-contrast`, and `prefers-reduced-motion` for changes that affect color, focus, or animation.
- Do not use CSS visual reordering to create a reading or focus sequence that differs from the DOM.

## WCAG 2.2 Additions To Remember

- `2.4.11 Focus Not Obscured (Minimum)`: sticky headers, drawers, cookie banners, and overlays must not entirely hide the focused component.
- `2.5.7 Dragging Movements`: provide non-drag pointer operation when dragging is not essential.
- `2.5.8 Target Size (Minimum)`: the WCAG minimum is `24×24` CSS pixels with exceptions; the project applies its target token broadly.
- `3.2.6 Consistent Help`: keep repeated help mechanisms in a consistent relative order.
- `3.3.7 Redundant Entry`: reuse or let users select information already entered in the same process.
- `3.3.8 Accessible Authentication (Minimum)`: avoid cognitive-function tests unless an allowed alternative or assistance mechanism exists.
