// ABOUTME: Provider-neutral webhook contract models shared by application services and provider adapters.
// ABOUTME: Defines envelopes, delivery messages, endpoint requests, and result shapes without infrastructure dependencies.

namespace Explore.Application.Contracts.Webhooks;

public sealed record WebhookEventEnvelope(
    Guid Id,
    string Type,
    int Version,
    DateTimeOffset OccurredAt,
    Guid TenantId,
    IReadOnlyDictionary<string, object?> Data);

public sealed record WebhookEventBuildContext(
    Guid MessageId,
    Guid TenantId,
    string EventType,
    string EventId,
    string AggregateKind,
    Guid AggregateId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, object?> Data,
    Guid? ConsumerId = null);

public sealed record WebhookPayloadBuildResult(
    bool Succeeded,
    WebhookEventEnvelope? Envelope,
    byte[]? PayloadBytes,
    string? PayloadHash,
    DateTimeOffset? PayloadRetentionUntil,
    string? FailureCategory,
    string? SafeDetail)
{
    public static WebhookPayloadBuildResult Success(
        WebhookEventEnvelope envelope,
        byte[] payloadBytes,
        string payloadHash,
        DateTimeOffset payloadRetentionUntil) =>
        new(true, envelope, payloadBytes.ToArray(), payloadHash, payloadRetentionUntil, null, null);

    public static WebhookPayloadBuildResult Failure(string failureCategory, string? safeDetail = null) =>
        new(false, null, null, null, null, failureCategory, safeDetail);
}

public sealed record WebhookEventPublishResult(
    bool Succeeded,
    bool Skipped,
    Guid? MessageId,
    string? ProviderMessageId,
    bool IsRetryable,
    string? FailureCategory,
    string? SafeDetail)
{
    public static WebhookEventPublishResult Success(Guid messageId, string? providerMessageId = null) =>
        new(true, false, messageId, providerMessageId, false, null, null);

    public static WebhookEventPublishResult SkippedResult(string failureCategory, string? safeDetail = null) =>
        new(true, true, null, null, false, failureCategory, safeDetail);

    public static WebhookEventPublishResult Failure(
        string failureCategory,
        bool isRetryable,
        string? safeDetail = null) =>
        new(false, false, null, null, isRetryable, failureCategory, safeDetail);
}

public sealed record CreateWebhookEndpointInput(
    Guid TenantId,
    Guid ConsumerId,
    Uri Url,
    string? Description,
    IReadOnlyCollection<string> EventTypes,
    int? MaxAttempts = null,
    int? TimeoutSeconds = null,
    int? RateLimitPerMinute = null);

public sealed record UpdateWebhookEndpointInput(
    Guid EndpointId,
    Guid TenantId,
    Uri Url,
    string? Description,
    IReadOnlyCollection<string> EventTypes,
    int? MaxAttempts = null,
    int? TimeoutSeconds = null,
    int? RateLimitPerMinute = null);

public sealed record WebhookEndpointResult(
    Guid EndpointId,
    Guid TenantId,
    Guid ConsumerId,
    Uri Url,
    string Status,
    int SecretVersion,
    IReadOnlyCollection<string> EventTypes);
