// ABOUTME: API request for explicitly abandoning an operator-owned provider publication.
// ABOUTME: Carries optimistic concurrency evidence and a normalized audit reason without tenant authority.

namespace Explore.Application.DTOs.Webhooks;

public sealed record AbandonWebhookProviderPublicationRequestDto
{
    public long ExpectedConcurrencyVersion { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}
