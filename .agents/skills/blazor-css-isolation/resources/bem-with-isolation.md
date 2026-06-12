ABOUTME: BEM naming rules for isolated component styles.
ABOUTME: Keeps naming strict and uses MudBlazor theme vars.

# BEM with CSS Isolation

Use BEM naming in `.razor.css` files to keep component styling explicit and maintainable.

## Rules
- Block: `.block`
- Element: `.block__element`
- Modifier: `.block--modifier`
- One block namespace per component.
- Use MudBlazor theme variables, not hardcoded colors.
