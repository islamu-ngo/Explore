ABOUTME: MudBlazor wrapper components (AppButton, AppCard, AppTextField, AppIconButton, AppDialogShell).
ABOUTME: Covers parameter defaults, display:contents pattern, and ::deep styling.

# Wrapper Components

All wrappers live in `Explore.Blazor.Client/Components/Common/`.

## Component Catalog

| Component | Wraps | Key Defaults | Parameters |
|-----------|-------|-------------|------------|
| AppButton | MudButton | Variant=Filled, Color=Primary, Size=Medium, Elevation=0 | OnClick, Disabled, FullWidth, ButtonType, StartIcon, EndIcon, Href, ChildContent |
| AppCard | MudCard | Elevation=0, Outlined=false | ChildContent, AdditionalAttributes |
| AppTextField\<T\> | MudTextField\<T\> | Variant=Outlined, Margin=None, InputType=Text | Dense, Required, Disabled, ReadOnly, Immediate, Lines=1, MaxLength, DebounceInterval, AdditionalAttributes |
| AppIconButton | MudIconButton | Color=Default, Size=Medium, Edge=False | Icon (required), OnClick, AdditionalAttributes |
| AppDialogShell | — | BEM structure | Title, HeaderContent, ChildContent (required), ActionsContent |

## Styling Pattern: display:contents + ::deep

Every wrapper uses `display: contents` so it is invisible to CSS layout — the MudBlazor inner component participates directly in the parent's flex/grid:

```css
/* AppButton.razor.css */
:host { display: contents; }

::deep .mud-button-root {
    border-radius: var(--isl-button-border-radius);
    font-size: var(--isl-button-font-size);
    transition: var(--isl-button-transition);
}
```

**Rules:**
1. Always wrap in a `<div>` or element for CSS isolation scope — `::deep` needs a parent scope.
2. Only use `::deep` to style direct MudBlazor children — never grandchildren.
3. Use design tokens in `::deep` rules, not hardcoded values.

## AppDialogShell BEM Structure

```
.app-dialog-shell
  .app-dialog-shell__header
  .app-dialog-shell__body
  .app-dialog-shell__actions
```

Used by all dialogs to enforce consistent layout. The `Title`, `HeaderContent`, `ChildContent`, and `ActionsContent` render regions map to BEM blocks.

## When to Use Wrappers vs Direct MudBlazor

| Scenario | Use |
|----------|-----|
| Standard button/card/input | Always use App* wrapper |
| Complex MudBlazor component (DataGrid, TreeView) | Use MudBlazor directly + CSS isolation |
| Dialog layout | AppDialogShell for structure |
| One-off styling need | MudBlazor `Class` parameter, NOT a new wrapper |

## Related

- `resources/appearance-builder.md` — appearance styling
- `resources/token-system.md` — tokens consumed by wrappers
