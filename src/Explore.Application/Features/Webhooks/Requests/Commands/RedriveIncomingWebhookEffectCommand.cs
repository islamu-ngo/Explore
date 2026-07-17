// ABOUTME: Authorized command for redriving one dead-lettered incoming Coop effect pointer.
// ABOUTME: Carries tenant identity, expected generation, and a bounded operator reason.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Webhooks.Requests.Commands;

[AuthorizeResource(ResourceKinds.Webhook, AuthorizationActions.Webhooks.RedriveIncoming)]
public sealed class RedriveIncomingWebhookEffectCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid TenantId { get; init; }
    public Guid EffectOutboxId { get; init; }
    public int ExpectedProcessingGeneration { get; init; }
    public string Reason { get; init; } = string.Empty;

    string? ISecureRequest.ResourceId => EffectOutboxId == Guid.Empty
        ? null
        : EffectOutboxId.ToString("D");

    IDictionary<string, object>? ISecureRequest.ResourceAttributes => TenantId == Guid.Empty
        ? null
        : new Dictionary<string, object>
        {
            ["tenantId"] = TenantId.ToString("D"),
            ["effectOutboxId"] = EffectOutboxId.ToString("D"),
            ["expectedProcessingGeneration"] = ExpectedProcessingGeneration,
            ["webhookOperation"] = "redrive-incoming-effect"
        };
}
