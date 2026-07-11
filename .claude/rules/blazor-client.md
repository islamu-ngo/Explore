---
name: blazor-client
description: Apply when editing Explore.Blazor.Client components, styles, or UI services.
paths:
  - "src/Explore.Blazor.Client/**/*.cs"
  - "src/Explore.Blazor.Client/**/*.razor"
  - "src/Explore.Blazor.Client/**/*.razor.css"
related_skills: [blazor-ui-conventions, blazor-css-isolation, design-system]
related_docs: [docs/BLAZOR.md, docs/ACCESSIBILITY.md, docs/DESIGN_SYSTEM.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Explore.Blazor.Client.Tests, Event.Architecture.Tests]
related_intents: [blazor-component-affordance, add-hal-link]
---

# Blazor Client Rules

## Applies To
- `src/Explore.Blazor.Client/**/*.{cs,razor,razor.css}`

## Path-Specific Constraints
- **Render Mode**: Default to `InteractiveAuto`. Avoid assumptions about server-only state in shared client components.
- **MudBlazor v9**: Use MudBlazor v9 APIs exclusively. Prefer repo-standard wrapper components over raw MudBlazor controls.
- **CSS Isolation (BEM)**: Every `.razor` file should have a matching `.razor.css`. Use BEM naming for scoped classes.
- **Deep Selectors**: Use `::deep` only as a last resort for third-party component overrides.
- **Accessibility**: Structural semantics (headings, focus, labels) take precedence over visual shortcuts.

## Must Read
- [docs/QUICK_REFERENCE.md#critical-rules](../../docs/QUICK_REFERENCE.md#critical-rules) (Rule #21)
- [docs/BLAZOR.md](../../docs/BLAZOR.md)
- [docs/ACCESSIBILITY.md](../../docs/ACCESSIBILITY.md)

## Verification
- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests`

## Related
- Intents: `blazor-component-affordance`, `add-hal-link`
- Agents: `presentation-engineer-agent.md`, `quality-verifier-agent.md`
- Rules: `blazor-server.md`, `api-hateoas.md`, `tests.md`
