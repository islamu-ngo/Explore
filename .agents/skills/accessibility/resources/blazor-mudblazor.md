<!-- ABOUTME: Project-native accessibility patterns for Blazor and the pinned MudBlazor version. -->
<!-- ABOUTME: Covers rendered semantics, page focus, forms, dialogs, custom widgets, and dynamic announcements. -->

# Blazor And MudBlazor Accessibility

## Evidence Order

1. Apply `docs/ACCESSIBILITY.md` and inspect the current component, service contract, rendered tests, and package pin.
2. Use Microsoft Blazor documentation for framework behavior and MudBlazor documentation for the pinned component API.
3. Use WCAG 2.2 for normative success criteria and WAI-ARIA APG for custom-widget behavior.
4. Verify the rendered DOM and browser accessibility tree; a Razor parameter alone is not evidence of the final accessible name, role, state, or relationship.

`Directory.Packages.props` currently pins MudBlazor `9.7.0`. Recheck that source before relying on version-sensitive behavior.

## Native Semantics First

- Use `<button>`, `<a href>`, `<input>`, `<select>`, headings, lists, tables, and landmarks before recreating them with generic elements and ARIA.
- Use a button for an action and a link for navigation. Buttons support Enter and Space; links use their native Enter activation.
- When a custom composite widget is unavoidable, follow its WAI-ARIA APG pattern in full, including keyboard commands, focus movement, roles, states, and accessible naming.
- Do not add `aria-label` to a generic element unless its role supports an accessible name and the name is needed. Prefer visible text or `aria-labelledby` when a visible label already exists.
- Keep the visible label text at the start of the accessible name; do not replace a useful visible label with unrelated `aria-label` text.

## Pages And Navigation

- Each routable page supplies a descriptive `PageTitle` and one project-required `h1`; use `HtmlTag="h1"` when MudText typography and semantic level differ.
- Keep heading levels sequential and DOM order meaningful without CSS reordering that changes the visual order only.
- Do not duplicate the skip link, main/header/navigation landmarks, ARIA live regions, `MudRTLProvider`, or navigation-focus behavior already owned by `MainLayout`.
- The project `IAccessibilityFocusService.FocusOnNavigateAsync()` focuses the first `h1` and falls back to `#main-content`; page components should provide the target rather than implement another navigation-focus path.
- Name multiple landmarks of the same type distinctly, using a visible heading with `aria-labelledby` when practical.

## MudBlazor Controls

- Give icon-only `MudIconButton` controls an action-oriented accessible name such as `aria-label="Delete event"`, not an icon name such as `Delete icon`.
- Prefer each component's semantic parameters (`Label`, `HelperText`, `ErrorText`, disabled/expanded/selected state) before adding raw ARIA.
- Use direct unmatched `aria-*` attributes or `UserAttributes` only when the component supports passing them to the intended DOM node; assert the rendered node because wrapper placement varies.
- Treat disabled controls deliberately: if users need the reason, provide adjacent visible text or help content that remains perceivable when the control cannot receive focus.
- Re-test MudBlazor-rendered semantics after package upgrades; component behavior and ARIA support can change between releases.

## Forms And Validation

- Every control needs a persistent programmatic label. Placeholder text is a hint, not a label.
- Associate instructions and errors with the input. MudBlazor input controls expose `HelperId` and `ErrorId` for `aria-describedby`; verify the IDs and rendered relationship when setting custom help or error content.
- Express invalid and required state programmatically and in visible text; never use color or an asterisk as the only indication.
- Use meaningful `autocomplete`, input type, and input mode values for common user data so browsers and assistive technology can identify input purpose.
- On failed submission, expose a summary or first invalid field, keep errors beside their fields, and move focus only when it helps users locate the failure.
- .NET 10 Blazor manages ARIA for its built-in validation components in supported client-validation paths, but MudBlazor and mixed render modes still require rendered-DOM verification.

## Dynamic Content

- Use `AnnouncePoliteAsync` for non-urgent result counts, completion, or saved-state messages that appear without navigation or focus movement.
- Use `AnnounceAssertiveAsync` only for urgent errors or time-sensitive state that must interrupt current speech.
- Do not announce content that receives focus, and do not repeat the same message through both a component live region and the project announcer service.
- Keep status text visible where it helps all users. `role="status"` is appropriate for advisory updates; `role="alert"` is for important, time-sensitive errors, not every validation hint.
- Collapse rapid progress updates into meaningful milestones instead of flooding the live region.

## Dialogs, Drawers, And Popovers

- MudBlazor dialogs already include a focus trap; do not add another `MudFocusTrap` or custom Tab loop.
- Set dialog initial focus intentionally. For irreversible actions, prefer the least destructive action; for long structured content, focus a meaningful heading or introductory element when the component API permits it.
- Enable `CloseOnEscapeKey` when Escape dismissal is valid, and keep a visible close or cancel button in the dialog's tab sequence.
- Call `SaveFocusAsync()` immediately before opening and `RestoreFocusAsync()` in `finally` after the dialog or drawer completes, with a stable fallback selector when the trigger may disappear.
- Popovers, menus, tabs, grids, and listboxes must expose the component's state and use the keyboard pattern for that widget, not a generic Enter/Space handler.

## Images And Icons

- Content images get concise purpose-specific alternative text; decorative images get `Alt=""`.
- Do not repeat nearby captions in full, start alternative text with “image of,” or expose file names as alternatives.
- Icons next to visible text are decorative and should not create a second accessible name; standalone interactive icons need the control's action name.
- Complex charts or diagrams need a concise alternative plus an equivalent data table or longer description.
