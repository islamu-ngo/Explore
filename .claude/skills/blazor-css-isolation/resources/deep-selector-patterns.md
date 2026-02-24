ABOUTME: Guidance for using ::deep with CSS isolation.
ABOUTME: Keep usage rare and targeted to third-party internals.

# ::deep Selector Patterns

`::deep` is an escape hatch for styling nested third-party internals from isolated CSS.

## Use Sparingly

- Use for MudBlazor/third-party internals when no `Class`/parameter option exists.
- Do not use as a first choice for styling your own child components.

## Alternatives
- Style child components in their own `.razor.css`.
- Pass class names through parameters.
- Wrap child components and target the wrapper.
