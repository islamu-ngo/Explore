// ABOUTME: Authorized command for resuming a Local webhook endpoint after automatic circuit pause.
// ABOUTME: Carries tenant and endpoint identity into the resource authorization pipeline.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Resume)]
public sealed class ResumeWebhookEndpointCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid EndpointId { get; init; }

    public Guid ActorUserId { get; init; }

    string? ISecureRequest.ResourceId => EndpointId == Guid.Empty ? null : EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => EndpointId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["endpointId"] = EndpointId.ToString("D"),
            ["webhookOperation"] = "resume"
        };
}
