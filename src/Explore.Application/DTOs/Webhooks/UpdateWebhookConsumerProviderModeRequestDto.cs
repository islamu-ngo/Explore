// ABOUTME: Request DTO for changing a webhook consumer's provider mode under optimistic concurrency.
// ABOUTME: Requires an explicit pending-work decision, operator reason, and uncertainty acknowledgement.

namespace Explore.Application.DTOs.Webhooks;

public sealed class UpdateWebhookConsumerProviderModeRequestDto
{
    public int ProviderModeId { get; init; }

    public int ExpectedConfigurationVersion { get; init; }

    public int PendingWorkDecisionId { get; init; }

    public required string PendingWorkReason { get; init; }

    public bool AcknowledgeUncertainProviderPublications { get; init; }
}
