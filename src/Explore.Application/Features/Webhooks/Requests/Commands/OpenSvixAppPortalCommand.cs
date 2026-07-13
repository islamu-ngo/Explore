// ABOUTME: Authorized command for creating Svix App Portal access for one verified tenant consumer.
// ABOUTME: Supplies resource attributes without accepting caller-selected portal capabilities.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.OpenProviderPortal)]
public sealed class OpenSvixAppPortalCommand : IRequest<WebhookProviderPortalAccessCommandResponse>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid ConsumerId { get; init; }

    public string SessionId { get; init; } = string.Empty;

    public int? ExpiresInSeconds { get; init; }

    string? ISecureRequest.ResourceId => ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>
            {
                ["tenantId"] = TenantId.ToString("D"),
                ["provider"] = "svix"
            };

            attributes["consumerId"] = ConsumerId.ToString("D");

            return attributes;
        }
    }
}
