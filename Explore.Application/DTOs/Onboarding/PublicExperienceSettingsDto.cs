// ABOUTME: DTO for anonymous/authenticated public experience settings resolved via instance->tenant cascade.
// ABOUTME: Powers home-page routing and white-label branding without requiring admin permissions.

namespace Explore.Application.DTOs.Onboarding;

public class PublicExperienceSettingsDto
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
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public List<string> EnabledModules { get; set; } = new();
    public string AnalyticsProvider { get; set; } = "none";
    public bool AnalyticsEnabled { get; set; }
    public string AnalyticsPublicApiKey { get; set; } = string.Empty;
    public string AnalyticsEndpointUrl { get; set; } = string.Empty;
    public int RenderPolicyVersion { get; set; } = 1;
    public string RenderPolicyPreset { get; set; } = "SeoBalanced";
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = "InteractiveAuto";
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = "InteractiveAuto";
    public bool PublicSeoPrerenderEnabled { get; set; } = true;
    public string OperationalRenderMode { get; set; } = "InteractiveAuto";
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = "InteractiveAuto";
    public bool AdminPrerenderEnabled { get; set; }
    public string OnboardingRenderMode { get; set; } = "InteractiveAuto";
    public bool OnboardingPrerenderEnabled { get; set; }
    public bool DisallowInteractiveServerOnOnboarding { get; set; } = true;
}
