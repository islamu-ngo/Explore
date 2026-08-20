<!-- ABOUTME: Source register and evidence boundary for the accessibility skill refresh. -->
<!-- ABOUTME: Records official authorities used without retaining copied implementation source or prose. -->

# Accessibility Research Sources

## Authority Order

1. Repository facts: `docs/ACCESSIBILITY.md`, current Blazor code and service contracts, rendered tests, and `Directory.Packages.props`.
2. Normative web standards: WCAG and ARIA specifications from W3C.
3. Framework documentation: Microsoft Learn for the repository's .NET version.
4. Component documentation: MudBlazor documentation for the repository's pinned version.

External sources inform neutral requirements and public API behavior. Repository-native structure, services, examples, verification, and wording were designed independently; no third-party implementation source, snippets, tests, assets, or copied documentation prose were used.

## Source Register

Accessed `2026-08-20`.

| Source | URL | Used for |
|---|---|---|
| W3C WCAG 2.2 Recommendation | https://www.w3.org/TR/WCAG22/ | Normative AA criteria, including focus not obscured, dragging alternatives, target size, status messages, and accessible authentication. |
| W3C ARIA in HTML | https://www.w3.org/TR/html-aria/ | Native semantics and valid ARIA use in HTML. |
| WAI-ARIA APG Read Me First | https://www.w3.org/WAI/ARIA/apg/practices/read-me-first/ | ARIA role/behavior boundary and assistive-technology testing requirement. |
| WAI-ARIA APG Keyboard Interface | https://www.w3.org/WAI/ARIA/apg/practices/keyboard-interface/ | Focus movement and role-specific keyboard behavior for custom widgets. |
| WAI-ARIA APG Modal Dialog Pattern | https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/ | Initial focus, focus containment, Escape, labeling, close control, and return-focus behavior. |
| W3C Technique ARIA22 | https://www.w3.org/WAI/WCAG22/Techniques/aria/ARIA22 | Polite status messages and `role="status"` behavior. |
| ASP.NET Core Blazor Routing (.NET 10) | https://learn.microsoft.com/aspnet/core/blazor/fundamentals/routing?view=aspnetcore-10.0 | `FocusOnNavigate` and focusing the page `h1` after client-side navigation. |
| ASP.NET Core Blazor Forms Validation (.NET 10) | https://learn.microsoft.com/aspnet/core/blazor/forms/validation?view=aspnetcore-10.0 | Framework-managed validation ARIA in supported Blazor validation paths. |
| MudBlazor Dialog | https://mudblazor.com/components/dialog | Focus trap, `DefaultFocus`, and opt-in Escape dismissal behavior. |
| MudBlazor Focus Trap | https://mudblazor.com/components/focustrap | Existing dialog focus trap and the boundary against adding a second trap. |
| MudBlazor `MudInputControl` API | https://mudblazor.com/api/MudInputControl | `Label`, `HelperId`, `ErrorId`, and `UserAttributes` accessibility hooks. |
| MudBlazor Releases | https://github.com/MudBlazor/MudBlazor/releases/tag/v9.7.0 | Version-specific accessibility changes for the locally pinned release. |

## Evidence Limits And Refresh Triggers

- The skill is engineering guidance, not a WCAG conformance evaluation or legal certification.
- WAI-ARIA APG examples require browser and assistive-technology testing; a documented pattern is not proof of support in every combination.
- Recheck Microsoft documentation when the target framework changes.
- Recheck MudBlazor documentation, rendered markup, and tests whenever the package pin changes.
- Recheck WCAG and ARIA links before making a new conformance claim or changing the project's target standard.
