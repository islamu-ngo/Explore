// ABOUTME: Authorized command for creating Svix App Portal access for one verified typed-owner consumer.
// ABOUTME: Resolves persisted ownership without accepting caller-selected portal capabilities.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.OpenProviderPortal)]
public sealed class OpenSvixAppPortalCommand
    : IRequest<WebhookProviderPortalAccessCommandResponse>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid ConsumerId { get; init; }

    public string SessionId { get; init; } = string.Empty;

    public int? ExpiresInSeconds { get; init; }

    string? ISecureRequest.ResourceId => ConsumerId.ToString("D");

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.Consumer;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => ConsumerId;
}
