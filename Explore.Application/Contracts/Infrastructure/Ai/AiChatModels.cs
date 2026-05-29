// ABOUTME: Provider-neutral request, response, and error records for AI chat provider adapters.
// ABOUTME: Keeps prompts, proposed actions, usage metadata, and provider errors typed without provider SDK dependencies.

namespace Explore.Application.Contracts.Infrastructure.Ai;

using Explore.Domain.Ai;

public sealed record AiChatRequest(
    string ModelId,
    IReadOnlyList<AiChatMessage> Messages,
    string? SystemPrompt,
    AiChatOptions Options,
    AiStructuredActionSchema? ActionSchema = null);

public sealed record AiChatMessage(
    AiMessageRole Role,
    string Content,
    string? Name = null);

public sealed record AiChatOptions(
    int MaxInputTokens,
    int MaxOutputTokens,
    decimal Temperature,
    int TimeoutSeconds,
    bool ToolProposalsEnabled,
    bool StreamingEnabled);

public sealed record AiStructuredActionSchema(
    IReadOnlyList<AiProposedActionKind> AllowedKinds,
    string JsonSchema);

public sealed record AiChatResponse(
    string AssistantMessage,
    IReadOnlyList<AiProposedActionCandidate> ProposedActions,
    AiTokenUsage Usage,
    string? ProviderRequestId = null,
    string? FinishReason = null);

public sealed record AiProposedActionCandidate(
    AiProposedActionKind Kind,
    string PayloadJson,
    string? Summary = null);

public sealed record AiTokenUsage(
    int? InputTokens,
    int? OutputTokens,
    int? TotalTokens);

public sealed record AiChatProviderError(
    string Code,
    string Message,
    bool IsTransient);

public sealed record AiChatProviderResult(
    bool Succeeded,
    AiChatResponse? Response,
    AiChatProviderError? Error)
{
    public static AiChatProviderResult Success(AiChatResponse response) => new(true, response, null);

    public static AiChatProviderResult Failure(string code, string message, bool isTransient = false) =>
        new(false, null, new AiChatProviderError(code, message, isTransient));
}

public sealed record AiModelDescriptor(
    string Id,
    string DisplayName,
    int? MaxInputTokens = null,
    int? MaxOutputTokens = null,
    bool SupportsToolProposals = false,
    bool SupportsStreaming = false);
