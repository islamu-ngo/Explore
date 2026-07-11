// ABOUTME: Authorized query for tenant-scoped webhook endpoint management rows.
// ABOUTME: Supports optional consumer filtering while keeping resource checks tenant-bound.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.View)]
public sealed class GetWebhookEndpointsQuery : IRequest<IReadOnlyList<WebhookEndpointDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid? ConsumerId { get; init; }

    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => ConsumerId?.ToString("D") ?? TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["consumerId"] = ConsumerId?.ToString("D") ?? string.Empty
    };
}
