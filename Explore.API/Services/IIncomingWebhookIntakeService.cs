// ABOUTME: API-layer intake boundary for verified incoming webhooks and idempotency capture.
// ABOUTME: Ensures provider callbacks are read raw, verified, and stored before side effects run.

namespace Explore.API.Services;

public interface IIncomingWebhookIntakeService
{
    Task<IncomingWebhookReadResult> ReadAndVerifyAsync(
        HttpRequest request,
        string provider,
        long maxBodyBytes,
        CancellationToken cancellationToken);

    Task<IncomingWebhookCaptureResult> CaptureAsync(
        IncomingWebhookReadResult readResult,
        Guid tenantId,
        string? providerMessageId,
        string? eventType,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task MarkRejectedAsync(
        Guid tenantId,
        Guid messageId,
        string failureCategory,
        string? safeDetail,
        CancellationToken cancellationToken);
}
