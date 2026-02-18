// ABOUTME: Client service for anonymous-safe public experience settings used by startup routing and white-label UI.
// ABOUTME: Provides a single route-resolution helper for event-list versus landing-page entry behavior.

using System.Net.Http.Json;

namespace Explore.Blazor.Client.Services;

public interface IPublicExperienceService
{
    Task<PublicExperienceSettingsModel?> GetSettingsAsync();
    string ResolveHomeRoute(PublicExperienceSettingsModel? settings);
}

public class PublicExperienceService : IPublicExperienceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicExperienceService> _logger;

    public PublicExperienceService(
        IHttpClientFactory httpClientFactory,
        ILogger<PublicExperienceService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<PublicExperienceSettingsModel?> GetSettingsAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            return await client.GetFromJsonAsync<PublicExperienceSettingsModel>("api/v1/PublicExperience/settings");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience settings, falling back to event list.");
            return null;
        }
    }

    public string ResolveHomeRoute(PublicExperienceSettingsModel? settings)
    {
        return settings?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true
            ? "/home"
            : "/events";
    }
}

public class PublicExperienceSettingsModel
{
    public Guid TenantId { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
    public string PreferredHomePage { get; set; } = "EventList";
    public string BrandDisplayName { get; set; } = "ISLAMU Explore";
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public bool IsIslamicModuleEnabled { get; set; }
    public bool IsTechModuleEnabled { get; set; }
    public List<string> EnabledModules { get; set; } = new();
    public string AnalyticsProvider { get; set; } = "none";
    public bool AnalyticsEnabled { get; set; }
    public string AnalyticsPublicApiKey { get; set; } = string.Empty;
    public string AnalyticsEndpointUrl { get; set; } = string.Empty;
}
