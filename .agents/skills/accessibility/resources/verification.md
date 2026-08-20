<!-- ABOUTME: Verification workflow for accessibility changes in Blazor and MudBlazor UI. -->
<!-- ABOUTME: Combines repository tests, rendered-DOM assertions, and risk-based human evaluation. -->

# Accessibility Verification

## Automated Baseline

Run the smallest relevant commands, then expand when the changed surface requires it:

```bash
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/AccessibilityConventionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1
dotnet test --project tests/Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
dotnet build --configuration Release --verbosity quiet
```

The architecture lane checks project conventions such as page headings, shell landmarks/live regions, and physical-direction CSS. bUnit tests prove rendered component states. Neither lane proves real keyboard, visual, browser, or assistive-technology behavior.

## Rendered-DOM Assertions

For changed components, assert the actual element produced by MudBlazor or a wrapper:

- Native element or expected role.
- Accessible name and visible-label match.
- `for`/`id`, `aria-labelledby`, `aria-describedby`, `aria-controls`, and error relationships.
- State changes for expanded, selected, pressed, checked, busy, invalid, and disabled controls.
- Logical heading and landmark structure.
- Live-region or announcer behavior without duplicate output.
- Focus-service calls for open, close, cancel, exception, and trigger-removal paths.
- Loading, empty, failure, denied, disabled, and success states.

Do not add an accessibility-test dependency for a single check that bUnit and browser inspection already cover. Add automated browser scanning only when the repository adopts it as a maintained CI surface with a named owner and false-positive policy.

## Manual Matrix

### Every Interactive Change

- Use keyboard only: Tab and Shift+Tab order, native activation keys, arrow keys for composites, and Escape where dismissal is supported.
- Confirm focus is visible, never trapped unexpectedly, not hidden behind sticky/overlay content, and restored after transient UI closes.
- Inspect the browser accessibility tree for the changed control's name, role, description, value, and state.
- Test pointer target size and a non-drag alternative when the interaction supports dragging.

### Layout, Content, Or Theme Change

- Test narrow reflow and browser zoom through `200%` and `400%`; confirm no clipped text or lost operation.
- Test light and dark themes, long content, and text-spacing overrides.
- Test LTR and RTL at narrow and wide breakpoints.
- Test forced colors, increased contrast, and reduced motion when the change affects focus, color, animation, or transitions.

### Complex Or High-Risk Flow

- Use at least one supported screen-reader/browser combination for navigation, forms, errors, dynamic status, dialogs, and custom widgets.
- Test mobile/touch and orientation when the flow is used responsively.
- Test timeout, authentication, irreversible action, privacy, payment, or registration recovery paths when changed.
- Record the tested browser, assistive technology, viewport, direction, theme, and result in the PR or task evidence.

## Review Outcome

Report one of:

- `Verified`: named automated and manual checks passed for the changed scope.
- `Partially verified`: list the checks run and the exact untested risk.
- `Blocked`: name the failing behavior, affected users, and required correction.

Never report `WCAG compliant`, `accessible`, or `certified` from these checks alone. A conformance claim requires a defined scope, evaluation methodology, representative pages and states, and qualified human review.
