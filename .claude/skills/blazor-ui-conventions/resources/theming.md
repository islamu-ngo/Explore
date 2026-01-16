# Theming - Consistent UI Styling

This document outlines the theming strategy for the ISLAMU Event Blazor application, focusing on customizing MudBlazor, managing dark/light modes, and ensuring a consistent visual identity.

---

## 1. MudBlazor Theming Basics

MudBlazor uses a theming system that allows customization of colors, typography, shadows, and more. The primary entry point for customization is the `MudThemeProvider` component, typically placed in `MainLayout.razor`.

### `MainLayout.razor` Theme Setup

```razor
@inherits LayoutComponentBase

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_currentTheme" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="0">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start" OnClick="@((e) => DrawerToggle())" />
        <MudSpacer />
        <MudSwitch @bind-Checked="@_isDarkMode" Color="Color.Primary" Label="Dark Mode" />
        <MudIconButton Icon="@Icons.Custom.Brands.GitHub" Color="Color.Inherit" Link="https://github.com/MudBlazor/MudBlazor" Target="_blank" />
    </MudAppBar>
    <MudDrawer @bind-Open="@_drawerOpen" Elevation="1">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6">ISLAMU Event</MudText>
        </MudDrawerHeader>
        <NavMenu />
    </MudDrawer>
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.Large" Class="my-4 pt-4">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    [CascadingParameter(Name = "InitialTheme")] // Consumed from App.razor
    public bool InitialIsDarkTheme { get; set; }

    private bool _drawerOpen = true;
    private bool _isDarkMode; // Bound to MudThemeProvider and MudSwitch
    private MudTheme _currentTheme = new MudTheme(); // Default theme instance

    protected override void OnInitialized()
    {
        _isDarkMode = InitialIsDarkTheme; // Initialize from cascaded value
        _currentTheme = ISLAMUTheme.CreateTheme(); // Load custom theme
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }
}
```

### Custom `MudTheme` Definition

Define your custom theme settings in a static class or a dedicated file (e.g., `Shared/ISLAMUTheme.cs`).

**`Shared/ISLAMUTheme.cs`**:
```csharp
using MudBlazor;

namespace Explore.Blazor.Shared;

public static class ISLAMUTheme
{
    public static MudTheme CreateTheme()
    {
        var theme = new MudTheme()
        {
            Palette = new Palette()
            {
                Primary = "#34685D", // Dark teal
                Secondary = "#E0BBE4", // Light purple
                Tertiary = "#957DAD", // Medium purple
                Info = "#2196F3",
                Success = "#4CAF50",
                Warning = "#FFC107",
                Error = "#F44336",
                Dark = "#27272f",
                TextPrimary = "#34685D", // Primary text color
                TextSecondary = "rgba(0,0,0, 0.54)",
                AppbarBackground = "#34685D", // App bar color
                AppbarText = Colors.Shades.White,
                Background = Colors.Grey.Lighten5,
                DrawerBackground = "#34685D", // Drawer background color
                DrawerText = Colors.Shades.White,
                Surface = Colors.Shades.White,
                // ... more color definitions
            },
            PaletteDark = new Palette()
            {
                Primary = "#58A293", // Lighter teal for dark mode
                Secondary = "#FFD700", // Gold for accent
                Tertiary = "#BB86FC",
                Info = "#90CAF9",
                Success = "#A5D6A7",
                Warning = "#FFEB3B",
                Error = "#EF9A9A",
                Dark = "#27272f",
                TextPrimary = Colors.Shades.White,
                TextSecondary = "rgba(255,255,255, 0.70)",
                AppbarBackground = "#27272f", // Dark app bar
                AppbarText = Colors.Shades.White,
                Background = "#303030", // Dark background
                DrawerBackground = "#212121",
                DrawerText = "rgba(255,255,255, 0.70)",
                Surface = "#424242",
                // ... more dark mode color definitions
            },
            Typography = new Typography()
            {
                Default = new Default() { FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" } },
                H1 = new H1() { FontSize = "3rem", FontWeight = 300, LineHeight = 1.167 },
                // ... other typography settings
            },
            Shadows = new Shadow()
            {
                // Custom shadows if needed
            },
            // ... other theme properties
        };
        return theme;
    }
}
```

---

## 2. Dark/Light Mode Switching

The application supports dynamic dark/light mode switching, with the preference persisted via cookies.

### `App.razor` Theme Initialization (Server-side)

The `App.razor` component, running on the server initially, reads the theme preference from a cookie and cascades it down to `MainLayout.razor`.

```razor
// File: Explore.Blazor/App.razor
@inject IHttpContextAccessor HttpContextAccessor // Provides access to HttpContext

@code {
    private bool _isDarkTheme;

    protected override void OnInitialized()
    {
        // Read the 'theme' cookie to determine initial dark mode state
        var themeCookie = HttpContextAccessor.HttpContext?.Request.Cookies["theme"];
        _isDarkTheme = themeCookie == "dark";
    }
}

<CascadingValue Value="_isDarkTheme" Name="InitialTheme"> @* Cascades the initial theme state *@
    <Routes @rendermode="InteractiveAuto" /> @* Renders root routes *@
</CascadingValue>
```

### `MainLayout.razor` Theme Consumption and Persistence

`MainLayout.razor` consumes the cascaded theme value and includes a `MudSwitch` to allow users to toggle dark/light mode. The choice is then saved to a cookie.

```razor
// File: Explore.Blazor/Shared/MainLayout.razor
@inject IJSRuntime JS

@code {
    [CascadingParameter(Name = "InitialTheme")]
    public bool InitialIsDarkTheme { get; set; } // Consumes the cascaded value

    private bool _isDarkMode; // Bound to MudThemeProvider and MudSwitch
    private MudTheme _currentTheme = new MudTheme();

    protected override void OnInitialized()
    {
        _isDarkMode = InitialIsDarkTheme;
        _currentTheme = ISLAMUTheme.CreateTheme(); // Load custom theme
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Set up a watcher or initial check from local storage if needed in WASM
        }
    }

    private async Task OnDarkModeChanged(bool value)
    {
        // Save the theme preference to a cookie via JavaScript interop
        await JS.InvokeVoidAsync("setCookie", "theme", value ? "dark" : "light", 365);
        _isDarkMode = value; // Update local state, MudThemeProvider will react
        StateHasChanged(); // Force re-render if necessary
    }
}

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_currentTheme" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar>
        <MudSpacer />
        <MudSwitch @bind-Checked="_isDarkMode"
                   Color="Color.Primary"
                   Label="Dark Mode"
                   ValueChanged="OnDarkModeChanged" /> @* Event for persistence *@
    </MudAppBar>
    @* ... rest of layout ... *@
</MudLayout>
```

### JavaScript for Cookie Management

A simple JavaScript function to set/get cookies.

**`wwwroot/js/site.js`**:
```javascript
window.setCookie = (name, value, days) => {
    let expires = "";
    if (days) {
        let date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/; SameSite=Lax";
};

window.getCookie = (name) => {
    let nameEQ = name + "=";
    let ca = document.cookie.split(';');
    for(let i=0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) === ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
};
```

---

## 3. Custom CSS Overrides and Variables

For fine-grained control or overrides not possible through the `MudTheme` object, use custom CSS.

### `wwwroot/css/site.css`

This file is for global styles and overrides.

```css
/* Custom properties for consistency, e.g., for BEM blocks */
:root {
    --islamu-primary-color: #34685D;
    --islamu-secondary-color: #E0BBE4;
    --islamu-font-family: 'Roboto', sans-serif;
}

/* Override MudBlazor default typography */
body {
    font-family: var(--islamu-font-family);
}

h1.mud-typography-h1 {
    color: var(--islamu-primary-color);
}

/* Custom styles for components */
.event-card {
    border: 1px solid var(--mud-palette-lines-default);
    border-radius: var(--mud-default-border-radius);
    box-shadow: var(--mud-shadow-1);
}

.event-card__title {
    color: var(--islamu-primary-color);
}
```

### `wwwroot/css/variables.css` (Optional, for more granular control)

This file can define CSS variables for colors, spacing, etc., making them easily reusable across the application.

```css
/* Global CSS variables */
:root {
    /* Colors */
    --islamu-color-teal: #34685D;
    --islamu-color-purple-light: #E0BBE4;
    --islamu-color-gold: #FFD700;

    /* Spacing */
    --islamu-spacing-unit: 8px;
    --islamu-spacing-xs: var(--islamu-spacing-unit);
    --islamu-spacing-sm: calc(var(--islamu-spacing-unit) * 2);
    /* ... */
}

/* Use in other CSS files */
.my-component {
    padding: var(--islamu-spacing-sm);
    background-color: var(--islamu-color-teal);
}
```

---

## 4. Best Practices for Theming

*   **Centralize `MudTheme`**: Define your primary light and dark themes in a dedicated static class (e.g., `ISLAMUTheme.cs`).
*   **Use CSS Variables**: Leverage MudBlazor's CSS variables (e.g., `var(--mud-palette-primary)`) and define your own (`--islamu-primary-color`) for consistent styling.
*   **BEM for Custom CSS**: Combine theming with BEM methodology for maintainable custom component styles.
*   **Minimal Global Overrides**: Keep `site.css` lean, focusing on broad overrides and global utilities.
*   **Test Dark/Light Mode**: Always test your application thoroughly in both dark and light modes to ensure readability and visual harmony.
*   **Accessible Colors**: Ensure sufficient contrast between text and background colors, especially for accessibility.

---

**Related Resources**:
- [mudblazor-usage.md](mudblazor-usage.md) - For how to apply styling to specific MudBlazor components.
- [bem-methodology.md](bem-methodology.md) - Guidelines for structured CSS naming.
- [state-management.md](state-management.md) - How theme state is managed and cascaded.
