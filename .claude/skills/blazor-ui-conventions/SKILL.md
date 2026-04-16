---
name: blazor-ui-conventions
description: Comprehensive UI conventions for Blazor applications. Covers MudBlazor usage, BEM methodology, theming, component structure, state management, and render modes.
type: ui
enforcement: suggest
priority: high
---

ABOUTME: Blazor UI rules (MudBlazor, render modes, routing, state).
ABOUTME: Read referenced resources before applying.

# Blazor UI Conventions & MudBlazor Guidelines

> **Project-Agnostic Blazor UI Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose
Lean rules for Blazor + MudBlazor + render modes + routing.

## When This Skill Activates
- Keywords: blazor, razor, mudblazor, render mode, dialog, state, theme
- File patterns: `**/*.razor`, `**/*.razor.cs`, `**/*.Client/**/*.cs`

## MudBlazor Version
- **Current: MudBlazor v9** (.NET 8/9/10 compatible). See `resources/v9-migration.md` for the breaking-change reference.

## Aesthetic Direction
- **Neo-minimal**: clean surfaces, generous whitespace, soft rounded corners (`12px`), subtle shadows (`Elevation 0-1`), purposeful color.
- Inspired by modern design systems: flat components, outlined inputs, muted neutrals, accent color only for CTAs.
- See `theming.md` for the full `MudTheme` config and `mudblazor-usage.md` for per-component defaults.

## Non‑Inferable Rules (Must Follow)
- Default render mode: **InteractiveAuto** (use InteractiveServer only for server‑only needs).
- Avoid `HttpContext` in InteractiveAuto/WASM.
- Use MudBlazor components over raw HTML.
- BEM class naming for custom CSS (see blazor-css-isolation).
- Use `[Parameter]` + `EventCallback` for child → parent; `ParameterState<T>` only for custom MudBlazor base components.
- Blazouter routes/guards defined centrally when used.

## Resources (Read Before Applying)
- [mudblazor-usage.md](resources/mudblazor-usage.md)
- [component-design.md](resources/component-design.md)
- [state-management.md](resources/state-management.md)
- [render-modes.md](resources/render-modes.md)
- [blazouter-routing.md](resources/blazouter-routing.md)
- [bem-methodology.md](resources/bem-methodology.md)
- [theming.md](resources/theming.md)
- [common-patterns.md](resources/common-patterns.md)

## Related Documentation
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md)
- [`blazor-bff-patterns`](../blazor-bff-patterns/SKILL.md)
- [`auth-patterns`](../auth-patterns/SKILL.md)
