// ABOUTME: Authorized query for tenant-scoped webhook consumer management rows.
// ABOUTME: Keeps read access tenant-bound while handlers own entity-to-DTO mapping.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.View)]
public sealed class GetWebhookConsumersQuery : IRequest<IReadOnlyList<WebhookConsumerDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
