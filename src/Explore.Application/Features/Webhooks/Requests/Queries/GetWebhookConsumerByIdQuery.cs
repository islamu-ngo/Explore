// ABOUTME: Authorized query for one tenant-scoped webhook consumer management record.
// ABOUTME: Supplies consumer and tenant attributes to the MediatR authorization pipeline.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.View)]
public sealed class GetWebhookConsumerByIdQuery : IRequest<WebhookConsumerDto?>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid ConsumerId { get; init; }

    string? ISecureRequest.ResourceId => ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["consumerId"] = ConsumerId.ToString("D")
    };
}
