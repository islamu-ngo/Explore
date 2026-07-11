// ABOUTME: Authorized query for one tenant-scoped webhook endpoint management row.
// ABOUTME: Supplies endpoint and tenant attributes to the MediatR authorization pipeline.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.View)]
public sealed class GetWebhookEndpointByIdQuery : IRequest<WebhookEndpointDto?>, ISecureRequest
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
