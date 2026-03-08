// ABOUTME: Code-behind for SetupLayout providing theme toggle with same palettes as MainLayout.
// ABOUTME: Persists theme preference via BFF cookie endpoint for SSR consistency.

using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Explore.Blazor.Client.Layout;

public partial class SetupLayout : LayoutComponentBase
{
    private bool _isDarkMode;
    private MudThemeProvider _mudThemeProvider = null!;
    private MudTheme _theme = null!;

    [Inject]
    private HttpClient HttpClient { get; set; } = null!;

    [CascadingParameter(Name = "InitialTheme")]
    public bool? InitialTheme { get; set; }

    protected override void OnInitialized()
    {
        if (InitialTheme.HasValue)
        {
            _isDarkMode = InitialTheme.Value;
        }

        _theme = BuildTheme();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !InitialTheme.HasValue)
        {
            try
            {
                _isDarkMode = await _mudThemeProvider.GetSystemDarkModeAsync();
                StateHasChanged();
            }
            catch
            {
                // Ignore — default to light
            }
        }
    }

    private async Task ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        var themeValue = _isDarkMode ? "dark" : "light";

        try
        {
            await HttpClient.PostAsync($"/bff/theme?theme={Uri.EscapeDataString(themeValue)}", null);
        }
        catch
        {
            // Best-effort persistence
        }
    }

    private static readonly PaletteLight _lightPalette = new()
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

    private static readonly PaletteDark _darkPalette = new()
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

    private static MudTheme BuildTheme() => new()
    {
        PaletteLight = _lightPalette,
        PaletteDark = _darkPalette,
        Typography = _typography,
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };
}
