// ABOUTME: API request for explicitly abandoning an operator-owned provider publication.
// ABOUTME: Carries optimistic concurrency evidence and a normalized audit reason without tenant authority.

namespace Explore.Application.DTOs.Webhooks;

public sealed class AbandonWebhookProviderPublicationRequestDto
{
    public long ExpectedConcurrencyVersion { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
}
