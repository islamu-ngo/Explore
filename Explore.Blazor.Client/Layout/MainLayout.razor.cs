// ABOUTME: Main layout code-behind handling theme initialization and user sync.
// ABOUTME: Uses MudBlazor built-in theme switching with cookie persistence for SSR.

using System.Net.Http;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    private const int NavbarHeightPx = 64;
    private const int AnnouncementBarHeightPx = 48;

    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private bool _announcementVisible = true;
    private MudTheme? _theme;
    private MudThemeProvider _mudThemeProvider = null!;
    private bool _hideChrome;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    protected ILogger<MainLayout> Logger { get; set; } = null!;

    [Inject]
    protected HttpClient HttpClient { get; set; } = null!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    protected SidebarState SidebarState { get; set; } = null!;

    [CascadingParameter(Name = "InitialTheme")]
    public bool? InitialTheme { get; set; }

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();

        // Use the cascaded initial theme from cookie if available (for SSR)
        if (InitialTheme.HasValue)
        {
            _isDarkMode = InitialTheme.Value;
        }

        _theme = BuildTheme();

        UpdateChromeVisibility();
        NavigationManager.LocationChanged += OnLocationChanged;
        SidebarState.OnChange += StateHasChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Sync user in background after first render to improve perceived performance
            try
            {
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    await UserService.SyncUserAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error syncing user");
            }

            // If no cookie-based theme was provided, detect system preference via MudBlazor
            if (!InitialTheme.HasValue)
            {
                try
                {
                    // Default to Light mode to match server assumption
                    _isDarkMode = false;

                    // Then verify system preference
                    var systemDark = await _mudThemeProvider.GetSystemDarkModeAsync();
                    if (systemDark)
                    {
                        _isDarkMode = true;
                        await InvokeAsync(StateHasChanged);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error initializing theme");
                }
            }

            _isInitialized = true;
        }
    }

    private async Task DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
        var themeValue = _isDarkMode ? "dark" : "light";

        try
        {
            // Persist to cookie via BFF endpoint so SSR reads the preference on next page load
            await HttpClient.PostAsync($"/bff/theme?theme={Uri.EscapeDataString(themeValue)}", null);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error saving theme preference");
        }
    }

    private readonly PaletteLight _lightPalette = new()
    {
        Primary = "#00D16F",
        Secondary = "#1A1A1A",
        Black = "#0A0A0A",
        AppbarText = "#1A1A1A",
        AppbarBackground = "rgba(245,245,247,0.8)",
        Background = "#F5F5F7",
        Surface = "#FFFFFF",
        DrawerBackground = "#F5F5F7",
        DrawerText = "#1A1A1A",
        DrawerIcon = "#1A1A1A",
        GrayLight = "#E8E8E8",
        GrayLighter = "#F9F9F9",
        TextPrimary = "#1A1A1A",
        TextSecondary = "#666666",
        Info = "#2196F3",
        Success = "#00D16F",
        Warning = "#FFC107",
        Error = "#FF3D00",
        LinesDefault = "#E0E0E0",
        TableLines = "#E0E0E0",
        Divider = "#E0E0E0",
        OverlayLight = "rgba(255,255,255,0.8)"
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#00D16F",
        Secondary = "#FFFFFF",
        Surface = "#1E1E2D",
        Background = "#0A0A0A",
        BackgroundGray = "#111111",
        AppbarText = "#FFFFFF",
        AppbarBackground = "rgba(10,10,10,0.8)",
        DrawerBackground = "#0A0A0A",
        ActionDefault = "#B0B0B0",
        ActionDisabled = "#404040",
        ActionDisabledBackground = "#202020",
        TextPrimary = "#FFFFFF",
        TextSecondary = "#B0B0B0",
        TextDisabled = "#606060",
        DrawerIcon = "#FFFFFF",
        DrawerText = "#FFFFFF",
        GrayLight = "#2A2833",
        GrayLighter = "#1E1E2D",
        Info = "#2196F3",
        Success = "#00D16F",
        Warning = "#FFC107",
        Error = "#FF3D00",
        LinesDefault = "#333333",
        TableLines = "#333333",
        Divider = "#252525",
        OverlayLight = "rgba(0,0,0,0.8)"
    };

    private static readonly Typography _typography = new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Inter", "system-ui", "-apple-system", "sans-serif"],
            FontSize = ".9375rem",
            FontWeight = "400",
            LineHeight = "1.5",
            LetterSpacing = "-.011em"
        },
        H1 = new H1Typography { FontSize = "2.5rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-.022em" },
        H2 = new H2Typography { FontSize = "2rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-.017em" },
        H3 = new H3Typography { FontSize = "1.75rem", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-.014em" },
        H4 = new H4Typography { FontSize = "1.5rem", FontWeight = "600", LineHeight = "1.4" },
        H5 = new H5Typography { FontSize = "1.25rem", FontWeight = "600", LineHeight = "1.5" },
        H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.6" },
        Body1 = new Body1Typography { FontSize = ".9375rem", LineHeight = "1.6", LetterSpacing = "-.011em" },
        Body2 = new Body2Typography { FontSize = ".875rem", LineHeight = "1.5" },
        Button = new ButtonTypography { FontSize = ".875rem", FontWeight = "500", TextTransform = "none", LetterSpacing = "-.011em" },
        Caption = new CaptionTypography { FontSize = ".8125rem", LineHeight = "1.5" },
        Overline = new OverlineTypography { FontSize = ".75rem", FontWeight = "500", TextTransform = "uppercase", LetterSpacing = ".08em" }
    };

    private MudTheme BuildTheme() => new()
    {
        PaletteLight = _lightPalette,
        PaletteDark = _darkPalette,
        Typography = _typography,
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            AppbarHeight = GetAppbarHeight()
        }
    };

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        UpdateChromeVisibility();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateChromeVisibility()
    {
        var relative = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        var path = relative.Split('?', '#')[0];

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        _hideChrome = path.Equals("/setup", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/onboarding/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/startup", StringComparison.OrdinalIgnoreCase);

        SidebarState.SetHasSidebar(!_hideChrome);
    }

    private void OnDrawerOpenChanged(bool open) => SidebarState.SetOpen(open);

    /// <summary>
    /// Called when the announcement bar is shown or dismissed.
    /// Recreates the theme with an updated AppbarHeight so
    /// --mud-appbar-height on :root reflects the true header height.
    /// MudBlazor's ClipMode.Always drawer CSS and sticky components
    /// automatically use the updated value.
    /// </summary>
    private void OnAnnouncementVisibilityChanged(bool isVisible)
    {
        _announcementVisible = isVisible;
        if (_theme is not null)
        {
            _theme = BuildTheme();
            StateHasChanged();
        }
    }

    private string GetAppbarHeight()
    {
        var height = NavbarHeightPx + (_announcementVisible ? AnnouncementBarHeightPx : 0);
        return $"{height}px";
    }

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
        SidebarState.OnChange -= StateHasChanged;
    }
}
