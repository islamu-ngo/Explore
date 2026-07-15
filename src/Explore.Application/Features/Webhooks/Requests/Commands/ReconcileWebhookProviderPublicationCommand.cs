// ABOUTME: Authorized command for resolving a manual provider publication from exact operator evidence.
// ABOUTME: Requires tenant identity, optimistic version, provider message id, actor, and audit reason.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ReconcilePublication)]
public sealed class ReconcileWebhookProviderPublicationCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid PublicationId { get; init; }
    public Guid ActorUserId { get; init; }
    public long ExpectedConcurrencyVersion { get; init; }
    public string ExternalProviderMessageId { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => PublicationId == Guid.Empty ? null : PublicationId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => PublicationId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["publicationId"] = PublicationId.ToString("D"),
            ["webhookOperation"] = "reconcile-publication"
        };
}
