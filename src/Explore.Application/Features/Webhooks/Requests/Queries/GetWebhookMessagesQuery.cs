// ABOUTME: Authorized query for webhook delivery messages belonging to one canonical typed owner.
// ABOUTME: Preserves source-tenant evidence while authorization and reads use configuration ownership.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookMessagesQuery
    : IRequest<IReadOnlyList<WebhookMessageDto>>, ISecureRequest, IWebhookOwnerScopedRequest
{
    public int OwnerKindId { get; init; }

    public Guid? OwnerId { get; init; }

    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => OwnerId?.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["ownerKindId"] = OwnerKindId,
        ["ownerId"] = OwnerId?.ToString("D") ?? string.Empty
    };
}
