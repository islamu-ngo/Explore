ABOUTME: Troubleshooting checklist for Blazor CSS isolation.
ABOUTME: Focuses on scope attributes and stylesheet generation.

# Debugging Scoped CSS

When isolated CSS does not apply, verify build output and scope attributes.

## Checklist

1. Ensure file naming matches (`Component.razor` + `Component.razor.css`).
2. Confirm scope attribute exists in DOM (`b-xxxxxxxxxx`).
3. Confirm `{Project}.styles.css` is loaded.
4. Verify selector specificity and `::deep` placement.

## Typical Issues

- Styles not applied: wrong file name or missing stylesheet link.
- Unexpected overrides: global stylesheet or high-specificity MudBlazor selectors.
- Broken after dependency update: `::deep` tied to third-party internal class changes.

## v9 Migration Notes
- MudTabs CSS class parameters were renamed (`TabPanelClass` → `TabButtonsClass`, `PanelClass` → `TabPanelsClass`). Update `::deep` selectors targeting these.
- MudSwitch/MudCheckBox/MudRadio now render content inside a `<span>` — update `::deep` selectors if targeting child elements.
- MudDrawer uses CSS `transition` instead of `animation` — custom animation overrides may need updating.
