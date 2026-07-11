// ABOUTME: Client service for anonymous-safe public experience settings used by startup routing and white-label UI.
// ABOUTME: Provides a single route-resolution helper for event-list versus landing-page entry behavior.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface IPublicExperienceService
{
    event Action? SettingsChanged;

    Task<PublicExperienceSettingsDto?> GetSettingsAsync();
    Task<PublicExperienceShellDto?> GetShellAsync();
    string ResolveHomeRoute(PublicExperienceSettingsDto? settings);
    string ResolveHomeRoute(PublicExperienceShellDto? shell);
    Task<PublicExperienceSettingsDto?> GetCachedSettingsAsync();
    Task<PublicExperienceShellDto?> GetCachedShellAsync();
    void ResetCache();
}

public class PublicExperienceService : IPublicExperienceService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<PublicExperienceService> _logger;
    private PublicExperienceSettingsDto? _cachedSettings;
    private DateTimeOffset _settingsCacheExpiresAt;
    private PublicExperienceShellDto? _cachedShell;
    private DateTimeOffset _shellCacheExpiresAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public event Action? SettingsChanged;

    public PublicExperienceService(
        IEventApiClient apiClient,
        ILogger<PublicExperienceService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PublicExperienceSettingsDto?> GetSettingsAsync()
    {
        try
        {
            var settings = await _apiClient.GetPublicExperienceSettingsAsync(cancellationToken: CancellationToken.None);
            CacheSettings(settings);
            return settings;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(
                "Failed to load public experience settings, falling back to event list. Status: {StatusCode}.",
                ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience settings, falling back to event list.");
            return null;
        }
    }

    public async Task<PublicExperienceShellDto?> GetShellAsync()
    {
        try
        {
            var shell = await _apiClient.GetPublicExperienceShellAsync(cancellationToken: CancellationToken.None);
            CacheShell(shell);
            return shell;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(
                "Failed to load public experience shell, falling back to event list. Status: {StatusCode}.",
                ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience shell, falling back to event list.");
            return null;
        }
    }

    public async Task<PublicExperienceSettingsDto?> GetCachedSettingsAsync()
    {
        if (_cachedSettings != null && DateTimeOffset.UtcNow <= _settingsCacheExpiresAt)
        {
            return _cachedSettings;
        }

        var settings = await GetSettingsAsync();
        return settings ?? _cachedSettings;
    }

    public async Task<PublicExperienceShellDto?> GetCachedShellAsync()
    {
        if (_cachedShell != null && DateTimeOffset.UtcNow <= _shellCacheExpiresAt)
        {
            return _cachedShell;
        }

        var shell = await GetShellAsync();
        return shell ?? _cachedShell;
    }

    public void ResetCache()
    {
        _cachedSettings = null;
        _settingsCacheExpiresAt = default;
        _cachedShell = null;
        _shellCacheExpiresAt = default;
        SettingsChanged?.Invoke();
    }

    public string ResolveHomeRoute(PublicExperienceSettingsDto? settings)
    {
        return settings?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true
            ? "/home"
            : "/events";
    }

    public string ResolveHomeRoute(PublicExperienceShellDto? shell)
    {
        if (shell?.Home?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "/home";
        }

        if (shell?.Mode == PublicExperienceMode.OrganizationCentric
            && shell.PrimaryOrganization?.State == PublicExperiencePrimaryOrganizationState.Available)
        {
            return "/home";
        }

        return "/events";
    }

    private void CacheSettings(PublicExperienceSettingsDto? settings)
    {
        _cachedSettings = settings;
        _settingsCacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
    }

    private void CacheShell(PublicExperienceShellDto? shell)
    {
        _cachedShell = shell;
        _shellCacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
    }
}
