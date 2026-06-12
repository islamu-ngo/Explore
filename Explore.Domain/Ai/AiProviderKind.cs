// ABOUTME: Defines normalized AI provider kind identifiers for lookup-backed FK references.
// ABOUTME: Each enum value maps to an ai_provider_kinds row via stable integer ID.

namespace Explore.Domain.Ai;

public enum AiProviderKind
{
    None = 1,
    Fake = 2,
    OpenAiCompatible = 3,
    AnthropicCompatible = 4,
    OpenAiSdk = 5,
    AzureOpenAi = 6
}
