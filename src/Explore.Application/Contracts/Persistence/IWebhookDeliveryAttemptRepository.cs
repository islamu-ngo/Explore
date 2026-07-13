// ABOUTME: Repository contract for LocalProvider delivery attempt rows.
// ABOUTME: Keeps HTTP attempt audit state entity-first and tenant-safe.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record WebhookDeliveryClaimLimits(
    int MaxInFlightPerTenant,
    int MaxInFlightPerEndpoint,
    int MaxItemsPerClaimCycle);

public sealed record WebhookDeliveryClaimRequest(
    int BatchSize,
    int CandidateBatchSize,
    int GlobalInFlightLimit,
    IReadOnlyList<Guid> TenantOrder,
    DateTime ClaimedAt,
    TimeSpan LeaseDuration,
    Guid? AttemptId = null);

public sealed record WebhookDeliveryClaim(
    WebhookDeliveryAttempt Attempt,
    Guid LeaseToken,
    DateTime ClaimedAt,
    DateTime LeaseExpiresAt);

public interface IWebhookDeliveryAttemptRepository
{
    Task<WebhookDeliveryAttempt> CreateAsync(
        WebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDeliveryAttempt>> CreateManyAsync(
        IReadOnlyCollection<WebhookDeliveryAttempt> attempts,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDeliveryAttempt>> GetByMessageAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDeliveryAttempt>> ListByTenantAsync(
        Guid tenantId,
        Guid? messageId,
        Guid? endpointId,
        int limit,
        CancellationToken cancellationToken);

    Task<int> GetNextAttemptNumberAsync(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveAttemptForEndpointAsync(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetDueTenantIdsAsync(
        int tenantLimit,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookDeliveryClaim>> ClaimDueAsync(
        WebhookDeliveryClaimRequest request,
        IReadOnlyDictionary<Guid, WebhookDeliveryClaimLimits> tenantLimits,
        CancellationToken cancellationToken);

    Task<int> CountDueScheduledAsync(
        DateTime now,
        CancellationToken cancellationToken);

    Task<int> CountStaleSendingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken);

    Task<WebhookDeliveryAttempt?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken);

    Task MarkSucceededAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        DateTime sentAt,
        DateTime completedAt,
        int httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        WebhookDeliveryAttemptStatus status,
        DateTime completedAt,
        string failureCategory,
        int? httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken);

    Task<int> ResetStaleSendingAsync(
        DateTime processingStartedBefore,
        DateTime recoveredAt,
        string failureCategory,
        int batchSize,
        CancellationToken cancellationToken);
}
