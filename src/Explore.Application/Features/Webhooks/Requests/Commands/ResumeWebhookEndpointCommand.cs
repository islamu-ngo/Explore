// ABOUTME: Authorized command for resuming a Local webhook endpoint after manual or automatic pause.
// ABOUTME: Carries persisted endpoint identity into the owner-aware authorization pipeline.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Resume)]
public sealed class ResumeWebhookEndpointCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid EndpointId { get; init; }

    public Guid ActorUserId { get; init; }

    public long ExpectedDeliveryStateVersion { get; init; }

    public string ReasonCode { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => EndpointId == Guid.Empty ? null : EndpointId.ToString("D");

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.Endpoint;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => EndpointId;
}
