// ABOUTME: Authorized command for rotating an outgoing webhook endpoint signing secret reference.
// ABOUTME: Preserves dynamic tenant and endpoint metadata for webhook rotate-secret authorization checks.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.RotateSecret)]
public sealed class RotateWebhookEndpointSecretCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid EndpointId { get; init; }

    public required string NewSecretRef { get; init; }

    public int? PreviousSecretValidForSeconds { get; init; }

    public int ExpectedConfigurationVersion { get; init; }

    public int PendingWorkDecisionId { get; init; }

    public required string PendingWorkReason { get; init; }

    public bool AcknowledgeUncertainProviderPublications { get; init; }

    string? ISecureRequest.ResourceId => EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["endpointId"] = EndpointId.ToString("D")
    };
}
