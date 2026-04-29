// ABOUTME: Central runtime service for composing MudBlazor themes and managing appearance state.
// ABOUTME: Supports preset-based selection, user-owned profiles, custom theme generation, and System mode resolution.

using System.Net.Http.Json;
using Explore.Blazor.Client.Services.Appearance;
using MudBlazor;

namespace Explore.Blazor.Client.Services;

public sealed class AppearanceThemeService : IAppearanceThemeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AppearanceThemeService> _logger;

    private AppearanceState _current = new();
    private bool _isInitialized;

    private static readonly PaletteLight BuiltInLight = new()
    {
        Primary = "#0F62FE",
        Secondary = "#475569",
        Black = "#09090B",
        AppbarText = "#1E293B",
        AppbarBackground = "#FFFFFF",
        Background = "#F1F5F9",
        Surface = "#FFFFFF",
        DrawerBackground = "#FFFFFF",
        DrawerText = "#1E293B",
        DrawerIcon = "#475569",
        GrayLight = "#E2E8F0",
        GrayLighter = "#F8FAFC",
        TextPrimary = "#0F172A",
        TextSecondary = "#475569",
        Info = "#2563EB",
        Success = "#16A34A",
        Warning = "#D97706",
        Error = "#DC2626",
        LinesDefault = "#CBD5E1",
        TableLines = "#CBD5E1",
        Divider = "#CBD5E1",
        OverlayLight = "rgba(248,250,252,0.8)"
    };

    private static readonly PaletteDark BuiltInDark = new()
    {
        Primary = "#3B82F6",
        Secondary = "#F1F5F9",
        Surface = "#1E293B",
        Background = "#0B0F19",
        BackgroundGray = "#1E293B",
        AppbarText = "#F1F5F9",
        AppbarBackground = "rgba(11,15,25,0.85)",
        DrawerBackground = "#0B0F19",
        ActionDefault = "#94A3B8",
        ActionDisabled = "#334155",
        ActionDisabledBackground = "#1E293B",
        TextPrimary = "#F8FAFC",
        TextSecondary = "#94A3B8",
        TextDisabled = "#475569",
        DrawerIcon = "#CBD5E1",
        DrawerText = "#F1F5F9",
        GrayLight = "#334155",
        GrayLighter = "#1E293B",
        Info = "#60A5FA",
        Success = "#10B981",
        Warning = "#F59E0B",
        Error = "#EF4444",
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

    public AppearanceState Current => _current;
    public event EventHandler<AppearanceStateChangedEventArgs>? Changed;

    public AppearanceThemeService(HttpClient httpClient, ILogger<AppearanceThemeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task InitializeAsync(MudThemeProvider themeProvider, CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ResolvedAppearanceDto>("/bff/appearance", cancellationToken);
            if (response is not null)
            {
                _current.ResolvedAppearance = response;
                _current.ThemeMode = response.ThemeMode;
                _current.ServerEffectiveDarkMode = response.ServerEffectiveDarkMode;
                _current.Direction = response.Direction;
                _current.Language = response.Language;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load resolved appearance from BFF.");
        }

        try
        {
            var presets = await _httpClient.GetFromJsonAsync<IReadOnlyList<AvailablePresetDto>>("/bff/appearance/presets", cancellationToken);
            _current.AvailablePresets = presets ?? Array.Empty<AvailablePresetDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading available presets from BFF.");
            _current.AvailablePresets = Array.Empty<AvailablePresetDto>();
        }

        try
        {
            var profiles = await _httpClient.GetFromJsonAsync<IReadOnlyList<UserAppearanceProfileDto>>("/bff/appearance/profiles", cancellationToken);
            _current.UserProfiles = profiles ?? Array.Empty<UserAppearanceProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not load user appearance profiles from BFF.");
            _current.UserProfiles = Array.Empty<UserAppearanceProfileDto>();
        }

        _current.IsInitialized = true;
        _isInitialized = true;
        Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
    }

    public async Task SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        var previousMode = _current.ThemeMode;
        var previousDirection = _current.Direction;
        var previousLanguage = _current.Language;

        try
        {
            await _httpClient.PutAsJsonAsync("/bff/appearance/active-profile", new SetActiveProfileRequestDto { ProfileId = profileId }, cancellationToken);
            _current.ResolvedAppearance = await _httpClient.GetFromJsonAsync<ResolvedAppearanceDto>("/bff/appearance", cancellationToken);
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting active profile.");
            _current.ThemeMode = previousMode;
            _current.Direction = previousDirection;
            _current.Language = previousLanguage;
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
    }

    public async Task ClonePresetAndActivateAsync(Guid presetId, CancellationToken cancellationToken = default)
    {
        var previousMode = _current.ThemeMode;
        var previousDirection = _current.Direction;
        var previousLanguage = _current.Language;

        try
        {
            var profile = await _httpClient.PostAsJsonAsync($"/bff/appearance/profiles/from-preset/{presetId}", new ClonePresetRequestDto(), cancellationToken);
            var profileDto = await profile.Content.ReadFromJsonAsync<UserAppearanceProfileDto>(cancellationToken: cancellationToken);

            if (profileDto is not null)
            {
                await _httpClient.PutAsJsonAsync("/bff/appearance/active-profile", new SetActiveProfileRequestDto { ProfileId = profileDto.Id }, cancellationToken);
                _current.ResolvedAppearance = await _httpClient.GetFromJsonAsync<ResolvedAppearanceDto>("/bff/appearance", cancellationToken);
            }

            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error cloning preset.");
            _current.ThemeMode = previousMode;
            _current.Direction = previousDirection;
            _current.Language = previousLanguage;
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
    }

    public async Task SetThemeModeAsync(string mode, CancellationToken cancellationToken = default)
    {
        var previousMode = _current.ThemeMode;

        try
        {
            _current.ThemeMode = mode;
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });

            await _httpClient.PutAsJsonAsync("/bff/appearance/mode", new SetThemeModeRequestDto { ThemeMode = mode }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting theme mode.");
            _current.ThemeMode = previousMode;
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
    }

    public async Task UpdateCurrentProfileAsync(UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        var activeProfileId = _current.ResolvedAppearance?.ActiveProfileId;
        if (activeProfileId is null) return;

        try
        {
            await _httpClient.PutAsJsonAsync($"/bff/appearance/profiles/{activeProfileId}", request, cancellationToken);
            _current.ResolvedAppearance = await _httpClient.GetFromJsonAsync<ResolvedAppearanceDto>("/bff/appearance", cancellationToken);
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating current profile.");
        }
    }

    public async Task<UserAppearanceProfileDto?> CreateCustomProfileAsync(CreateCustomProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/bff/appearance/profiles", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserAppearanceProfileDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error creating custom profile.");
            return null;
        }
    }

    public ClientPaletteDto GeneratePalettePreview(string naturalColor, string brandColor, bool isDark)
    {
        try
        {
            var response = _httpClient.GetFromJsonAsync<ClientPaletteDto>(
                $"/bff/appearance/generate-palette?naturalColor={Uri.EscapeDataString(naturalColor)}&brandColor={Uri.EscapeDataString(brandColor)}&isDark={isDark}",
                CancellationToken.None);
            return response.Result ?? GetFallbackPalette(isDark);
        }
        catch
        {
            return GetFallbackPalette(isDark);
        }
    }

    public async Task<ClientPaletteDto?> GeneratePalettePreviewAsync(string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ClientPaletteDto>(
                $"/bff/appearance/generate-palette?naturalColor={Uri.EscapeDataString(naturalColor)}&brandColor={Uri.EscapeDataString(brandColor)}&isDark={isDark}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generating palette preview.");
            return null;
        }
    }

    public MudTheme CreateTheme(string appbarHeight)
    {
        var theme = _current.ResolvedAppearance?.Theme;
        var light = theme?.LightPalette is not null ? ComposeLight(theme.LightPalette) : BuiltInLight;
        var dark = theme?.DarkPalette is not null ? ComposeDark(theme.DarkPalette) : BuiltInDark;

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

    public async Task<bool> ResolveEffectiveDarkModeAsync(MudThemeProvider themeProvider)
    {
        var mode = _current.ThemeMode.ToLowerInvariant();

        if (mode is "dark" or "darkhighcontrast") return true;
        if (mode is "light" or "lighthighcontrast") return false;

        if (_current.ServerEffectiveDarkMode.HasValue)
        {
            return _current.ServerEffectiveDarkMode.Value;
        }

        try
        {
            return await themeProvider.GetSystemDarkModeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving system dark mode preference.");
            return false;
        }
    }

    public async Task ArchiveProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _httpClient.PutAsync($"/bff/appearance/profiles/{profileId}/archive", null, cancellationToken);
            await RefreshProfilesAsync(cancellationToken);
            Changed?.Invoke(this, new AppearanceStateChangedEventArgs { State = _current });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error archiving profile.");
        }
    }

    public async Task<UserAppearanceProfileDto?> DuplicateProfileAsync(Guid profileId, string? name, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = name is not null
                ? JsonContent.Create(new { Name = name })
                : null;
            var response = await _httpClient.PostAsync($"/bff/appearance/profiles/{profileId}/duplicate", request, cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserAppearanceProfileDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error duplicating profile.");
            return null;
        }
    }

    public async Task RefreshProfilesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var profiles = await _httpClient.GetFromJsonAsync<IReadOnlyList<UserAppearanceProfileDto>>("/bff/appearance/profiles", cancellationToken);
            _current.UserProfiles = profiles ?? Array.Empty<UserAppearanceProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing profiles.");
        }
    }

    private static ClientPaletteDto GetFallbackPalette(bool isDark) => isDark
        ? new ClientPaletteDto
        {
            Primary = "#3B82F6", PrimaryContrastText = "#FFFFFF", Secondary = "#F1F5F9", SecondaryContrastText = "#0F172A",
            Background = "#0B0F19", Surface = "#1E293B", AppbarBackground = "rgba(11,15,25,0.85)", AppbarText = "#F1F5F9",
            DrawerBackground = "#0B0F19", DrawerText = "#F1F5F9", DrawerIcon = "#CBD5E1",
            TextPrimary = "#F8FAFC", TextSecondary = "#94A3B8", Info = "#60A5FA", Success = "#10B981", Warning = "#F59E0B", Error = "#EF4444",
            LinesDefault = "#334155", Divider = "#1E293B"
        }
        : new ClientPaletteDto
        {
            Primary = "#0F62FE", PrimaryContrastText = "#FFFFFF", Secondary = "#475569", SecondaryContrastText = "#FFFFFF",
            Background = "#F1F5F9", Surface = "#FFFFFF", AppbarBackground = "#FFFFFF", AppbarText = "#1E293B",
            DrawerBackground = "#FFFFFF", DrawerText = "#1E293B", DrawerIcon = "#475569",
            TextPrimary = "#0F172A", TextSecondary = "#475569", Info = "#2563EB", Success = "#16A34A", Warning = "#D97706", Error = "#DC2626",
            LinesDefault = "#CBD5E1", Divider = "#CBD5E1"
        };

    private static PaletteLight ComposeLight(ClientPaletteDto dto) => new()
    {
        Primary = dto.Primary ?? BuiltInLight.Primary.ToString(),
        Secondary = dto.Secondary ?? BuiltInLight.Secondary.ToString(),
        Black = BuiltInLight.Black,
        AppbarText = dto.AppbarText ?? BuiltInLight.AppbarText.ToString(),
        AppbarBackground = dto.AppbarBackground ?? BuiltInLight.AppbarBackground.ToString(),
        Background = dto.Background ?? BuiltInLight.Background.ToString(),
        Surface = dto.Surface ?? BuiltInLight.Surface.ToString(),
        DrawerBackground = dto.DrawerBackground ?? BuiltInLight.DrawerBackground.ToString(),
        DrawerText = dto.DrawerText ?? BuiltInLight.DrawerText.ToString(),
        DrawerIcon = dto.DrawerIcon ?? BuiltInLight.DrawerIcon.ToString(),
        GrayLight = BuiltInLight.GrayLight,
        GrayLighter = BuiltInLight.GrayLighter,
        TextPrimary = dto.TextPrimary ?? BuiltInLight.TextPrimary.ToString(),
        TextSecondary = dto.TextSecondary ?? BuiltInLight.TextSecondary.ToString(),
        Info = dto.Info ?? BuiltInLight.Info.ToString(),
        Success = dto.Success ?? BuiltInLight.Success.ToString(),
        Warning = dto.Warning ?? BuiltInLight.Warning.ToString(),
        Error = dto.Error ?? BuiltInLight.Error.ToString(),
        LinesDefault = dto.LinesDefault ?? BuiltInLight.LinesDefault.ToString(),
        TableLines = dto.LinesDefault ?? BuiltInLight.TableLines.ToString(),
        Divider = dto.Divider ?? BuiltInLight.Divider.ToString(),
        OverlayLight = BuiltInLight.OverlayLight
    };

    private static PaletteDark ComposeDark(ClientPaletteDto dto) => new()
    {
        Primary = dto.Primary ?? BuiltInDark.Primary.ToString(),
        Secondary = dto.Secondary ?? BuiltInDark.Secondary.ToString(),
        Surface = dto.Surface ?? BuiltInDark.Surface.ToString(),
        Background = dto.Background ?? BuiltInDark.Background.ToString(),
        BackgroundGray = BuiltInDark.BackgroundGray,
        AppbarText = dto.AppbarText ?? BuiltInDark.AppbarText.ToString(),
        AppbarBackground = dto.AppbarBackground ?? BuiltInDark.AppbarBackground.ToString(),
        DrawerBackground = dto.DrawerBackground ?? BuiltInDark.DrawerBackground.ToString(),
        ActionDefault = BuiltInDark.ActionDefault,
        ActionDisabled = BuiltInDark.ActionDisabled,
        ActionDisabledBackground = BuiltInDark.ActionDisabledBackground,
        TextPrimary = dto.TextPrimary ?? BuiltInDark.TextPrimary.ToString(),
        TextSecondary = dto.TextSecondary ?? BuiltInDark.TextSecondary.ToString(),
        TextDisabled = BuiltInDark.TextDisabled,
        DrawerIcon = dto.DrawerIcon ?? BuiltInDark.DrawerIcon.ToString(),
        DrawerText = dto.DrawerText ?? BuiltInDark.DrawerText.ToString(),
        GrayLight = BuiltInDark.GrayLight,
        GrayLighter = BuiltInDark.GrayLighter,
        Info = dto.Info ?? BuiltInDark.Info.ToString(),
        Success = dto.Success ?? BuiltInDark.Success.ToString(),
        Warning = dto.Warning ?? BuiltInDark.Warning.ToString(),
        Error = dto.Error ?? BuiltInDark.Error.ToString(),
        LinesDefault = dto.LinesDefault ?? BuiltInDark.LinesDefault.ToString(),
        TableLines = dto.LinesDefault ?? BuiltInDark.TableLines.ToString(),
        Divider = dto.Divider ?? BuiltInDark.Divider.ToString(),
        OverlayLight = BuiltInDark.OverlayLight
    };
}