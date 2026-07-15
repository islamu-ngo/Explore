// ABOUTME: Authorized command for verifying or rebinding one consumer provider application.
// ABOUTME: Uses persisted typed ownership as authority while treating provider identity as untrusted input.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ManageProvider)]
public sealed class RepairWebhookProviderBindingCommand
    : IRequest<BaseCommandResponse<Guid>>, ISecureRequest, IWebhookPersistedOwnerRequest
{
    public Guid ConsumerId { get; init; }

    public string ExternalApplicationId { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => ConsumerId == Guid.Empty
        ? null
        : ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["consumerId"] = ConsumerId.ToString("D"),
        ["provider"] = "svix",
        ["webhookOperation"] = "repair-provider-binding"
    };

    WebhookOwnedResourceKind IWebhookPersistedOwnerRequest.OwnedResourceKind =>
        WebhookOwnedResourceKind.Consumer;

    Guid IWebhookPersistedOwnerRequest.OwnedResourceId => ConsumerId;
}
