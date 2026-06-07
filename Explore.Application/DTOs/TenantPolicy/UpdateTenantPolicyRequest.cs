// ABOUTME: Write model for tenant policy updates — writable fields only.
// ABOUTME: CanOverride* flags are NOT included; they are read-only and set by instance governance.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.TenantPolicy;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
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
    public bool AnnouncementBarEnabled { get; set; }
    public string AnnouncementBarMessage { get; set; } = string.Empty;
    public string AnnouncementBarLinkText { get; set; } = string.Empty;
    public string AnnouncementBarLinkUrl { get; set; } = string.Empty;
    public bool ForceAnnouncementBarRedisplay { get; set; }

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

    // AI assistant integration
    public bool AiAssistantEnabled { get; set; }
    public string AiAssistantProvider { get; set; } = "none";
    public string AiAssistantEndpointUrl { get; set; } = string.Empty;
    public string AiAssistantApiKey { get; set; } = string.Empty;
    public string AiAssistantModelId { get; set; } = string.Empty;
    public bool AiAssistantAllowAnonymousAccess { get; set; }

    // API-hosted MCP adapter runtime governance
    public bool McpEnabled { get; set; }
    public bool McpEnableLegacySse { get; set; }
}
