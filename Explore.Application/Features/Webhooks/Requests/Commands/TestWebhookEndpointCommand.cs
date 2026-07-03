// ABOUTME: Authorized command for scheduling a LocalProvider test delivery to one webhook endpoint.
// ABOUTME: Carries tenant and endpoint metadata for webhook test authorization checks.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Test)]
public sealed class TestWebhookEndpointCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid EndpointId { get; init; }

    string? ISecureRequest.ResourceId => EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["endpointId"] = EndpointId.ToString("D")
    };
}
