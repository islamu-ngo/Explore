// ABOUTME: Server-side BFF API forwarding helpers for authenticated preference and appearance endpoints.
// ABOUTME: Keeps BffPreferenceEndpoints thin while preserving token-safe generated-client forwarding boundaries.

namespace Explore.Blazor.Services.Preferences;

using Api = Explore.Blazor.Client.Clients;

public interface IBffPreferenceForwardingService
{
    Task<Api.ResolvedAppearanceDto> GetAppearanceAsync(CancellationToken cancellationToken);

    Task<ICollection<Api.AvailablePresetDto>> GetPresetsAsync(CancellationToken cancellationToken);

    Task<ICollection<Api.UserAppearanceProfileDto>> GetProfilesAsync(CancellationToken cancellationToken);

    Task SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<Api.UserAppearanceProfileDto> ClonePresetAsync(Guid presetId, CancellationToken cancellationToken);

    Task<Api.UserAppearanceProfileDto> CreateProfileAsync(Api.CreateCustomProfileRequestDto request, CancellationToken cancellationToken);

    Task<Api.UserAppearanceProfileDto> UpdateProfileAsync(Guid profileId, Api.UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken);

    Task<Api.UiThemePaletteDto> GeneratePaletteAsync(string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken);

    Task ArchiveProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<Api.UserAppearanceProfileDto> DuplicateProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<ICollection<Api.AvailablePresetDto>> GetAvailableThemesAsync(CancellationToken cancellationToken);

    Task PersistPreferencesAsync(BffAppearancePreferences preferences, CancellationToken cancellationToken);
}

public sealed class BffPreferenceForwardingService(Api.IEventApiClient apiClient) : IBffPreferenceForwardingService
{
    public Task<Api.ResolvedAppearanceDto> GetAppearanceAsync(CancellationToken cancellationToken) =>
        apiClient.GetCurrentUserAppearancePreferencesAsync(cancellationToken: cancellationToken);

    public Task<ICollection<Api.AvailablePresetDto>> GetPresetsAsync(CancellationToken cancellationToken) =>
        apiClient.GetAvailableThemesAsync(cancellationToken: cancellationToken);

    public Task<ICollection<Api.UserAppearanceProfileDto>> GetProfilesAsync(CancellationToken cancellationToken) =>
        apiClient.GetUserAppearanceProfilesAsync(cancellationToken: cancellationToken);

    public Task SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken) =>
        apiClient.SetActiveAppearanceProfileAsync(
            new Api.SetActiveProfileRequestDto { ProfileId = profileId },
            cancellationToken: cancellationToken);

    public Task<Api.UserAppearanceProfileDto> ClonePresetAsync(Guid presetId, CancellationToken cancellationToken) =>
        apiClient.ClonePresetToProfileAsync(presetId, cancellationToken: cancellationToken);

    public Task<Api.UserAppearanceProfileDto> CreateProfileAsync(
        Api.CreateCustomProfileRequestDto request,
        CancellationToken cancellationToken) =>
        apiClient.CreateCustomAppearanceProfileAsync(request, cancellationToken: cancellationToken);

    public Task<Api.UserAppearanceProfileDto> UpdateProfileAsync(
        Guid profileId,
        Api.UpdateAppearanceProfileRequestDto request,
        CancellationToken cancellationToken) =>
        apiClient.UpdateAppearanceProfileAsync(profileId, request, cancellationToken: cancellationToken);

    public Task<Api.UiThemePaletteDto> GeneratePaletteAsync(
        string naturalColor,
        string brandColor,
        bool isDark,
        CancellationToken cancellationToken) =>
        apiClient.GenerateAppearancePaletteAsync(
            naturalColor,
            brandColor,
            isDark,
            cancellationToken: cancellationToken);

    public Task ArchiveProfileAsync(Guid profileId, CancellationToken cancellationToken) =>
        apiClient.ArchiveAppearanceProfileAsync(profileId, cancellationToken: cancellationToken);

    public Task<Api.UserAppearanceProfileDto> DuplicateProfileAsync(Guid profileId, CancellationToken cancellationToken) =>
        apiClient.DuplicateAppearanceProfileAsync(profileId, cancellationToken: cancellationToken);

    public Task<ICollection<Api.AvailablePresetDto>> GetAvailableThemesAsync(CancellationToken cancellationToken) =>
        apiClient.GetAvailableThemesAsync(cancellationToken: cancellationToken);

    public async Task PersistPreferencesAsync(
        BffAppearancePreferences preferences,
        CancellationToken cancellationToken)
    {
        var request = new Api.UpdateUserAppearancePreferencesDto
        {
            ThemeMode = preferences.ThemeMode,
            Direction = preferences.Direction,
            Language = preferences.Language,
            DefaultThemeId = preferences.DefaultThemeId
        };

        _ = await apiClient.UpdateCurrentUserAppearancePreferencesAsync(
            request,
            cancellationToken: cancellationToken);
    }
}
