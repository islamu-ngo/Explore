// ABOUTME: Authorized command for creating Svix App Portal access for the current tenant.
// ABOUTME: Supplies tenant-scoped webhook resource attributes to the MediatR authorization pipeline.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.OpenProviderPortal)]
public sealed class OpenSvixAppPortalCommand : IRequest<WebhookProviderPortalAccessCommandResponse>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid? ConsumerId { get; init; }

    public string SessionId { get; init; } = string.Empty;

    public bool ReadOnly { get; init; }

    public int? ExpiresInSeconds { get; init; }

    public IReadOnlyCollection<string> FeatureFlags { get; init; } = [];

    string? ISecureRequest.ResourceId => ConsumerId?.ToString("D") ?? TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object>
            {
                ["tenantId"] = TenantId.ToString("D"),
                ["provider"] = "svix"
            };

            if (ConsumerId is { } consumerId)
            {
                attributes["consumerId"] = consumerId.ToString("D");
            }

            return attributes;
        }
    }
}
