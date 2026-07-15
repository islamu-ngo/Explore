// ABOUTME: Authorized query for one tenant-scoped webhook bulk replay operation.
// ABOUTME: Exposes normalized lifecycle and bounded scheduling evidence without delivery payloads.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.BulkReplay)]
public sealed class GetWebhookBulkReplayOperationQuery : IRequest<WebhookBulkReplayOperationDto?>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid OperationId { get; init; }

    string? ISecureRequest.ResourceId => OperationId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["bulkReplayOperationId"] = OperationId.ToString("D")
    };
}
