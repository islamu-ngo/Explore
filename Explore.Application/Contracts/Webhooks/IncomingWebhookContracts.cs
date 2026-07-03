// ABOUTME: Provider-neutral incoming webhook callback contract models.
// ABOUTME: Separates signed provider callbacks into verification and idempotent processing boundaries.

namespace Explore.Application.Contracts.Webhooks;

public sealed record IncomingWebhookContext(
    string Provider,
    string RawPayload,
    IReadOnlyDictionary<string, string> Headers,
    DateTimeOffset ReceivedAt);

public sealed record IncomingWebhookVerificationResult(
    bool IsVerified,
    string? ProviderMessageId,
    string? EventType,
    string? IdempotencyKey,
    string? FailureCategory,
    string? SafeDetail)
{
    public static IncomingWebhookVerificationResult Verified(
        string providerMessageId,
        string? eventType,
        string idempotencyKey) =>
        new(true, providerMessageId, eventType, idempotencyKey, null, null);

    public static IncomingWebhookVerificationResult Rejected(
        string failureCategory,
        string? safeDetail = null) =>
        new(false, null, null, null, failureCategory, safeDetail);
}

public sealed record IncomingWebhookProcessingMessage(
    Guid Id,
    Guid? TenantId,
    string Provider,
    string ProviderMessageId,
    string IdempotencyKey,
    string? EventType,
    string RawPayloadHash,
    DateTimeOffset ReceivedAt);

public interface IIncomingWebhookVerifier
{
    string Provider { get; }

    Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken);
}

public interface IIncomingWebhookHandler
{
    Task<bool> HandleAsync(
        IncomingWebhookProcessingMessage message,
        CancellationToken cancellationToken);
}
