// ABOUTME: Authorized query for recent tenant-scoped webhook bulk replay operations.
// ABOUTME: Returns bounded normalized operation metadata for management polling and history.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.BulkReplay)]
public sealed class GetWebhookBulkReplayOperationsQuery : IRequest<IReadOnlyList<WebhookBulkReplayOperationDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
