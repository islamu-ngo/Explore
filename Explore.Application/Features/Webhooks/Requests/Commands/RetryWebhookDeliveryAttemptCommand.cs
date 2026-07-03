// ABOUTME: Authorized command for scheduling a manual retry of a failed LocalProvider webhook attempt.
// ABOUTME: Delegates retry eligibility and new attempt creation to the delivery drain service boundary.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Retry)]
public sealed class RetryWebhookDeliveryAttemptCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
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
