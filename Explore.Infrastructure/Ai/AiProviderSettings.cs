// ABOUTME: Defines resolved AI provider settings consumed by Infrastructure adapters.
// ABOUTME: Keeps provider validation inputs separate from Application contracts and provider SDKs.

namespace Explore.Infrastructure.Ai;

using Explore.Application.Contracts.Infrastructure.Ai;

public sealed class AiProviderSettings
{
    public const string SectionName = "AiProvider";

    public const string ProviderNone = AiProviderDefaults.ProviderNone;
    public const string ProviderFake = AiProviderDefaults.ProviderFake;
    public const string ProviderOpenAiCompatible = AiProviderDefaults.ProviderOpenAiCompatible;

    public bool Enabled { get; set; }
    public string Provider { get; set; } = ProviderNone;
    public string EndpointUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public int MaxInputTokens { get; set; } = 8000;
    public int MaxOutputTokens { get; set; } = 1024;
    public decimal Temperature { get; set; } = 0.2m;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetentionDays { get; set; } = 30;
    public int DailyMessageLimit { get; set; } = 50;
    public bool ToolProposalsEnabled { get; set; }
    public bool StreamingEnabled { get; set; }
    public bool AllowLocalProviderEndpoints { get; set; }
}
