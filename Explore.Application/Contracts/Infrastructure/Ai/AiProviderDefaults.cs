// ABOUTME: Shared provider-neutral AI provider identifiers and deterministic fake model metadata.
// ABOUTME: Lets Application and Infrastructure agree on safe defaults without provider SDK coupling.

namespace Explore.Application.Contracts.Infrastructure.Ai;

public static class AiProviderDefaults
{
    public const string ProviderNone = "none";
    public const string ProviderFake = "fake";
    public const string ProviderOpenAiCompatible = "openai-compatible";
    public const string FakeModelId = "fake-ai-assistant-v1";
    public const string FakeModelDisplayName = "Fake AI Assistant";
    public const int DefaultMaxInputTokens = 8000;
    public const int DefaultMaxOutputTokens = 1024;
}
