# BEM with CSS Isolation

Use BEM naming in `.razor.css` files to keep component styling explicit and maintainable.

## Pattern

- Block: `.event-card`
- Element: `.event-card__header`
- Modifier: `.event-card--featured`

## Example

```razor
<article class="event-card event-card--featured">
    <h2 class="event-card__title">@Title</h2>
</article>
```

```css
.event-card { border: 1px solid var(--mud-palette-lines-default); }
.event-card--featured { border-color: var(--mud-palette-primary); }
.event-card__title { color: var(--mud-palette-text-primary); }
```

## Rules

- Keep one block namespace per component.
- Prefer semantic element names over positional names.
- Use MudBlazor theme variables, not hardcoded color values.
