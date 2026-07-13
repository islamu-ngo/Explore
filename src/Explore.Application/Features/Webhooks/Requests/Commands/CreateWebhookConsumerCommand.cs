// ABOUTME: Authorized command for creating tenant-scoped outgoing webhook consumers.
// ABOUTME: Carries normalized request fields and tenant attributes for resource authorization.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.Create)]
public sealed class CreateWebhookConsumerCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid? OwnerActorId { get; init; }

    public Guid? OwnerUserId { get; init; }

    public int ConsumerKindId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int ProviderModeId { get; init; }

    string? ISecureRequest.ResourceId => TenantId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object>
    {
        ["tenantId"] = TenantId.ToString("D")
    };
}
