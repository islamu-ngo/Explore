---
name: blazor-css-isolation
description: Blazor CSS isolation patterns with BEM methodology. Covers component.razor.css scoped styling, ::deep selector for child components, and BEM class naming conventions.
type: ui
enforcement: suggest
priority: high
---

# Blazor CSS Isolation with BEM Methodology

> **Project-Agnostic CSS Isolation Patterns for Blazor**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../docs/TEMPLATE_GLOSSARY.md).

## Purpose

Provides enterprise-grade patterns for component-scoped CSS in Blazor applications using CSS isolation (`.razor.css` files) combined with BEM (Block Element Modifier) methodology. Ensures maintainable, collision-free styling with proper encapsulation.

## When This Skill Activates

**Triggered by**:
- Keywords: "css isolation", "scoped css", "razor.css", "::deep", "bem", "block element modifier", "component styling"
- File patterns: `**/*.razor.css`, `**/*.razor`
- Content patterns: `::deep`, BEM class names (`.block__element--modifier`)

## How Blazor CSS Isolation Works

### Build-Time Transformation

Blazor compiles `.razor.css` files by:
1. **Generating unique scope attribute**: Format `b-<10-char-string>` (e.g., `b-3xxtam6d07`)
2. **Appending to selectors**: `h1` becomes `h1[b-3xxtam6d07]`
3. **Applying to DOM elements**: Rendered elements receive the scope attribute
4. **Bundling**: Generated CSS outputs to `{Project}.styles.css`

**Pattern**:
```css
/* Author in Counter.razor.css */
h1 { color: brown; }

/* Compiled output in {Project}.styles.css */
h1[b-3xxtam6d07] { color: brown; }
```

**Key Benefit**: Styles apply ONLY to component's own elements, preventing global collisions.

## Resources

| Resource | Description |
|----------|-------------|
| [bem-with-isolation.md](resources/bem-with-isolation.md) | BEM naming patterns for isolated CSS |
| [deep-selector-patterns.md](resources/deep-selector-patterns.md) | Safe ::deep usage for child components |
| [mudblazor-styling.md](resources/mudblazor-styling.md) | Styling MudBlazor components with isolation |
| [debugging-scoped-css.md](resources/debugging-scoped-css.md) | Browser DevTools techniques for isolated CSS |

## Quick Reference

### 1. File Structure & Naming

**Pattern**: Place `ComponentName.razor.css` next to `ComponentName.razor` (same folder, same name).

```
Components/
├── Card.razor
├── Card.razor.css       ✅ Automatically scoped to Card component
├── EventList.razor
└── EventList.razor.css  ✅ Automatically scoped to EventList component
```

**Rules**:
- File names are case-insensitive but MUST match `.razor` filename
- Missing the `.razor.css` file means no scoped styles for that component
- Scoped CSS bundle (`{Project}.styles.css`) must be referenced in `App.razor` or `index.html`

---

### 2. BEM with CSS Isolation

**Pattern**: Use BEM class names in component markup; compiler scopes them automatically.

#### Component Markup (Card.razor)

```razor
<article class="card card--featured">
    <header class="card__header">
        <h2 class="card__title">@Title</h2>
        <span class="card__badge card__badge--new">New</span>
    </header>
    <div class="card__body">
        <p class="card__text">@Description</p>
    </div>
    <footer class="card__footer">
        <MudButton Class="card__action">Read More</MudButton>
    </footer>
</article>

@code {
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string Description { get; set; } = string.Empty;
}
```

#### Scoped CSS (Card.razor.css)

```css
/* Block */
.card {
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: 8px;
    background: var(--mud-palette-surface);
}

/* Block modifier */
.card--featured {
    border-color: var(--mud-palette-primary);
    box-shadow: var(--mud-shadow-2);
}

/* Elements */
.card__header {
    padding: 16px;
    border-bottom: 1px solid var(--mud-palette-lines-default);
}

.card__title {
    margin: 0;
    font-size: 1.25rem;
    font-weight: 600;
    color: var(--mud-palette-text-primary);
}

.card__badge {
    display: inline-block;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 0.75rem;
}

/* Element modifier */
.card__badge--new {
    background: var(--mud-palette-success);
    color: var(--mud-palette-success-text);
}

.card__body {
    padding: 16px;
}

.card__text {
    margin: 0;
    line-height: 1.5;
}

.card__footer {
    padding: 12px 16px;
    border-top: 1px solid var(--mud-palette-lines-default);
}

.card__action {
    text-transform: uppercase;
}
```

**Compiled Output** (automatic):
```css
/* All selectors receive scope attribute */
.card[b-3xxtam6d07] { ... }
.card--featured[b-3xxtam6d07] { ... }
.card__header[b-3xxtam6d07] { ... }
.card__title[b-3xxtam6d07] { ... }
/* ... and so on */
```

**Why BEM + Isolation?**
- **BEM**: Explicit, readable class names (`.block__element--modifier`)
- **Isolation**: Automatic scoping prevents `.card` collision with other components
- **Result**: Predictable, maintainable styles without naming conflicts

---

### 3. Styling Child Components (Preferred Patterns)

#### Pattern A: Child's Own CSS (Recommended)

**Best Practice**: Each component styles itself in its own `.razor.css`.

```razor
<!-- Parent.razor -->
<div class="parent">
    <ChildCard Title="Example" />
</div>

<!-- ChildCard.razor -->
<div class="child-card">
    <h3>@Title</h3>
</div>
```

```css
/* Parent.razor.css */
.parent {
    display: grid;
    gap: 16px;
}

/* ChildCard.razor.css (child styles itself) */
.child-card {
    padding: 12px;
    background: #f5f5f5;
}
```

**Why**: Separation of concerns; child owns its styles.

---

#### Pattern B: Wrapper Container (Safe Descendant Styling)

**Pattern**: Wrap child in HTML element to apply scope attribute, enabling descendant selectors.

```razor
<!-- Parent.razor -->
<div class="parent">
    <div class="parent__child-wrapper">
        <ChildComponent />
    </div>
</div>
```

```css
/* Parent.razor.css */
.parent__child-wrapper {
    padding: 8px;
    border: 1px solid #ddd;
}

/* Wrapper receives scope attribute; descendant selectors work */
.parent__child-wrapper > div {
    margin-bottom: 8px;
}
```

**Why**: Wrapper gets scope attribute; you can style children without `::deep`.

---

### 4. The ::deep Selector (Use Sparingly)

**Purpose**: Penetrate child component encapsulation to style nested elements.

**Transformation**:
```css
/* Authored */
.parent ::deep .child-element {
    color: red;
}

/* Compiled */
.parent[b-3xxtam6d07] .child-element {
    color: red;
}
```

**Pattern**: Scope attribute moves to ancestor, selector reaches descendants.

#### Safe ::deep Usage

```razor
<!-- Host.razor -->
<div class="host">
    <ThirdPartyComponent />
</div>
```

```css
/* Host.razor.css */
.host ::deep .tp-control {
    background: var(--mud-palette-surface);
}

.host ::deep .tp-control__header {
    font-weight: 600;
}
```

**When to Use ::deep**:
- ✅ Styling third-party components with no exposed styling API
- ✅ Overriding library component internals when necessary
- ✅ Reaching nested DOM that cannot be wrapped

**When NOT to Use ::deep**:
- ❌ Styling your own child components (use child's `.razor.css`)
- ❌ First resort (prefer wrapper pattern or component parameters)
- ❌ Global resets (use global CSS file instead)

**Tradeoffs**:
- ⚠️ **Fragile**: Coupled to child's internal markup/class names
- ⚠️ **Upgrade Risk**: Library updates may break selectors
- ⚠️ **Specificity**: Adds complexity to CSS cascade

---

### 5. Styling MudBlazor Components

**Pattern**: Combine MudBlazor's `Class` parameter with BEM in isolated CSS.

```razor
<!-- EventCard.razor -->
<MudCard Class="event-card event-card--upcoming">
    <MudCardHeader Class="event-card__header">
        <MudText Typo="Typo.h6" Class="event-card__title">@Title</MudText>
    </MudCardHeader>
    <MudCardContent Class="event-card__body">
        <MudText Class="event-card__description">@Description</MudText>
    </MudCardContent>
    <MudCardActions Class="event-card__footer">
        <MudButton Variant="Variant.Text" Color="Color.Primary" Class="event-card__action">
            Register
        </MudButton>
    </MudCardActions>
</MudCard>
```

```css
/* EventCard.razor.css */
.event-card {
    border-radius: 12px;
}

.event-card--upcoming {
    border-left: 4px solid var(--mud-palette-success);
}

.event-card__header {
    background: var(--mud-palette-surface-variant);
}

.event-card__title {
    color: var(--mud-palette-primary);
}

.event-card__description {
    line-height: 1.6;
}

.event-card__footer {
    justify-content: flex-end;
}

.event-card__action {
    font-weight: 500;
}

/* Override MudBlazor internals with ::deep (use sparingly) */
.event-card ::deep .mud-card-header-content {
    padding: 20px;
}
```

**Best Practice**:
1. Use MudBlazor's `Class` parameter for BEM classes
2. Style those classes in `.razor.css`
3. Use `::deep` ONLY for unreachable MudBlazor internals
4. Prefer MudBlazor theme variables over hardcoded colors

---

### 6. Debugging Scoped CSS

#### Browser DevTools Techniques

**Step 1: Inspect Element Scope Attribute**

Look for generated attribute in rendered HTML:
```html
<h1 b-3xxtam6d07>Counter</h1>
```

**Step 2: Find Matching Selector in Styles**

In DevTools Styles panel, look for:
```css
h1[b-3xxtam6d07] {
    color: brown;
}
```

**Step 3: Verify Bundle Reference**

Check `App.razor` or `index.html` includes:
```html
<link href="{Project}.styles.css" rel="stylesheet" />
```

**Common Issues**:

| Symptom | Cause | Fix |
|---------|-------|-----|
| Styles not applying | Missing bundle reference | Add `<link>` to `{Project}.styles.css` |
| Wrong component styled | Scope attribute mismatch | Rebuild project; check file names match |
| Child not styled | Using parent CSS | Add `.razor.css` to child OR use wrapper/::deep |
| ::deep not working | Child renders to body | Use component's `Target` parameter if available |

---

## BEM Naming Guidelines

### Pattern Structure

```
.block
.block__element
.block--modifier
.block__element--modifier
```

### Practical Examples

```css
/* Component: EventCard */
.event-card { }                      /* Block */
.event-card__header { }              /* Element */
.event-card__title { }               /* Element */
.event-card--featured { }            /* Block modifier */
.event-card__badge--new { }          /* Element modifier */

/* Component: SearchBar */
.search-bar { }
.search-bar__input { }
.search-bar__button { }
.search-bar--expanded { }
.search-bar__input--focused { }
```

### Naming Best Practices

- **Keep names concise**: `card__title` not `card__title-text-content`
- **Use semantic names**: `card__action` not `card__button1`
- **Avoid abbreviations**: `card__description` not `card__desc`
- **Single responsibility**: `card__header` not `card__header-with-icon`

---

## Key Principles

1. **Colocation**: Place `.razor.css` next to `.razor` file
2. **Component Ownership**: Each component styles itself in its own `.razor.css`
3. **BEM for Clarity**: Use BEM even though isolation prevents collisions
4. **Wrapper Over ::deep**: Prefer wrapping children in containers
5. **::deep as Last Resort**: Only penetrate encapsulation when necessary
6. **MudBlazor Integration**: Use `Class` parameter + isolated CSS
7. **Scope Verification**: Check `b-<string>` attribute in DevTools

---

## Do's

- ✅ **DO** name files `Component.razor.css` (matches `.razor` filename)
- ✅ **DO** use BEM class names for explicit styling hooks
- ✅ **DO** style child components in their own `.razor.css`
- ✅ **DO** wrap children in containers for descendant styling
- ✅ **DO** verify `{Project}.styles.css` is referenced in app
- ✅ **DO** use MudBlazor theme variables for colors
- ✅ **DO** test scope attributes in browser DevTools

## Don'ts

- ❌ **DON'T** use `::deep` as first approach (prefer child's own CSS)
- ❌ **DON'T** style children from parent without wrapping
- ❌ **DON'T** forget to reference `{Project}.styles.css` bundle
- ❌ **DON'T** use global CSS for component-specific styles
- ❌ **DON'T** couple parent CSS to child's internal structure
- ❌ **DON'T** use inline styles (defeats isolation benefits)

---

**Related Documentation**:
- [`docs/BLAZOR.md`](../../../docs/BLAZOR.md) - Blazor architecture and patterns
- [`blazor-ui-conventions`](../blazor-ui-conventions/SKILL.md) - MudBlazor and component design
- [`docs/QUICK_REFERENCE.md`](../../../docs/QUICK_REFERENCE.md) - Critical architectural rules

**Official References**:
- [ASP.NET Core Blazor CSS Isolation](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation)
- [BEM Methodology](http://getbem.com/introduction/)
- [MudBlazor Documentation](https://mudblazor.com/)
