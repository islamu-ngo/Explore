// ABOUTME: Authorized command for redriving one dead-lettered incoming webhook processing generation.
// ABOUTME: Carries tenant, inbox identity, expected generation, and a bounded operator reason.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.RedriveIncoming)]
public sealed class RedriveIncomingWebhookCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }

    public Guid IncomingWebhookMessageId { get; init; }

    public int ExpectedProcessingGeneration { get; init; }

    public string Reason { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => IncomingWebhookMessageId == Guid.Empty
        ? null
        : IncomingWebhookMessageId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["incomingWebhookMessageId"] = IncomingWebhookMessageId.ToString("D"),
            ["expectedProcessingGeneration"] = ExpectedProcessingGeneration,
            ["webhookOperation"] = "redrive-incoming"
        };
}
