// ABOUTME: DTO for anonymous/authenticated public experience settings resolved via instance->tenant cascade.
// ABOUTME: Powers home-page routing and white-label branding without requiring admin permissions.

using Explore.Application.DTOs.Footer;

namespace Explore.Application.DTOs.Onboarding;

public class PublicExperienceSettingsDto
{
    public Guid TenantId { get; set; }
    public Explore.Application.Models.PublicExperienceMode Mode { get; set; } = Explore.Application.Models.PublicExperienceMode.DiscoveryCentric;
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
    public AnalyticsConsentBootstrapDto? AnalyticsConsent { get; set; }
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
    public bool DisallowInteractiveServerOnOnboarding { get; set; } = true;
    public bool IsAiAssistantEnabled { get; set; }
    public bool IsAiAssistantAvailable { get; set; }
    public bool AiAssistantAllowAnonymousAccess { get; set; }
    public FooterConfigDto FooterConfig { get; set; } = new();
    public Guid? DefaultThemeId { get; set; }
    public string ThemeMode { get; set; } = "system";
    public string Direction { get; set; } = "auto";
    public string Language { get; set; } = "en";
    public bool ClientPickerEnabled { get; set; } = true;
}
