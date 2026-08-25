// ABOUTME: API request for resolving a manual provider publication with exact provider evidence.
// ABOUTME: Carries optimistic concurrency, provider message identity, and a normalized audit reason.

namespace Explore.Application.DTOs.Webhooks;

public sealed record ReconcileWebhookProviderPublicationRequestDto
{
    public long ExpectedConcurrencyVersion { get; init; }
    public string ExternalProviderMessageId { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = string.Empty;
}
