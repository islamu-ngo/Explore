// ABOUTME: Safe AI assistant bootstrap payload for the authenticated assistant rail.
// ABOUTME: Exposes availability, model choices, feature flags, and limits without secrets or provider endpoints.

namespace Explore.Application.DTOs.Ai;

public sealed class AiAssistantBootstrapDto
{
    public Guid TenantId { get; set; }
    public bool Enabled { get; set; }
    public bool Available { get; set; }
    public string? DisabledReason { get; set; }
    public string Provider { get; set; } = "none";
    public string? DefaultModelId { get; set; }
    public IReadOnlyList<AiAssistantModelDto> Models { get; set; } = [];
    public AiAssistantFeatureFlagsDto Features { get; set; } = new();
    public AiAssistantLimitsDto Limits { get; set; } = new();
    public int RetentionDays { get; set; }
}

public sealed class AiAssistantModelDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? MaxInputTokens { get; set; }
    public int? MaxOutputTokens { get; set; }
    public bool SupportsToolProposals { get; set; }
    public bool SupportsStreaming { get; set; }
}

public sealed class AiAssistantFeatureFlagsDto
{
    public bool ToolProposalsEnabled { get; set; }
    public bool StreamingEnabled { get; set; }
}

public sealed class AiAssistantLimitsDto
{
    public int MaxInputTokens { get; set; }
    public int MaxOutputTokens { get; set; }
    public decimal Temperature { get; set; }
    public int TimeoutSeconds { get; set; }
    public int DailyMessageLimit { get; set; }
}
