// ABOUTME: Instance-level AI assistant governance settings exposed through the admin settings API.
// ABOUTME: Carries runtime defaults, allowed model IDs, and tenant override lock state for AI configuration.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.Instance;

public sealed record AiAssistantGovernanceSettingsDto
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "openai";
    public string EndpointUrl { get; set; } = string.Empty;
    [JsonIgnore]
    public string ApiKey { get; set; } = string.Empty;
    public bool ApiKeyConfigured { get; init; }
    public string ModelId { get; set; } = string.Empty;
    public IReadOnlyList<string> AllowedModelIds { get; set; } = [];
    public bool AllowAnonymousAccess { get; set; }
    public bool ToolProposalsEnabled { get; set; } = true;
    public bool LockTenantAiAssistant { get; set; } = true;
}
