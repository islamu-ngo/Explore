using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class MainLayout : LayoutComponentBase
{
    private bool _isDarkMode = false;
    private bool _isInitialized = false;
    private MudTheme? _theme;

    [Inject]
    protected IUserService UserService { get; set; } = null!;

    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    protected ILogger<MainLayout> Logger { get; set; } = null!;

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

        // Use the cascaded initial theme from cookie if available
        if (InitialTheme.HasValue)
        {
            _isDarkMode = InitialTheme.Value;
        }

        _theme = new()
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties()
        };
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

            // Read theme from localStorage (client-side source of truth)
            try
            {
                var storedTheme = await JSRuntime.InvokeAsync<string>("ExploreTheme.getStoredTheme");
                if (!string.IsNullOrEmpty(storedTheme))
                {
                    var isDarkStorage = storedTheme == "dark";
                    if (_isDarkMode != isDarkStorage)
                    {
                        _isDarkMode = isDarkStorage;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error reading theme from localStorage");
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
            await JSRuntime.InvokeVoidAsync("ExploreTheme.setStoredTheme", themeValue);
            await JSRuntime.InvokeVoidAsync("ExploreTheme.setThemeCookie", themeValue);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error saving theme");
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
}
