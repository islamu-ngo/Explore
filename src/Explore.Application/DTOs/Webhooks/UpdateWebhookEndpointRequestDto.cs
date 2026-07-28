// ABOUTME: Grouped PATCH contract for webhook endpoint configuration.
// ABOUTME: Keeps concurrency and pending-work governance atomic with supplied configuration groups.

namespace Explore.Application.DTOs.Webhooks;

public sealed class UpdateWebhookEndpointRequestDto
{
    public UpdateWebhookEndpointDestinationDto? Destination { get; init; }
    public UpdateWebhookEndpointSubscriptionsDto? Subscriptions { get; init; }
    public UpdateWebhookEndpointDeliveryPolicyDto? DeliveryPolicy { get; init; }
    public required UpdateWebhookEndpointGovernanceDto Governance { get; init; }
}

public sealed class UpdateWebhookEndpointDestinationDto
{
    public required string Url { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateWebhookEndpointSubscriptionsDto
{
    public IReadOnlyList<Guid> EventTypeIds { get; init; } = [];
}

public sealed class UpdateWebhookEndpointDeliveryPolicyDto
{
    public int? MaxAttempts { get; init; }
    public int? TimeoutSeconds { get; init; }
    public int? RateLimitPerMinute { get; init; }
}

public sealed class UpdateWebhookEndpointGovernanceDto
{
    public int ExpectedConfigurationVersion { get; init; }
    public int PendingWorkDecisionId { get; init; }
    public required string PendingWorkReason { get; init; }
    public bool AcknowledgeUncertainProviderPublications { get; init; }
}
