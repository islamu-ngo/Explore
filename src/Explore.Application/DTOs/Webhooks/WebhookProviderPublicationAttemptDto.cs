// ABOUTME: Safe API evidence for one provider publication or reconciliation attempt.
// ABOUTME: Exposes normalized outcomes and bounded metadata while omitting payloads and credentials.

namespace Explore.Application.DTOs.Webhooks;

public sealed class WebhookProviderPublicationAttemptDto
{
    public Guid Id { get; init; }
    public int AttemptNumber { get; init; }
    public long PublicationFence { get; init; }
    public int OutcomeId { get; init; }
    public required string OutcomeCode { get; init; }
    public required string OutcomeName { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime RecordedAt { get; init; }
    public string? ExternalProviderMessageId { get; init; }
    public string? FailureCategory { get; init; }
    public string? SafeDetail { get; init; }
}
