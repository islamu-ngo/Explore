// ABOUTME: DTO representing instance-level governance settings controlled during onboarding and runtime.
// ABOUTME: Provides a stable contract for deployment mode, tenant onboarding, and moderation defaults.

namespace Explore.Application.DTOs.Onboarding;

public class InstanceGovernanceSettingsDto
{
    public string DeploymentMode { get; set; } = "SingleTenant";
    public bool AllowTenantSelfServiceRegistration { get; set; }
    public bool AllowTenantWhiteLabeling { get; set; }
    public string DefaultPublicHomePage { get; set; } = "EventList";
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
    public bool EnableIslamicModule { get; set; } = true;
    public bool EnableTechModule { get; set; } = true;
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool AllowTenantToOmitVerification { get; set; }
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public bool AllowTenantCustomDomains { get; set; } = true;
    public string DefaultBrandDisplayName { get; set; } = "ISLAMU Explore";
    public string DefaultBrandLogoUrl { get; set; } = string.Empty;
    public string DefaultBrandFaviconUrl { get; set; } = string.Empty;
    public string DefaultBrandCustomCssUrl { get; set; } = string.Empty;
    public bool LockTenantHomePagePreference { get; set; }
    public bool LockTenantSubdomain { get; set; }
    public bool LockTenantCustomDomain { get; set; }
    public bool LockTenantBrandDisplayName { get; set; }
    public bool LockTenantBrandLogoUrl { get; set; }
    public bool LockTenantBrandFaviconUrl { get; set; }
    public bool LockTenantBrandCustomCssUrl { get; set; }

    // Authorization
    public string AuthorizationProvider { get; set; } = "local";
}
