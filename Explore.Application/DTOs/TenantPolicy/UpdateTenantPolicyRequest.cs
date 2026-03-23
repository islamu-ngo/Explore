// ABOUTME: Write model for tenant policy updates — writable fields only.
// ABOUTME: CanOverride* flags are NOT included; they are read-only and set by instance governance.

namespace Explore.Application.DTOs.TenantPolicy;

public class UpdateTenantPolicyRequest
{
    public bool AllowUserSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSubmittedEvents { get; set; } = true;
    public bool AllowGroupSubmittedEvents { get; set; } = true;
    public bool AllowOrganizationSelfRegistration { get; set; } = true;
    public bool AllowGroupSelfRegistration { get; set; } = true;
    public bool EventCardClickOpensDetailPage { get; set; }
    public bool RequireEventApproval { get; set; }
    public bool RequireOrganizationVerification { get; set; } = true;
    public string PreferredHomePage { get; set; } = "EventList";
    public string Subdomain { get; set; } = string.Empty;
    public string CustomDomain { get; set; } = string.Empty;
    public string BrandDisplayName { get; set; } = string.Empty;
    public string BrandLogoUrl { get; set; } = string.Empty;
    public string BrandFaviconUrl { get; set; } = string.Empty;
    public string BrandCustomCssUrl { get; set; } = string.Empty;

    // Community guidelines
    public string CommunityGuidelinesContent { get; set; } = string.Empty;

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
}
