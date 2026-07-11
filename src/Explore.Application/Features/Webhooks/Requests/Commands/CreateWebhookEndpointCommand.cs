// ABOUTME: Authorized command for creating an outgoing webhook endpoint and subscriptions.
// ABOUTME: Carries endpoint configuration without exposing raw signing secret values.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Create)]
public sealed class CreateWebhookEndpointCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid ConsumerId { get; init; }

    public required string Url { get; init; }

    public string? Description { get; init; }

    public required string SecretRef { get; init; }

    public IReadOnlyList<Guid> EventTypeIds { get; init; } = [];

    public int? MaxAttempts { get; init; }

    public int? TimeoutSeconds { get; init; }

    public int? RateLimitPerMinute { get; init; }

    string? ISecureRequest.ResourceId => ConsumerId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["consumerId"] = ConsumerId.ToString("D")
    };
}
