// ABOUTME: Application result for retention-aware webhook payload reads.
// ABOUTME: Distinguishes available, tenant-safe not-found, and known-but-gone outcomes.

using Explore.Application.DTOs.Webhooks;

namespace Explore.Application.Responses;

public enum WebhookMessagePayloadReadStatus
{
    Available = 1,
    NotFound = 2,
    Gone = 3
}

public sealed record WebhookMessagePayloadReadResult(
    WebhookMessagePayloadReadStatus Status,
    WebhookMessagePayloadDto? Payload = null)
{
    public static WebhookMessagePayloadReadResult Available(WebhookMessagePayloadDto payload) =>
        new(WebhookMessagePayloadReadStatus.Available, payload);

    public static WebhookMessagePayloadReadResult NotFound() =>
        new(WebhookMessagePayloadReadStatus.NotFound);

    public static WebhookMessagePayloadReadResult Gone() =>
        new(WebhookMessagePayloadReadStatus.Gone);
}
