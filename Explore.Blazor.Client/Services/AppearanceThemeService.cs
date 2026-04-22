// ABOUTME: Central runtime service for composing MudBlazor themes and persisting the current appearance mode.
// ABOUTME: Supports dynamic UiTheme palettes fetched via the BFF with a built-in fallback for anonymous or failure paths.

using System.Net.Http.Json;
using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Models.Appearance;
using MudBlazor;

namespace Explore.Blazor.Client.Services;

public interface IAppearanceThemeService
{
    MudTheme CreateTheme(string appbarHeight, AvailableThemeModel? activeTheme = null);
    Task<bool> ResolveInitialDarkModeAsync(bool? serverHint, MudThemeProvider themeProvider);
    Task PersistThemeModeAsync(bool isDarkMode, CancellationToken cancellationToken = default);
    Task<string> ResolveInitialDirectionAsync();
    Task PersistDirectionAsync(string direction, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AvailableThemeModel>> GetAvailableThemesAsync(CancellationToken cancellationToken = default);
    Task<AvailableThemeModel?> ResolveActiveThemeAsync(CancellationToken cancellationToken = default);
}

public sealed class AppearanceThemeService : IAppearanceThemeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppearanceThemeService> _logger;

    private static readonly PaletteLight BuiltInLight = new()
    {
        Primary = "#2563EB",
        Secondary = "#1E293B",
        Black = "#0F172A",
        AppbarText = "#1E293B",
        AppbarBackground = "rgba(248,250,252,0.85)",
        Background = "#F8FAFC",
        Surface = "#FFFFFF",
        DrawerBackground = "#F1F5F9",
        DrawerText = "#1E293B",
        DrawerIcon = "#475569",
        GrayLight = "#E2E8F0",
        GrayLighter = "#F8FAFC",
        TextPrimary = "#0F172A",
        TextSecondary = "#64748B",
        Info = "#2563EB",
        Success = "#047857",
        Warning = "#B45309",
        Error = "#DC2626",
        LinesDefault = "#E2E8F0",
        TableLines = "#E2E8F0",
        Divider = "#E2E8F0",
        OverlayLight = "rgba(248,250,252,0.8)"
    };

    private static readonly PaletteDark BuiltInDark = new()
    {
        Primary = "#60A5FA",
        Secondary = "#F1F5F9",
        Surface = "#1E293B",
        Background = "#0F172A",
        BackgroundGray = "#1E293B",
        AppbarText = "#F1F5F9",
        AppbarBackground = "rgba(15,23,42,0.85)",
        DrawerBackground = "#0F172A",
        ActionDefault = "#94A3B8",
        ActionDisabled = "#334155",
        ActionDisabledBackground = "#1E293B",
        TextPrimary = "#F1F5F9",
        TextSecondary = "#94A3B8",
        TextDisabled = "#475569",
        DrawerIcon = "#CBD5E1",
        DrawerText = "#F1F5F9",
        GrayLight = "#334155",
        GrayLighter = "#1E293B",
        Info = "#60A5FA",
        Success = "#34D399",
        Warning = "#FBBF24",
        Error = "#F87171",
        LinesDefault = "#334155",
        TableLines = "#334155",
        Divider = "#1E293B",
        OverlayLight = "rgba(15,23,42,0.8)"
    };

    private static readonly Typography Typography = new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Inter", "system-ui", "-apple-system", "sans-serif"],
            FontSize = ".9375rem",
            FontWeight = "400",
            LineHeight = "1.5",
            LetterSpacing = "-.011em"
        },
        H1 = new H1Typography { FontSize = "clamp(1.875rem, 1.5rem + 1.04vw, 2.5rem)", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-.022em" },
        H2 = new H2Typography { FontSize = "clamp(1.625rem, 1.375rem + 0.625vw, 2rem)", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-.017em" },
        H3 = new H3Typography { FontSize = "clamp(1.5rem, 1.333rem + 0.42vw, 1.75rem)", FontWeight = "600", LineHeight = "1.3", LetterSpacing = "-.014em" },
        H4 = new H4Typography { FontSize = "clamp(1.25rem, 1.083rem + 0.42vw, 1.5rem)", FontWeight = "600", LineHeight = "1.4" },
        H5 = new H5Typography { FontSize = "clamp(1.125rem, 1.042rem + 0.21vw, 1.25rem)", FontWeight = "600", LineHeight = "1.5" },
        H6 = new H6Typography { FontSize = "1.125rem", FontWeight = "600", LineHeight = "1.6" },
        Body1 = new Body1Typography { FontSize = ".9375rem", LineHeight = "1.6", LetterSpacing = "-.011em" },
        Body2 = new Body2Typography { FontSize = ".875rem", LineHeight = "1.5" },
        Button = new ButtonTypography { FontSize = ".875rem", FontWeight = "500", TextTransform = "none", LetterSpacing = "-.011em" },
        Caption = new CaptionTypography { FontSize = ".8125rem", LineHeight = "1.5" },
        Overline = new OverlineTypography { FontSize = ".75rem", FontWeight = "500", TextTransform = "uppercase", LetterSpacing = ".08em" }
    };

    public AppearanceThemeService(
        HttpClient httpClient,
        ILogger<AppearanceThemeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public MudTheme CreateTheme(string appbarHeight, AvailableThemeModel? activeTheme = null)
    {
        var light = activeTheme?.LightPalette is { } lightDto ? ComposeLight(lightDto) : BuiltInLight;
        var dark = activeTheme?.DarkPalette is { } darkDto ? ComposeDark(darkDto) : BuiltInDark;

        return new MudTheme
        {
            PaletteLight = light,
            PaletteDark = dark,
            Typography = Typography,
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
                AppbarHeight = appbarHeight
            }
        };
    }

    public async Task<bool> ResolveInitialDarkModeAsync(bool? serverHint, MudThemeProvider themeProvider)
    {
        if (serverHint.HasValue)
        {
            return serverHint.Value;
        }

        try
        {
            var preferences = await _httpClient.GetFromJsonAsync<UserAppearancePreferencesDto>("/bff/theme");
            if (preferences?.ThemeMode is "dark")
            {
                return true;
            }

            if (preferences?.ThemeMode is "light")
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load current theme preference from the BFF.");
        }

        try
        {
            return await themeProvider.GetSystemDarkModeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving system theme preference");
            return false;
        }
    }

    public async Task PersistThemeModeAsync(bool isDarkMode, CancellationToken cancellationToken = default)
    {
        var themeMode = isDarkMode ? "dark" : "light";

        try
        {
            using var response = await _httpClient.PostAsync(
                $"/bff/theme?theme={Uri.EscapeDataString(themeMode)}",
                content: null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Theme preference persistence failed with status code {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving theme preference");
        }
    }

    public async Task<string> ResolveInitialDirectionAsync()
    {
        try
        {
            var preferences = await _httpClient.GetFromJsonAsync<UserAppearancePreferencesDto>("/bff/theme");
            if (preferences?.Direction is "ltr" or "rtl")
            {
                return preferences.Direction;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load direction preference from the BFF.");
        }

        return "auto";
    }

    public async Task PersistDirectionAsync(string direction, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.PostAsync(
                $"/bff/direction?dir={Uri.EscapeDataString(direction)}",
                null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Direction preference persistence failed with status code {StatusCode}", (int)response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving direction preference");
        }
    }

    public async Task<IReadOnlyList<AvailableThemeModel>> GetAvailableThemesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var themes = await _httpClient.GetFromJsonAsync<IReadOnlyList<AvailableThemeModel>>("/bff/ui-themes", cancellationToken);
            return themes ?? Array.Empty<AvailableThemeModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading available UI themes from the BFF.");
            return Array.Empty<AvailableThemeModel>();
        }
    }

    public async Task<AvailableThemeModel?> ResolveActiveThemeAsync(CancellationToken cancellationToken = default)
    {
        var themes = await GetAvailableThemesAsync(cancellationToken);
        if (themes.Count == 0)
        {
            return null;
        }

        Guid? preferredThemeId = null;
        try
        {
            var preferences = await _httpClient.GetFromJsonAsync<UserAppearancePreferencesDto>("/bff/theme", cancellationToken);
            preferredThemeId = preferences?.DefaultThemeId;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load appearance preferences while resolving active theme.");
        }

        if (preferredThemeId.HasValue)
        {
            var preferred = themes.FirstOrDefault(t => t.Id == preferredThemeId.Value);
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return themes.FirstOrDefault(t => t.IsDefault == true) ?? themes[0];
    }

    private static PaletteLight ComposeLight(UiThemePaletteModel dto) => new()
    {
        Primary = dto.Primary,
        Secondary = dto.Secondary,
        Black = BuiltInLight.Black,
        AppbarText = dto.AppbarText,
        AppbarBackground = dto.AppbarBackground,
        Background = dto.Background,
        Surface = dto.Surface,
        DrawerBackground = dto.DrawerBackground,
        DrawerText = dto.DrawerText,
        DrawerIcon = dto.DrawerIcon,
        GrayLight = BuiltInLight.GrayLight,
        GrayLighter = BuiltInLight.GrayLighter,
        TextPrimary = dto.TextPrimary,
        TextSecondary = dto.TextSecondary,
        Info = dto.Info,
        Success = dto.Success,
        Warning = dto.Warning,
        Error = dto.Error,
        LinesDefault = dto.LinesDefault,
        TableLines = dto.LinesDefault,
        Divider = dto.Divider,
        OverlayLight = BuiltInLight.OverlayLight
    };

    private static PaletteDark ComposeDark(UiThemePaletteModel dto) => new()
    {
        Primary = dto.Primary,
        Secondary = dto.Secondary,
        Surface = dto.Surface,
        Background = dto.Background,
        BackgroundGray = BuiltInDark.BackgroundGray,
        AppbarText = dto.AppbarText,
        AppbarBackground = dto.AppbarBackground,
        DrawerBackground = dto.DrawerBackground,
        ActionDefault = BuiltInDark.ActionDefault,
        ActionDisabled = BuiltInDark.ActionDisabled,
        ActionDisabledBackground = BuiltInDark.ActionDisabledBackground,
        TextPrimary = dto.TextPrimary,
        TextSecondary = dto.TextSecondary,
        TextDisabled = BuiltInDark.TextDisabled,
        DrawerIcon = dto.DrawerIcon,
        DrawerText = dto.DrawerText,
        GrayLight = BuiltInDark.GrayLight,
        GrayLighter = BuiltInDark.GrayLighter,
        Info = dto.Info,
        Success = dto.Success,
        Warning = dto.Warning,
        Error = dto.Error,
        LinesDefault = dto.LinesDefault,
        TableLines = dto.LinesDefault,
        Divider = dto.Divider,
        OverlayLight = BuiltInDark.OverlayLight
    };
}
