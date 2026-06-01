// ABOUTME: Represents safe validation outcomes for AI tool contract and payload checks.
// ABOUTME: Keeps failure codes/messages provider-neutral and free of raw model payload content.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolValidationResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage)
{
    public static AiToolValidationResult Success() => new(true, null, null);

    public static AiToolValidationResult Failure(string failureCode, string failureMessage)
        => new(false, failureCode, failureMessage);
}
