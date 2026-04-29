// ABOUTME: Implementation of IUserAppearancePreferencesService wrapping IEventApiClient.
// ABOUTME: Handles error catching and logging for user appearance preferences.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public sealed class UserAppearancePreferencesService : IUserAppearancePreferencesService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<UserAppearancePreferencesService> _logger;

    public UserAppearancePreferencesService(IEventApiClient apiClient, ILogger<UserAppearancePreferencesService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Explore.Blazor.Client.Services.Appearance.ResolvedAppearanceDto> GetCurrentPreferencesAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _apiClient.GetCurrentUserAppearancePreferencesAsync(cancellationToken: ct);
            return MapToDomainDto(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user appearance preferences");
            throw;
        }
    }

    public async Task SetActiveProfileAsync(Explore.Blazor.Client.Services.Appearance.SetActiveProfileRequestDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var request = new SetActiveProfileRequestDto
            {
                ProfileId = dto.ProfileId,
                ThemeMode = dto.ThemeMode,
                Direction = dto.Direction,
                Language = dto.Language
            };
            await _apiClient.SetActiveAppearanceProfileAsync(request, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user appearance preferences");
            throw;
        }
    }

    private static Explore.Blazor.Client.Services.Appearance.ResolvedAppearanceDto MapToDomainDto(ResolvedAppearanceDto dto)
    {
        return new Explore.Blazor.Client.Services.Appearance.ResolvedAppearanceDto
        {
            ActiveProfileId = dto.ActiveProfileId,
            SourcePresetId = dto.SourcePresetId,
            SourcePresetKey = dto.SourcePresetKey,
            ResolutionSource = dto.ResolutionSource ?? string.Empty,
            ThemeMode = dto.ThemeMode ?? "system",
            ServerEffectiveDarkMode = dto.ServerEffectiveDarkMode,
            Direction = dto.Direction ?? "auto",
            Language = dto.Language ?? "en",
            Theme = new Explore.Blazor.Client.Services.Appearance.ResolvedThemeDto
            {
                DisplayName = dto.Theme?.DisplayName ?? string.Empty,
                IsSnapshot = dto.Theme?.IsSnapshot ?? false,
                IsUserEditable = dto.Theme?.IsUserEditable ?? false,
                Origin = dto.Theme?.Origin,
                LightPalette = MapPalette(dto.Theme?.LightPalette),
                DarkPalette = MapPalette(dto.Theme?.DarkPalette)
            },
            Capabilities = new Explore.Blazor.Client.Services.Appearance.AppearanceCapabilitiesDto
            {
                CanEditProfile = dto.Capabilities?.CanEditProfile ?? false,
                CanCreateCustomProfile = dto.Capabilities?.CanCreateCustomProfile ?? false,
                CanClonePreset = dto.Capabilities?.CanClonePreset ?? false,
                CanDeleteProfile = dto.Capabilities?.CanDeleteProfile ?? false
            }
        };
    }

    private static Explore.Blazor.Client.Services.Appearance.ClientPaletteDto MapPalette(UiThemePaletteDto? dto)
    {
        if (dto == null) return new Explore.Blazor.Client.Services.Appearance.ClientPaletteDto();

        return new Explore.Blazor.Client.Services.Appearance.ClientPaletteDto
        {
            Primary = dto.Primary ?? string.Empty,
            PrimaryContrastText = dto.PrimaryContrastText ?? "#FFFFFF",
            Secondary = dto.Secondary ?? string.Empty,
            SecondaryContrastText = dto.SecondaryContrastText ?? "#FFFFFF",
            Background = dto.Background ?? string.Empty,
            Surface = dto.Surface ?? string.Empty,
            AppbarBackground = dto.AppbarBackground ?? string.Empty,
            AppbarText = dto.AppbarText ?? string.Empty,
            DrawerBackground = dto.DrawerBackground ?? string.Empty,
            DrawerText = dto.DrawerText ?? string.Empty,
            DrawerIcon = dto.DrawerIcon ?? string.Empty,
            TextPrimary = dto.TextPrimary ?? string.Empty,
            TextSecondary = dto.TextSecondary ?? string.Empty,
            Info = dto.Info ?? string.Empty,
            Success = dto.Success ?? string.Empty,
            Warning = dto.Warning ?? string.Empty,
            Error = dto.Error ?? string.Empty,
            LinesDefault = dto.LinesDefault ?? string.Empty,
            Divider = dto.Divider ?? string.Empty
        };
    }
}
