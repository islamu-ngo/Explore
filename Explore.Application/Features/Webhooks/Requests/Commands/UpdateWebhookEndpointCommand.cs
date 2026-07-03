// ABOUTME: Authorized command for updating an outgoing webhook endpoint and subscription set.
// ABOUTME: Uses tenant and endpoint identifiers as dynamic authorization metadata for webhook administration.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Update)]
public sealed class UpdateWebhookEndpointCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid EndpointId { get; init; }

    public required string Url { get; init; }

    public string? Description { get; init; }

    public IReadOnlyList<Guid> EventTypeIds { get; init; } = [];

    public int? MaxAttempts { get; init; }

    public int? TimeoutSeconds { get; init; }

    public int? RateLimitPerMinute { get; init; }

    string? ISecureRequest.ResourceId => EndpointId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D"),
        ["endpointId"] = EndpointId.ToString("D")
    };
}
