// ABOUTME: DTO for tenant-level onboarding and runtime policy settings.
// ABOUTME: Contains actionable policy values and delegation constraints from instance governance.

namespace Explore.Application.DTOs.Onboarding;

public class TenantPolicySettingsDto
{
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public bool RequireEventApproval { get; set; }
    public bool RequireOrganizationVerification { get; set; } = true;
    public bool CanTenantOmitVerification { get; set; }
    public bool IsTenantWhiteLabelingEnabled { get; set; }
    public string PreferredHomePage { get; set; } = "EventList";
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;
    public bool AnnouncementBarEnabled { get; set; }
    public string AnnouncementBarMessage { get; set; } = string.Empty;
    public string AnnouncementBarLinkText { get; set; } = string.Empty;
    public string AnnouncementBarLinkUrl { get; set; } = string.Empty;
    public int AnnouncementBarRevision { get; set; }
    public bool CanOverrideHomePagePreference { get; set; } = true;
    public bool CanOverrideSubdomain { get; set; } = true;
    public bool CanOverrideCustomDomain { get; set; } = true;
    public bool CanOverrideBrandDisplayName { get; set; } = true;
    public bool CanOverrideBrandLogoUrl { get; set; } = true;
    public bool CanOverrideBrandFaviconUrl { get; set; } = true;
    public bool CanOverrideBrandCustomCssUrl { get; set; } = true;
    public bool CanOverrideEventCardClickBehavior { get; set; } = true;

    // Render policy tenant overrides
    public string RenderPolicyPreset { get; set; } = string.Empty;
    public bool EnableAdvancedRenderPolicyOverrides { get; set; }
    public string GlobalRenderMode { get; set; } = string.Empty;
    public bool GlobalPrerenderEnabled { get; set; }
    public string PublicSeoRenderMode { get; set; } = string.Empty;
    public bool PublicSeoPrerenderEnabled { get; set; }
    public string OperationalRenderMode { get; set; } = string.Empty;
    public bool OperationalPrerenderEnabled { get; set; }
    public string AdminRenderMode { get; set; } = string.Empty;
    public bool AdminPrerenderEnabled { get; set; }
    public bool CanOverrideRenderPolicy { get; set; }
    public bool CanOverridePublicSeoRenderPolicy { get; set; }
    public bool CanOverrideOperationalRenderPolicy { get; set; }
    public bool CanOverrideAdminRenderPolicy { get; set; }

    // Community guidelines
    public string CommunityGuidelinesContent { get; set; } = string.Empty;
    public bool CanOverrideCommunityGuidelines { get; set; } = true;

    // Category-level override flags (inverse of instance lock)
    public bool CanOverrideSmtp { get; set; }
    public bool CanOverrideStorage { get; set; }
    public bool CanOverrideAnalytics { get; set; }
    public bool CanOverrideAiAssistant { get; set; }

    // AI assistant integration
    public bool AiAssistantEnabled { get; set; }
    public string AiAssistantEndpointUrl { get; set; } = string.Empty;
    public string AiAssistantApiKey { get; set; } = string.Empty;
    public bool AiAssistantAllowAnonymousAccess { get; set; }
}
