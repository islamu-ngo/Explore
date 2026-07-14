// ABOUTME: Authorized command for verifying or rebinding one consumer provider application.
// ABOUTME: Uses the persisted tenant and consumer as authority while treating provider identity as untrusted input.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ManageProvider)]
public sealed class RepairWebhookProviderBindingCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid ConsumerId { get; init; }

    public string ExternalApplicationId { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => ConsumerId == Guid.Empty
        ? null
        : ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["consumerId"] = ConsumerId.ToString("D"),
            ["provider"] = "svix",
            ["webhookOperation"] = "repair-provider-binding"
        };
}
