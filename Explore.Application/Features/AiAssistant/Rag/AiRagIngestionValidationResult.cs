// ABOUTME: Represents safe validation outcomes for future AI RAG ingestion candidates.
// ABOUTME: Uses stable failure codes without echoing summary text, tenant IDs, or private content.

namespace Explore.Application.Features.AiAssistant.Rag;

public sealed record AiRagIngestionValidationResult(
    bool Succeeded,
    string? FailureCode,
    string? FailureMessage)
{
    public static AiRagIngestionValidationResult Success() => new(true, null, null);

    public static AiRagIngestionValidationResult Failure(string failureCode, string failureMessage)
        => new(false, failureCode, failureMessage);
}
