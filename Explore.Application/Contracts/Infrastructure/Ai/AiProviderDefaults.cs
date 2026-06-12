// ABOUTME: Normalized AI provider identifiers and operational defaults.
// ABOUTME: String constants for governance settings; int IDs reference ai_provider_kinds lookup table.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public static class AiProviderDefaults
{
    public const string ProviderNone = "none";
    public const string ProviderFake = "fake";
    public const string ProviderOpenAiCompatible = "openai-compatible";
    public const string ProviderAnthropicCompatible = "anthropic-compatible";
    public const string ProviderOpenAiSdk = "openai-sdk";
    public const string ProviderAzureOpenAi = "azure-openai";

    public const int ProviderIdNone = 1;
    public const int ProviderIdFake = 2;
    public const int ProviderIdOpenAiCompatible = 3;
    public const int ProviderIdAnthropicCompatible = 4;
    public const int ProviderIdOpenAiSdk = 5;
    public const int ProviderIdAzureOpenAi = 6;

    public const string FakeModelId = "fake-ai-assistant-v1";
    public const string FakeModelDisplayName = "Fake AI Assistant";
    public const int DefaultMaxInputTokens = 8000;
    public const int DefaultMaxOutputTokens = 1024;
    public const int DefaultTimeoutSeconds = 120;
    public const int LocalProviderTimeoutSeconds = 300;
    public const int MaxTimeoutSeconds = 300;

    public static int ProviderNameToId(string? providerName) => providerName?.Trim().ToLowerInvariant() switch
    {
        ProviderFake => ProviderIdFake,
        ProviderOpenAiCompatible => ProviderIdOpenAiCompatible,
        ProviderAnthropicCompatible => ProviderIdAnthropicCompatible,
        ProviderOpenAiSdk => ProviderIdOpenAiSdk,
        ProviderAzureOpenAi => ProviderIdAzureOpenAi,
        _ => ProviderIdNone
    };

    public static string ProviderIdToLabel(int providerId) => providerId switch
    {
        ProviderIdNone => ProviderNone,
        ProviderIdFake => ProviderFake,
        ProviderIdOpenAiCompatible => ProviderOpenAiCompatible,
        ProviderIdAnthropicCompatible => ProviderAnthropicCompatible,
        ProviderIdOpenAiSdk => ProviderOpenAiSdk,
        ProviderIdAzureOpenAi => ProviderAzureOpenAi,
        _ => "unknown"
    };
}
