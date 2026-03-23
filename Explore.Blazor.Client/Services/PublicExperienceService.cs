// ABOUTME: Client service for anonymous-safe public experience settings used by startup routing and white-label UI.
// ABOUTME: Provides a single route-resolution helper for event-list versus landing-page entry behavior.

using System.Net.Http.Json;
using Explore.Blazor.Client.Models.Analytics;

namespace Explore.Blazor.Client.Services;

public interface IPublicExperienceService
{
    Task<PublicExperienceSettingsModel?> GetSettingsAsync();
    string ResolveHomeRoute(PublicExperienceSettingsModel? settings);
    Task<PublicExperienceSettingsModel?> GetCachedSettingsAsync();
    void ResetCache();
}

public class PublicExperienceService : IPublicExperienceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicExperienceService> _logger;
    private PublicExperienceSettingsModel? _cachedSettings;
    private DateTimeOffset _cacheExpiresAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

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
            var settings = await client.GetFromJsonAsync<PublicExperienceSettingsModel>("api/PublicExperience/settings");
            Cache(settings);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience settings, falling back to event list.");
            return null;
        }
    }

    public async Task<PublicExperienceSettingsModel?> GetCachedSettingsAsync()
    {
        if (_cachedSettings != null && DateTimeOffset.UtcNow <= _cacheExpiresAt)
        {
            return _cachedSettings;
        }

        var settings = await GetSettingsAsync();
        return settings ?? _cachedSettings;
    }

    public void ResetCache()
    {
        _cachedSettings = null;
        _cacheExpiresAt = default;
    }

    public string ResolveHomeRoute(PublicExperienceSettingsModel? settings)
    {
        return settings?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true
            ? "/home"
            : "/events";
    }

    private void Cache(PublicExperienceSettingsModel? settings)
    {
        _cachedSettings = settings;
        _cacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
    }
}

public class PublicExperienceSettingsModel
{
    public Guid TenantId { get; set; }
    public string DeploymentMode { get; set; } = "SingleTenant";
    public string PreferredHomePage { get; set; } = "EventList";
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public bool IsIslamicModuleEnabled { get; set; }
    public bool IsTechModuleEnabled { get; set; }
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public string CommunityGuidelinesContent { get; set; } = string.Empty;
    public List<string> EnabledModules { get; set; } = new();
    public string AnalyticsProvider { get; set; } = "none";
    public bool AnalyticsEnabled { get; set; }
    public string AnalyticsConsentMode { get; set; } = "pseudonymous";
    public string AnalyticsTransportMode { get; set; } = "direct";
    public bool AnalyticsAllowIdentify { get; set; }
    public string AnalyticsPublicApiKey { get; set; } = string.Empty;
    public string AnalyticsEndpointUrl { get; set; } = string.Empty;
    public AnalyticsConsentBootstrapModel? AnalyticsConsent { get; set; }
    public int RenderPolicyVersion { get; set; } = 1;
    public string RenderPolicyPreset { get; set; } = "AllInteractiveServer";
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = "InteractiveServer";
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = "InteractiveServer";
    public bool PublicSeoPrerenderEnabled { get; set; }
    public string OperationalRenderMode { get; set; } = "InteractiveServer";
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = "InteractiveServer";
    public bool AdminPrerenderEnabled { get; set; }
    public string OnboardingRenderMode { get; set; } = "InteractiveServer";
    public bool OnboardingPrerenderEnabled { get; set; }
    public bool DisallowInteractiveServerOnOnboarding { get; set; } = false;
    public FooterConfigModel FooterConfig { get; set; } = new();
}

// ── Footer client-side models (mirror server DTOs for JSON deserialization) ──

public class FooterConfigModel
{
    public FooterSettingsModel Settings { get; set; } = new();
    public List<FooterLinkGroupModel> LinkGroups { get; set; } = [];
}

public class FooterSettingsModel
{
    public bool Enabled { get; set; } = true;
    public string Template { get; set; } = "standard-3-col";
    public bool ShowDescription { get; set; } = true;
    public string DescriptionText { get; set; } = string.Empty;
    public bool ShowSocialLinks { get; set; } = true;
    public List<FooterSocialLinkModel> SocialLinks { get; set; } = [];
    public string CopyrightText { get; set; } = string.Empty;
    public bool ShowCookieSettingsLink { get; set; } = true;
}

public class FooterLinkGroupModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<FooterLinkItemModel> Links { get; set; } = [];
}

public class FooterLinkItemModel
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public int Order { get; set; }
}

public class FooterSocialLinkModel
{
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
