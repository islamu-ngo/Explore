# BEM Methodology - CSS Naming Conventions

> **Project-Agnostic BEM CSS Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document outlines the **BEM (Block, Element, Modifier)** methodology for naming CSS classes within Blazor applications. Adhering to BEM ensures maintainable, scalable, and understandable stylesheets, especially important in component-based UI frameworks like Blazor with MudBlazor.

---

## 1. What is BEM?

BEM stands for **Block**, **Element**, **Modifier**. It's a highly structured naming convention that helps you develop CSS that is modular, reusable, and easy to understand.

*   **Block (`.block`)**: A standalone entity that is meaningful on its own.
    *   Examples: `.header`, `.footer`, `.menu`, `.button`, `.{entity}-card`, `.user-avatar`.
*   **Element (`.block__element`)**: A part of a block that has no standalone meaning and is semantically tied to its block.
    *   Examples: `.menu__item`, `.button__icon`, `.{entity}-card__title`, `.user-avatar__image`.
*   **Modifier (`.block--modifier` or `.block__element--modifier`)**: A flag on a block or an element. It's used to change the appearance, behavior, or state of a block or element.
    *   Examples: `.button--primary`, `.button--disabled`, `.{entity}-card--featured`, `.menu__item--active`.

### Naming Syntax

*   **Block**: `block-name`
*   **Element**: `block-name__element-name` (two underscores)
*   **Modifier**: `block-name--modifier-name` (two hyphens) or `block-name__element-name--modifier-name`

---

## 2. Why BEM?

*   **Modularity**: Encapsulated styles. Styles for a block never affect other blocks.
*   **Reusability**: Blocks and elements can be reused across different parts of the application.
*   **Maintainability**: Easy to understand what a CSS rule does, where it applies, and how it relates to other components. New features are less likely to break existing styles.
*   **Scalability**: Well-suited for large projects and teams.
*   **Specificity Management**: Keeps CSS specificity low and flat, reducing the need for `!important` and making overrides predictable.

---

## 3. BEM in Blazor with MudBlazor

When working with Blazor and MudBlazor, BEM is used for custom styling that goes beyond MudBlazor's theming and utility classes.

### Example: Custom {Entity} Card

Let's say we have an {entity} card component with custom styling that extends MudBlazor's `MudCard`.

**`{Entity}Card.razor`**:
```razor
<MudCard Class="{entity}-card @(_isFeatured ? "{entity}-card--featured" : "")">
    <MudCardMedia Image="@{Entity}.ImageUrl" Height="200" Class="{entity}-card__image" />
    <MudCardContent>
        <MudText Typo="Typo.h6" Class="{entity}-card__title">@{Entity}.Title</MudText>
        <MudText Typo="Typo.body2" Class="{entity}-card__date">@{Entity}.Date.ToShortDateString()</MudText>
    </MudCardContent>
    <MudCardActions Class="{entity}-card__actions">
        <MudButton Variant="Variant.Text" OnClick="ViewDetails" Class="{entity}-card__button">
            View Details
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter] public {Entity}Dto {Entity} { get; set; } = null!;
    [Parameter] public bool IsFeatured { get; set; }

    private bool _isFeatured => IsFeatured; // Use local state for conditional classes
    // ...
}
```

**`{Entity}Card.razor.css`** (scoped CSS for the component):
```css
/* Block: .{entity}-card */
.{entity}-card {
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: var(--mud-default-border-radius);
    box-shadow: var(--mud-shadow-1);
    transition: all 0.2s ease-in-out;
}

.{entity}-card:hover {
    box-shadow: var(--mud-shadow-3);
    transform: translateY(-2px);
}

/* Element: .{entity}-card__image */
.{entity}-card__image {
    object-fit: cover;
}

/* Element: .{entity}-card__title */
.{entity}-card__title {
    font-weight: 600;
    margin-bottom: 8px;
    /* Using MudBlazor's CSS variables for theme integration */
    color: var(--mud-palette-text-primary);
}

/* Element: .{entity}-card__date */
.{entity}-card__date {
    font-size: 0.875rem;
    color: var(--mud-palette-text-secondary);
}

/* Element: .{entity}-card__actions */
.{entity}-card__actions {
    padding: 8px 16px;
    justify-content: flex-end;
}

/* Element: .{entity}-card__button */
/* Note: MudButton already provides extensive styling, this is for subtle overrides */
.{entity}-card__button {
    text-transform: uppercase;
}

/* Modifier: .{entity}-card--featured */
.{entity}-card--featured {
    border-color: var(--mud-palette-primary); /* Highlight featured items */
    box-shadow: var(--mud-shadow-6);
}

.{entity}-card--featured .{entity}-card__title {
    color: var(--mud-palette-primary-text); /* Ensure good contrast */
}
```

### Best Practices for BEM with Blazor Scoped CSS:

*   **Scoped CSS (`.razor.css`)**: Always use Blazor's scoped CSS feature for component-specific styles. This helps prevent conflicts and automatically applies unique identifiers to your CSS rules.
*   **Deep Selectors (`::deep`, `::part`)**: Use `::deep` or `::part` when you need to style elements *inside* a MudBlazor component's shadow DOM or internal structure.
    ```css
    /* Example: Styling the text input within a MudTextField */
    .my-form-field ::deep .mud-input-control-input {
        color: var(--mud-palette-info);
    }
    ```
    *Note*: `::deep` is a deprecated combinator for shadow DOM piercing. While it works in Blazor scoped CSS today, `::part` is the web standard for styling parts of web components. MudBlazor may not expose many `::part`s currently, but it's good to be aware.
*   **MudBlazor Utility Classes**: Leverage MudBlazor's utility classes (`mb-4`, `pa-2`, `d-flex`, `justify-center`) for common spacing, padding, and flexbox layouts before resorting to custom BEM classes.
*   **CSS Variables**: Use MudBlazor's CSS variables (e.g., `var(--mud-palette-primary)`, `var(--mud-typography-h6-size)`) to integrate seamlessly with the application's theming system.
*   **Minimal Overrides**: Avoid overly broad overrides of MudBlazor's core classes. If a component doesn't offer enough customization, apply BEM classes to a wrapper or directly to the component for targeted styling.

---

## 4. Common BEM Mistakes to Avoid

*   **Over-nesting**: Keep selectors flat. Avoid deeply nested CSS rules.
    ```css
    /* BAD */
    .{entity}-card .{entity}-card__content .{entity}-card__title {
        /* ... */
    }

    /* GOOD */
    .{entity}-card__title {
        /* ... */
    }
    ```
*   **Block Modifiers on Elements**: Modifiers should describe the state of the *block* or *element* they are attached to, not another block's element.
    ```html
    <!-- BAD -->
    <div class="{entity}-card">
        <h2 class="{entity}-card__title {entity}-card--featured">{Entity} Title</h2>
    </div>

    <!-- GOOD -->
    <div class="{entity}-card {entity}-card--featured">
        <h2 class="{entity}-card__title">{Entity} Title</h2>
    </div>
    ```
*   **Semantic Overload**: A block should be a standalone entity. Don't make an element also a block.
    ```html
    <!-- BAD -->
    <div class="{entity}-card">
        <div class="title-block">{Entity} Title</div>
    </div>

    <!-- GOOD -->
    <div class="{entity}-card">
        <h2 class="{entity}-card__title">{Entity} Title</h2>
    </div>
    ```

---

## 5. Integrating BEM with Global Styles

While scoped CSS is preferred, some BEM blocks might need global definitions or overrides that affect multiple components. These should be placed in `wwwroot/css/site.css` or `wwwroot/css/app.css`.

**`wwwroot/css/site.css`**:
```css
/* Global BEM Block example */
.layout-header {
    background-color: var(--mud-palette-background-grey);
    padding: 16px;
}

.layout-header__logo {
    font-size: 1.5rem;
    font-weight: bold;
}
```

---

**Related Resources**:
- [theming.md](theming.md) - How CSS variables and MudBlazor theme work together.
- [mudblazor-usage.md](mudblazor-usage.md) - For how to apply these styles to specific MudBlazor components.
