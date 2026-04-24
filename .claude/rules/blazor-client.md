---
name: blazor-client
description: Apply when editing Explore.Blazor.Client components, styles, or UI services.
paths:
  - "Explore.Blazor.Client/**/*.cs"
  - "Explore.Blazor.Client/**/*.razor"
  - "Explore.Blazor.Client/**/*.razor.css"
related_skills: [blazor-ui-conventions, blazor-css-isolation, design-system]
related_docs: [docs/BLAZOR.md, docs/ACCESSIBILITY.md, docs/DESIGN_SYSTEM.md, docs/QUICK_REFERENCE.md]
minimum_tests: [Explore.Blazor.Client.Tests, Event.Architecture.Tests]
related_intents: [blazor-component-affordance, add-hal-link]
---
<!-- ABOUTME: Path-scoped rules for the Blazor client UI. -->
<!-- ABOUTME: Auto-loaded by Claude Code when editing files matching the `paths` glob. -->

# Blazor Client Rules

> **Applies to:** `Explore.Blazor.Client/**/*.{cs,razor,razor.css}`.
> **Authority:** Below `docs/QUICK_REFERENCE.md` and `docs/GOVERNANCE.md`; use them as the canonical source.

## Rules (Correct / Wrong)

| # | Rule | Correct | Wrong |
|---|---|---|---|
| 1 | Use HAL for action affordance | Gate edit/delete UI with `dto.HasHalLink("...")` helpers | Gate per-resource actions with `RoleHelper`, `IsInRole`, or claims |
| 2 | Default to documented render mode | Design for `InteractiveAuto` and avoid server-only assumptions | Depend on server-only state in shared client components |
| 3 | Follow MudBlazor v9 + wrapper patterns | Prefer MudBlazor and shared wrapper components with repo defaults | Recreate raw HTML or legacy MudBlazor APIs ad hoc |
| 4 | Keep CSS isolated and BEM-shaped | Use colocated `.razor.css`, wrapper elements, and scoped BEM classes | Push component styling into global CSS or bare `.mud-*` selectors |
| 5 | Use `::deep` sparingly | Limit `::deep` to third-party internals after wrapper pattern fails | Reach into child internals first or add `!important` |
| 6 | Keep accessibility structural | Preserve labels, focus visibility, logical CSS properties, and page headings | Trade semantics for visual shortcuts |

## Must-Reads for This Path

- `AGENTS.md`
- `docs/BLAZOR.md`
- `docs/ACCESSIBILITY.md`
- `docs/DESIGN_SYSTEM.md`
- `.claude/skills/blazor-ui-conventions/SKILL.md`
- `.claude/skills/blazor-css-isolation/SKILL.md`

## Anti-Patterns (Forbidden on These Paths)

- Per-resource mutation gating by local role logic instead of HAL links.
- Global unscoped MudBlazor overrides outside the approved design-system files.
- Physical CSS direction properties when logical properties are required by accessibility rules.

## Verification

- Build: `dotnet build --configuration Release --verbosity quiet`
- Tests: `Explore.Blazor.Client.Tests`, `Event.Architecture.Tests`

## Related

- Intents: `blazor-component-affordance`, `add-hal-link`
- Agents: `.claude/agents/blazor-component-architect.md`, `.claude/agents/frontend-error-fixer.md`
- Rules: `blazor-server.md`, `api-hateoas.md`, `tests.md`
