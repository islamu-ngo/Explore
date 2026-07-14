// ABOUTME: Request DTO for rotating a tenant-scoped outgoing webhook endpoint signing secret reference.
// ABOUTME: Accepts secret references only so raw webhook signing material never crosses the public API contract.

namespace Explore.Application.DTOs.Webhooks;

public sealed class RotateWebhookEndpointSecretRequestDto
{
    public required string NewSecretRef { get; init; }

    public int? PreviousSecretValidForSeconds { get; init; }

    public int ExpectedConfigurationVersion { get; init; }

    public int PendingWorkDecisionId { get; init; }

    public required string PendingWorkReason { get; init; }

    public bool AcknowledgeUncertainProviderPublications { get; init; }
}
