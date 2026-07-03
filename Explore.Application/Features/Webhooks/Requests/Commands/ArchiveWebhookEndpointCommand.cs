// ABOUTME: Authorized command for archiving an outgoing webhook endpoint.
// ABOUTME: Keeps endpoint history available while removing it from active endpoint management lists.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Delete)]
public sealed class ArchiveWebhookEndpointCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid EndpointId { get; init; }

    string? ISecureRequest.ResourceId => EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["endpointId"] = EndpointId.ToString("D")
    };
}
