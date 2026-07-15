// ABOUTME: API request for resolving a manual provider publication with exact provider evidence.
// ABOUTME: Carries optimistic concurrency, provider message identity, and a normalized audit reason.

namespace Explore.Application.DTOs.Webhooks;

public sealed class ReconcileWebhookProviderPublicationRequestDto
{
    public long ExpectedConcurrencyVersion { get; set; }
    public string ExternalProviderMessageId { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
}
