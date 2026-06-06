// ABOUTME: Represents safe validation outcomes for AI tool contract and payload checks.
// ABOUTME: Keeps failure codes/messages provider-neutral and free of raw model payload content.

namespace Explore.Application.Features.AiAssistant.Tools;

public sealed record AiToolValidationResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage,
    string? CorrectionMessage = null)
{
    public static AiToolValidationResult Success() => new(true, null, null, null);

    public static AiToolValidationResult Failure(string failureCode, string failureMessage, string? correctionMessage = null)
        => new(false, failureCode, failureMessage, correctionMessage);
}
