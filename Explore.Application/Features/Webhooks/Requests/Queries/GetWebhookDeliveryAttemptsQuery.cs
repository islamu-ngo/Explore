// ABOUTME: Authorized query for LocalProvider webhook delivery attempt audit rows.
// ABOUTME: Supports tenant, message, and endpoint filters for operations screens.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Webhooks;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Queries;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.ViewDelivery)]
public sealed class GetWebhookDeliveryAttemptsQuery : IRequest<IReadOnlyList<WebhookDeliveryAttemptDto>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid? MessageId { get; init; }

    public Guid? EndpointId { get; init; }

    public int Limit { get; init; } = 100;

    string? ISecureRequest.ResourceId => TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["messageId"] = MessageId?.ToString("D") ?? string.Empty,
        ["endpointId"] = EndpointId?.ToString("D") ?? string.Empty
    };
}
