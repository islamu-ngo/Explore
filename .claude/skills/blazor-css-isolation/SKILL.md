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
- Keywords: css isolation, scoped css, razor.css, ::deep, BEM, @layer, container queries, CSS nesting, oklch
- File patterns: `**/*.razor.css`, `**/*.razor`, `**/wwwroot/css/*.css`

## Non‑Inferable Rules (Must Follow)
- `.razor.css` file must match component name (colocated).
- Prefer **BEM** naming even though isolation prevents collisions.
- Style child components in their own CSS; use wrapper pattern before `::deep`.
- `::deep` only for third‑party internals (fragile).
- Ensure `{Project}.styles.css` is referenced in host.
- **MudBlazor styling**: Always wrap MudBlazor components in a `<div>` with a BEM class before using `::deep` selectors.
- **Customization priority**: component params → `Class` + utility classes → MudTheme → CSS variables → wrapper components → `::deep` (last resort).
- **Anti-pattern**: Never use global CSS to override MudBlazor base classes without scoping. Never use `!important` in isolated CSS.

## Global CSS Architecture (@layer)

Global styles use `@layer` cascade ordering via `css/layers.css`:

```
@layer reset, base, tokens, mudblazor-overrides, components, utilities;
```

Each layer file is imported with `@import url("file.css") layer(name)`. The layer order determines cascade priority — later layers win over earlier ones regardless of specificity. Files:

| Layer | File | Contents |
|-------|------|----------|
| `reset` | `reset.css` | Universal margin/padding reset, link/list reset |
| `base` | `base.css` | html/body defaults, scroll-lock |
| `tokens` | `tokens.css` | 3-tier design tokens (primitives → semantic → component) |
| `mudblazor-overrides` | `mudblazor-overrides.css` | Approved `.mud-*` exceptions with justification comments |
| `components` | `components.css` | `.isl-card`, footer, `.isl-button-pill`, `.isl-popover-menu`, `.isl-form-*` |
| `utilities` | `utilities.css` | `.isl-typo-*`, `.sr-only`, a11y media queries |

## CSS Nesting (Native)

Use native CSS nesting with `&` in `.razor.css` files:

- **Nest**: pseudo-classes (`:hover`, `:focus`), pseudo-elements (`::before`), modifiers (`&.block--modifier`), media/container queries
- **Cannot concatenate BEM**: `&__element` does NOT work in native CSS (Sass-only). Keep BEM element selectors flat.
- **Max depth**: 3 levels. Cross-cutting media queries affecting 5+ selectors stay flat at file bottom.

```css
/* CORRECT */
.my-card {
    border: 1px solid var(--isl-color-border);
    &:hover { transform: translateY(-2px); }
    &.my-card--featured { border-color: var(--mud-palette-primary); }
    @media (max-width: 48em) { padding: var(--isl-space-3); }
}

/* WRONG — BEM concatenation doesn't work in native CSS */
.my-card { &__title { font-weight: 600; } }
```

## Container Queries

Use `container-type: inline-size` on layout wrappers, then `@container` queries inside child selectors for component-level responsive adaptation:

```css
.event-list__main { container-type: inline-size; }
::deep .event-grid { @container (max-width: 599.98px) { /* single column */ } }
```

- Prefer unnamed containers (anonymous) unless disambiguation is needed.
- Use container queries instead of viewport media queries for component layouts.
- Keep viewport media queries for truly page-level concerns (navigation, footer).

## Resources (Read Before Applying)
- [bem-with-isolation.md](resources/bem-with-isolation.md)
- [deep-selector-patterns.md](resources/deep-selector-patterns.md)
- [mudblazor-styling.md](resources/mudblazor-styling.md)
- [debugging-scoped-css.md](resources/debugging-scoped-css.md)

## Related Documentation
- [`docs/BLAZOR.md`](../../../docs/BLAZOR.md)
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md)
