// ABOUTME: Authorized query for one LocalProvider webhook delivery attempt audit row.
// ABOUTME: Resolves persisted configuration ownership for HAL retry affordance generation.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookDeliveryAttemptByIdQuery
    : IRequest<WebhookDeliveryAttemptDto?>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid AttemptId { get; init; }

    string? ISecureRequest.ResourceId => AttemptId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["attemptId"] = AttemptId.ToString("D")
    };

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.DeliveryAttempt;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => AttemptId;
}
