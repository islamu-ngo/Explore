// ABOUTME: Authorized query for one persisted-owner webhook message audit row.
// ABOUTME: Uses webhook delivery authorization and omits raw payload data from the response DTO.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookMessageByIdQuery
    : IRequest<WebhookMessageDto?>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid MessageId { get; init; }

    string? ISecureRequest.ResourceId => MessageId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["messageId"] = MessageId.ToString("D")
    };

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.Message;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => MessageId;
}
