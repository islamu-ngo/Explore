// ABOUTME: Authorized query for one LocalProvider webhook delivery attempt audit row.
// ABOUTME: Keeps attempt detail reads tenant-bound for HAL retry affordance generation.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookDeliveryAttemptByIdQuery : IRequest<WebhookDeliveryAttemptDto?>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid AttemptId { get; init; }

    string? ISecureRequest.ResourceId => AttemptId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["attemptId"] = AttemptId.ToString("D")
    };
}
