# ::deep Selector Patterns

`::deep` is an escape hatch for styling nested third-party internals from isolated CSS.

## Use Sparingly

- Use for MudBlazor/third-party internals when no `Class`/parameter option exists.
- Do not use as a first choice for styling your own child components.

## Example

```css
.event-dialog ::deep .mud-dialog-content {
    padding: 20px;
}
```

## Preferred Alternatives

- Style child components in their own `.razor.css` files.
- Pass class names through component parameters.
- Wrap child components in a container and style the container descendants.
