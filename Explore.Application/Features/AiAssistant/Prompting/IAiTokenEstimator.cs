// ABOUTME: Defines Application-owned token estimation for AI prompt budgeting.
// ABOUTME: Keeps tokenizer-backed implementations optional without leaking provider SDKs into Application.

namespace Explore.Application.Features.AiAssistant.Prompting;

public interface IAiTokenEstimator
{
    bool IsTokenizerBacked { get; }

    int CountTokens(string? content);
}
