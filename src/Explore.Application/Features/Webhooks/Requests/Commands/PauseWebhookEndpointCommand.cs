// ABOUTME: Authorized command for manually pausing an active Local webhook endpoint.
// ABOUTME: Carries endpoint, actor, and normalized audit-reason evidence into owner-aware CQRS.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Pause)]
public sealed class PauseWebhookEndpointCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid EndpointId { get; init; }

    public Guid ActorUserId { get; init; }

    public long ExpectedDeliveryStateVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => EndpointId == Guid.Empty ? null : EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => EndpointId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["endpointId"] = EndpointId.ToString("D"),
            ["webhookOperation"] = "pause"
        };

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.Endpoint;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => EndpointId;
}
