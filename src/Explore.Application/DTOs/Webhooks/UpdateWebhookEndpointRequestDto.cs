// ABOUTME: Request DTO for updating a tenant-scoped outgoing webhook endpoint.
// ABOUTME: Replaces endpoint delivery controls and subscriptions without accepting raw signing secrets.

namespace Explore.Application.DTOs.Webhooks;

public sealed class UpdateWebhookEndpointRequestDto
{
    public required string Url { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<Guid> EventTypeIds { get; init; } = [];

    public int? MaxAttempts { get; init; }

    public int? TimeoutSeconds { get; init; }

    public int? RateLimitPerMinute { get; init; }

    public int ExpectedConfigurationVersion { get; init; }

    public int PendingWorkDecisionId { get; init; }

    public required string PendingWorkReason { get; init; }

    public bool AcknowledgeUncertainProviderPublications { get; init; }
}
