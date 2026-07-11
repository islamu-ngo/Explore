// ABOUTME: Authorized query for one tenant-scoped webhook message audit row.
// ABOUTME: Uses webhook delivery authorization and omits raw payload data from the response DTO.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookMessageByIdQuery : IRequest<WebhookMessageDto?>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid MessageId { get; init; }

    string? ISecureRequest.ResourceId => MessageId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["messageId"] = MessageId.ToString("D")
    };
}
