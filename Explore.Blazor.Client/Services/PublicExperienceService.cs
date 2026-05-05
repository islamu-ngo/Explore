// ABOUTME: Client service for anonymous-safe public experience settings used by startup routing and white-label UI.
// ABOUTME: Provides a single route-resolution helper for event-list versus landing-page entry behavior.

using System.Net.Http.Json;
using Explore.Blazor.Client.Models.Analytics;

namespace Explore.Blazor.Client.Services;

public interface IPublicExperienceService
{
    event Action? SettingsChanged;

    Task<PublicExperienceSettingsModel?> GetSettingsAsync();
    Task<PublicExperienceShellModel?> GetShellAsync();
    string ResolveHomeRoute(PublicExperienceSettingsModel? settings);
    string ResolveHomeRoute(PublicExperienceShellModel? shell);
    Task<PublicExperienceSettingsModel?> GetCachedSettingsAsync();
    Task<PublicExperienceShellModel?> GetCachedShellAsync();
    void ResetCache();
}

public class PublicExperienceService : IPublicExperienceService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PublicExperienceService> _logger;
    private PublicExperienceSettingsModel? _cachedSettings;
    private DateTimeOffset _settingsCacheExpiresAt;
    private PublicExperienceShellModel? _cachedShell;
    private DateTimeOffset _shellCacheExpiresAt;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public event Action? SettingsChanged;

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
            CacheSettings(settings);
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience settings, falling back to event list.");
            return null;
        }
    }

    public async Task<PublicExperienceShellModel?> GetShellAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BffClient");
            var shell = await client.GetFromJsonAsync<PublicExperienceShellModel>("api/PublicExperience/shell");
            CacheShell(shell);
            return shell;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load public experience shell, falling back to event list.");
            return null;
        }
    }

    public async Task<PublicExperienceSettingsModel?> GetCachedSettingsAsync()
    {
        if (_cachedSettings != null && DateTimeOffset.UtcNow <= _settingsCacheExpiresAt)
        {
            return _cachedSettings;
        }

        var settings = await GetSettingsAsync();
        return settings ?? _cachedSettings;
    }

    public async Task<PublicExperienceShellModel?> GetCachedShellAsync()
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

    public string ResolveHomeRoute(PublicExperienceSettingsModel? settings)
    {
        return settings?.PreferredHomePage?.Equals("LandingPage", StringComparison.OrdinalIgnoreCase) == true
            ? "/home"
            : "/events";
    }

    public string ResolveHomeRoute(PublicExperienceShellModel? shell)
    {
        if (shell?.Mode?.Equals("OrganizationCentric", StringComparison.OrdinalIgnoreCase) == true
            && shell.PrimaryOrganization.State.Equals("Available", StringComparison.OrdinalIgnoreCase))
        {
            return "/home";
        }

        return "/events";
    }

    private void CacheSettings(PublicExperienceSettingsModel? settings)
    {
        _cachedSettings = settings;
        _settingsCacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
    }

    private void CacheShell(PublicExperienceShellModel? shell)
    {
        _cachedShell = shell;
        _shellCacheExpiresAt = DateTimeOffset.UtcNow.Add(CacheDuration);
    }
}

public class PublicExperienceShellModel
{
    public int SchemaVersion { get; set; } = 1;
    public string Revision { get; set; } = string.Empty;
    public string Mode { get; set; } = "DiscoveryCentric";
    public PublicExperienceHomeModel Home { get; set; } = new();
    public PublicExperienceNavigationModel Navigation { get; set; } = new();
    public PublicExperienceEventCatalogModel EventCatalog { get; set; } = new();
    public PublicExperiencePrimaryOrganizationModel PrimaryOrganization { get; set; } = new();
    public List<PublicExperienceEventSectionModel> EventSections { get; set; } = [];
    public List<PublicExperienceCtaModel> Ctas { get; set; } = [];
    public FooterConfigModel Footer { get; set; } = new();
}

public class PublicExperienceHomeModel
{
    public string PreferredHomePage { get; set; } = "EventList";
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public List<PublicExperienceHomeBlockModel> Blocks { get; set; } = [];
}

public class PublicExperienceHomeBlockModel
{
    public string Key { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkText { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceNavigationModel
{
    public List<PublicExperienceNavigationLinkModel> Links { get; set; } = [];
}

public class PublicExperienceNavigationLinkModel
{
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceEventCatalogModel
{
    public string Label { get; set; } = "Events";
    public string Url { get; set; } = "/events";
}

public class PublicExperiencePrimaryOrganizationModel
{
    public string State { get; set; } = "NotConfigured";
    public Guid? OrganizationId { get; set; }
    public Guid? ActorId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string ProfilePictureUri { get; set; } = string.Empty;
}

public class PublicExperienceEventSectionModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PublicExperienceCtaModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string Placement { get; set; } = "Hero";
    public string Style { get; set; } = "Primary";
}

public class PublicExperienceSettingsModel
{
    public Guid TenantId { get; set; }
    public string Mode { get; set; } = "DiscoveryCentric";
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
    public bool AnnouncementBarEnabled { get; set; }
    public string AnnouncementBarMessage { get; set; } = string.Empty;
    public string AnnouncementBarLinkText { get; set; } = string.Empty;
    public string AnnouncementBarLinkUrl { get; set; } = string.Empty;
    public int AnnouncementBarRevision { get; set; }
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
    public bool IsAiAssistantAvailable { get; set; }
    public bool AiAssistantAllowAnonymousAccess { get; set; }
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
