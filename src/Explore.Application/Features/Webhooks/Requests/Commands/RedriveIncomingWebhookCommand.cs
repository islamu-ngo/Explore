// ABOUTME: Authorized command for redriving one dead-lettered incoming webhook processing generation.
// ABOUTME: Carries tenant, inbox identity, expected generation, and a bounded operator reason.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.RedriveIncoming)]
public sealed record RedriveIncomingWebhookCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid IncomingWebhookMessageId { get; init; }

    public int ExpectedProcessingGeneration { get; init; }

    public string Reason { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => IncomingWebhookMessageId == Guid.Empty
        ? null
        : IncomingWebhookMessageId.ToString("D");

    IAuthorizationFacts? ISecureRequest.AuthorizationFacts =>
        TenantId == Guid.Empty
        ? null
        : new TenantScopedAuthorizationFacts(TenantId);
}
