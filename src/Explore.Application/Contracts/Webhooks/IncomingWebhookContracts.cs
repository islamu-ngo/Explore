// ABOUTME: Provider-neutral incoming webhook verification and processing contracts.
// ABOUTME: Requires processors to use persisted claim identity and declare one stable receipt effect kind.

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

public sealed record IncomingWebhookProcessingResult(
    IncomingWebhookProcessingOutcome Outcome,
    string? FailureCategory = null,
    string? SafeDetail = null)
{
    public static IncomingWebhookProcessingResult Processed() =>
        new(IncomingWebhookProcessingOutcome.Processed);

    public static IncomingWebhookProcessingResult Ignored(string reasonCode, string? safeDetail = null) =>
        new(IncomingWebhookProcessingOutcome.Ignored, reasonCode, safeDetail);

    public static IncomingWebhookProcessingResult RejectedPermanent(string failureCategory, string? safeDetail = null) =>
        new(IncomingWebhookProcessingOutcome.RejectedPermanent, failureCategory, safeDetail);

    public static IncomingWebhookProcessingResult RetryDue(string failureCategory, string? safeDetail = null) =>
        new(IncomingWebhookProcessingOutcome.RetryDue, failureCategory, safeDetail);

    public static IncomingWebhookProcessingResult DeadLettered(string failureCategory, string? safeDetail = null) =>
        new(IncomingWebhookProcessingOutcome.DeadLettered, failureCategory, safeDetail);
}

public enum IncomingWebhookProcessingOutcome
{
    Processed = 1,
    Ignored = 2,
    RejectedPermanent = 3,
    RetryDue = 4,
    DeadLettered = 5
}

public interface IIncomingWebhookVerifier
{
    string Provider { get; }

    Task<IncomingWebhookVerificationResult> VerifyAsync(
        IncomingWebhookContext context,
        CancellationToken cancellationToken);
}

public interface IIncomingWebhookHandler
{
    string EffectKind { get; }

    Task<IncomingWebhookProcessingResult> HandleAsync(
        IncomingWebhookProcessingContext context,
        CancellationToken cancellationToken);
}
