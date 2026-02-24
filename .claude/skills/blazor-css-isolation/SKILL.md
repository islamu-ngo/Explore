---
name: blazor-css-isolation
description: Blazor CSS isolation patterns with BEM methodology. Covers component.razor.css scoped styling, ::deep selector for child components, and BEM class naming conventions.
type: ui
enforcement: suggest
priority: high
---

ABOUTME: CSS isolation + BEM rules for Blazor components.
ABOUTME: Read referenced resources before applying.

# Blazor CSS Isolation with BEM Methodology

> **Project-Agnostic CSS Isolation Patterns for Blazor**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Component‑scoped CSS with BEM; `::deep` only when required.

## When This Skill Activates
- Keywords: css isolation, scoped css, razor.css, ::deep, BEM
- File patterns: `**/*.razor.css`, `**/*.razor`

## Non‑Inferable Rules (Must Follow)
- `.razor.css` file must match component name (colocated).
- Prefer **BEM** naming even though isolation prevents collisions.
- Style child components in their own CSS; use wrapper pattern before `::deep`.
- `::deep` only for third‑party internals (fragile).
- Ensure `{Project}.styles.css` is referenced in host.

## Resources (Read Before Applying)
- [bem-with-isolation.md](resources/bem-with-isolation.md)
- [deep-selector-patterns.md](resources/deep-selector-patterns.md)
- [mudblazor-styling.md](resources/mudblazor-styling.md)
- [debugging-scoped-css.md](resources/debugging-scoped-css.md)

## Related Documentation
- [`docs/BLAZOR.md`](../../../docs/BLAZOR.md)
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md)
