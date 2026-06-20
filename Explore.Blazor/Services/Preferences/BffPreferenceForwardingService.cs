// ABOUTME: Server-side BFF API forwarding helpers for authenticated preference and appearance endpoints.
// ABOUTME: Keeps BffPreferenceEndpoints thin while preserving token-safe BffClient forwarding boundaries.

namespace Explore.Blazor.Services.Preferences;

using System.Net.Http.Json;
using Explore.Application.DTOs.Appearance;

public interface IBffPreferenceForwardingService
{
    Task<HttpResponseMessage> GetAppearanceAsync(CancellationToken cancellationToken);

    Task<HttpResponseMessage> GetPresetsAsync(CancellationToken cancellationToken);

    Task<HttpResponseMessage> GetProfilesAsync(CancellationToken cancellationToken);

    Task<HttpResponseMessage> SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<HttpResponseMessage> ClonePresetAsync(Guid presetId, CancellationToken cancellationToken);

    Task<HttpResponseMessage> CreateProfileAsync(CreateCustomProfileRequestDto request, CancellationToken cancellationToken);

    Task<HttpResponseMessage> UpdateProfileAsync(Guid profileId, UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken);

    Task<HttpResponseMessage> GeneratePaletteAsync(string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken);

    Task<HttpResponseMessage> ArchiveProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<HttpResponseMessage> DuplicateProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<HttpResponseMessage> GetAvailableThemesAsync(CancellationToken cancellationToken);

    Task<HttpResponseMessage> PersistPreferencesAsync(UserAppearancePreferencesDto preferences, CancellationToken cancellationToken);
}

public sealed class BffPreferenceForwardingService(IHttpClientFactory clientFactory) : IBffPreferenceForwardingService
{
    public Task<HttpResponseMessage> GetAppearanceAsync(CancellationToken cancellationToken)
    {
        return CreateClient().GetAsync("api/user/appearance", cancellationToken);
    }

    public Task<HttpResponseMessage> GetPresetsAsync(CancellationToken cancellationToken)
    {
        return CreateClient().GetAsync("api/user/appearance/presets", cancellationToken);
    }

    public Task<HttpResponseMessage> GetProfilesAsync(CancellationToken cancellationToken)
    {
        return CreateClient().GetAsync("api/user/appearance/profiles", cancellationToken);
    }

    public Task<HttpResponseMessage> SetActiveProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        var request = new SetActiveProfileRequestDto { ProfileId = profileId };
        return CreateClient().PutAsJsonAsync("api/user/appearance/active-profile", request, cancellationToken);
    }

    public Task<HttpResponseMessage> ClonePresetAsync(Guid presetId, CancellationToken cancellationToken)
    {
        return CreateClient().PostAsJsonAsync($"api/user/appearance/profiles/from-preset/{presetId}", (object?)null, cancellationToken);
    }

    public Task<HttpResponseMessage> CreateProfileAsync(CreateCustomProfileRequestDto request, CancellationToken cancellationToken)
    {
        return CreateClient().PostAsJsonAsync("api/user/appearance/profiles", request, cancellationToken);
    }

    public Task<HttpResponseMessage> UpdateProfileAsync(Guid profileId, UpdateAppearanceProfileRequestDto request, CancellationToken cancellationToken)
    {
        return CreateClient().PutAsJsonAsync($"api/user/appearance/profiles/{profileId}", request, cancellationToken);
    }

    public Task<HttpResponseMessage> GeneratePaletteAsync(string naturalColor, string brandColor, bool isDark, CancellationToken cancellationToken)
    {
        var path = $"api/user/appearance/generate-palette?naturalColor={Uri.EscapeDataString(naturalColor)}&brandColor={Uri.EscapeDataString(brandColor)}&isDark={isDark}";
        return CreateClient().GetAsync(path, cancellationToken);
    }

    public Task<HttpResponseMessage> ArchiveProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return CreateClient().PutAsync($"api/user/appearance/profiles/{profileId}/archive", null, cancellationToken);
    }

    public Task<HttpResponseMessage> DuplicateProfileAsync(Guid profileId, CancellationToken cancellationToken)
    {
        return CreateClient().PostAsync($"api/user/appearance/profiles/{profileId}/duplicate", null, cancellationToken);
    }

    public Task<HttpResponseMessage> GetAvailableThemesAsync(CancellationToken cancellationToken)
    {
        return CreateClient().GetAsync("api/user/appearance/presets", cancellationToken);
    }

    public Task<HttpResponseMessage> PersistPreferencesAsync(UserAppearancePreferencesDto preferences, CancellationToken cancellationToken)
    {
        var request = new UpdateUserAppearancePreferencesDto
        {
            ThemeMode = preferences.ThemeMode,
            Direction = preferences.Direction,
            Language = preferences.Language,
            DefaultThemeId = preferences.DefaultThemeId
        };

        return CreateClient().PutAsJsonAsync("api/user/appearance", request, cancellationToken);
    }

    private HttpClient CreateClient() => clientFactory.CreateClient("BffClient");
}
