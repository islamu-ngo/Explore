// ABOUTME: Authorized command for changing a tenant-scoped webhook consumer provider mode.
// ABOUTME: Carries optimistic concurrency and explicit pending-work governance metadata.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Update)]
public sealed class UpdateWebhookConsumerProviderModeCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid ConsumerId { get; init; }

    public int ProviderModeId { get; init; }

    public int ExpectedConfigurationVersion { get; init; }

    public int PendingWorkDecisionId { get; init; }

    public required string PendingWorkReason { get; init; }

    public bool AcknowledgeUncertainProviderPublications { get; init; }

    string? ISecureRequest.ResourceId => ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["consumerId"] = ConsumerId.ToString("D")
    };
}
