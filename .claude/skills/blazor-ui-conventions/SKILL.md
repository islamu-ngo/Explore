---
name: blazor-ui-conventions
description: Comprehensive UI conventions for ISLAMU Event Blazor applications. Covers MudBlazor usage, BEM methodology, theming, component structure, state management, and render modes.
type: ui
enforcement: suggest
priority: high
---

# Blazor UI Conventions & MudBlazor Guidelines

## 🎯 Purpose

This skill provides comprehensive guidelines and best practices for developing UI components in ISLAMU Event Blazor applications. It covers MudBlazor usage, BEM methodology for CSS, theming, component structure, lifecycle, state management, and render modes to ensure consistency, maintainability, and optimal user experience.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "blazor", "component", "razor", "mudblazor", "ui", "page", "dialog", "render", "parameter", "css", "theme", "state", "lifecycle"
- File patterns: `**/*.razor`, `**/*.razor.cs`, `**/*.Client/**/*.cs`, `**/*.css`, `**/*.scss`
- Content patterns: `@page`, `@inject`, `<Mud`, `Parameter`, `EventCallback`, `::deep`, `::part`, `BEM` class names.

## 🏗️ ISLAMU Event Blazor Architecture

```mermaid
graph TD
    subgraph Blazor Hybrid App
        BFF[Explore.Blazor (Server-side BFF)]
        WASM[Explore.Blazor.Client (WebAssembly)]
        Router[Router / InteractiveAuto]
    end

    subgraph UI Layer
        Components[UI Components (.razor)]
        MudBlazor[MudBlazor Library]
        CSS[BEM-structured CSS]
    end

    Router -- Renders on --> BFF
    Router -- Interactivity via --> WASM
    Components -- Built with --> MudBlazor
    Components -- Styled with --> CSS
    WASM -- Communicates with --> BFF
```

## 📚 Resources

| Resource | Description |
|----------|-------------|
| [mudblazor-usage.md](resources/mudblazor-usage.md) | Detailed usage of common MudBlazor components (Grid, Buttons, Forms, Dialogs, Tables, Snackbar, Cards). |
| [component-design.md](resources/component-design.md) | Blazor component structure, lifecycle, parameters, `EventCallback`, and best practices for parent-child communication. |
| [state-management.md](resources/state-management.md) | Comprehensive guide to Blazor state management techniques (component state, parameters, cascading values, scoped/singleton services). |
| [render-modes.md](resources/render-modes.md) | Explanation of Blazor render modes (InteractiveAuto, Server, WebAssembly, Static SSR) and their appropriate use cases. |
| [bem-methodology.md](resources/bem-methodology.md) | Guidelines for BEM (Block, Element, Modifier) CSS naming convention for maintainable stylesheets. |
| [theming.md](resources/theming.md) | Customizing MudBlazor themes, dark/light mode switching, and consistent styling. |
| [common-patterns.md](resources/common-patterns.md) | Real-world Blazor implementation patterns for forms, dialogs, tables, navigation, loading states, error handling, search/filter, and infinite scroll. |

## ⚡ Quick Reference

### 1. BEM Methodology

**Rule**: All custom CSS should follow the BEM (Block, Element, Modifier) naming convention.

```css
/* Block: .event-card */
.event-card {
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: var(--mud-default-border-radius);
    box-shadow: var(--mud-shadow-1);
}

/* Element: .event-card__title */
.event-card__title {
    font-size: var(--mud-typography-h6-size);
    color: var(--mud-palette-primary);
}

/* Modifier: .event-card--featured */
.event-card--featured {
    border-color: var(--mud-palette-success);
    box-shadow: var(--mud-shadow-2);
}
```
*For more details, see [bem-methodology.md](resources/bem-methodology.md).*

### 2. MudBlazor Usage

**Rule**: Prefer MudBlazor components over custom HTML for consistent UI and functionality.

```razor
@* Grid Layout *@
<MudGrid Spacing="3">
    <MudItem xs="12" sm="6" md="4" lg="3">
        <MudCard>
            <MudCardMedia Image="@evt.CoverImageUrl" Height="200" />
            <MudCardContent>
                <MudText Typo="Typo.h6">@evt.Title</MudText>
            </MudCardContent>
        </MudCard>
    </MudItem>
</MudGrid>

@* Buttons *@
<MudButton Variant="Variant.Filled" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add">
    Create Event
</MudButton>

@* Forms *@
<MudTextField @bind-Value="title" Label="Event Title" Required="true" />
<MudSelect @bind-Value="selectedAge" Label="Audience Age">
    <MudSelectItem Value="1">Adults</MudSelectItem>
</MudSelect>
```
*For more details, see [mudblazor-usage.md](resources/mudblazor-usage.md).*

### 3. Theming

**Rule**: Global styles and theme overrides are managed in `wwwroot/css/StyleGlobal.css` and `wwwroot/css/variables.css`. Dark/Light mode switching is handled via a `CascadingValue`.

```csharp
// App.razor logic for cascading theme
@inject IHttpContextAccessor HttpContextAccessor

@code {
    var theme = HttpContextAccessor.HttpContext?.Request.Cookies["theme"];
    var isDark = theme == "dark";
}

<CascadingValue Value="isDark" Name="InitialTheme">
    <Routes @rendermode="InteractiveAuto" />
</CascadingValue>

// MainLayout.razor consuming the theme
@code {
    [CascadingParameter(Name = "InitialTheme")]
    public bool IsDarkMode { get; set; }

    private MudTheme _theme = new();
    private bool _isDarkMode;

    protected override void OnInitialized()
    {
        _isDarkMode = IsDarkMode;
    }
}

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_theme" />
```
*For more details, see [theming.md](resources/theming.md).*

### 4. Component Structure & Lifecycle

**Rule**: Use the code-behind pattern for complex components. Use `OnInitializedAsync` for data fetching, `OnParametersSetAsync` for reacting to parameter changes, and `EventCallback` for child-to-parent communication.

```razor
@* Child Component: EventCard.razor *@
<MudCard>
    <MudCardActions>
        <MudButton OnClick="DeleteClicked">Delete</MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter]
    public EventDto Event { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    private async Task DeleteClicked()
    {
        await OnDelete.InvokeAsync(Event.Id);
    }
}
```
*For more details, see [component-design.md](resources/component-design.md).*

### 5. State Management

**Rule**: Choose the appropriate state management pattern based on the component relationship and data scope.

-   **Component State**: For isolated component data.
-   **Parameters/EventCallback**: For direct parent-child communication.
-   **CascadingValue**: For sharing state deep in the component hierarchy (e.g., `AuthenticationState`, theme).
-   **Scoped Services**: For sharing state across multiple, unrelated components within the same user session.
-   **Singleton Services**: For global, read-only application data.

*For more details, see [state-management.md](resources/state-management.md).*

### 6. Render Modes

**Rule**: Use `InteractiveAuto` as the default render mode for interactive components. Use `InteractiveServer` for server-only features (e.g., `HttpContext` access) and `InteractiveWebAssembly` for offline-first or highly client-side interactive components.

```razor
@page "/events"
@rendermode InteractiveAuto // Default for interactive pages
```
*For more details, see [render-modes.md](resources/render-modes.md).*

### 7. Common Patterns

**Rule**: Implement common UI patterns like forms, dialogs, tables, and loading states consistently across the application.

```razor
@* Confirmation Dialog *@
@inject IDialogService DialogService

<MudButton OnClick="DeleteEvent">Delete</MudButton>

@code {
    private async Task DeleteEvent()
    {
        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Delete");
        var result = await dialog.Result;
        if (!result.Canceled) { /* Perform delete */ }
    }
}
```
*For more details, see [common-patterns.md](resources/common-patterns.md).*

## ✅ Do's

-   ✅ **DO** use `@rendermode="InteractiveAuto"` as the default for interactive pages.
-   ✅ **DO** prefer MudBlazor components over custom HTML for consistency.
-   ✅ **DO** follow the BEM methodology for all custom CSS classes.
-   ✅ **DO** use `[Parameter]` for inputs and `EventCallback<T>` for child-to-parent events.
-   ✅ **DO** load data in `OnInitializedAsync` for initial setup.
-   ✅ **DO** use `IDialogService` for modals and `ISnackbar` for notifications.
-   ✅ **DO** implement `IDisposable` for event clean-up in services and components.
-   ✅ **DO** use scoped services for state management across components within a user session.
-   ✅ **DO** centralize theme customization in `wwwroot/css/site.css` and `wwwroot/css/variables.css`.

## ❌ Don'ts

-   ❌ **DON'T** use raw HTML when a suitable MudBlazor component exists.
-   ❌ **DON'T** modify `[Parameter]` properties directly from within a child component.
-   ❌ **DON'T** access `HttpContext` in components rendered with `InteractiveAuto` or `InteractiveWebAssembly` (use `InteractiveServer` or BFF pattern).
-   ❌ **DON'T** use JavaScript interop for tasks that MudBlazor or Blazor itself can handle.
-   ❌ **DON'T** call `StateHasChanged()` unnecessarily (can lead to performance issues).
-   ❌ **DON'T** use static state in Blazor Server apps (can cause data leakage between users).

---

**Related Documentation**:
- [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md) - Overall system architecture, especially Blazor Hybrid.
- [`blazor-bff-patterns`](../blazor-bff-patterns/SKILL.md) - Details on YARP, token forwarding, and cookie-based auth.
- [`auth-patterns`](../auth-patterns/SKILL.md) - Authentication and authorization patterns in Blazor.