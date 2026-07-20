// ABOUTME: Defines transaction-bound suppression of linked recipient work for superseded fanout occurrences.
// ABOUTME: Hides stale in-app rows while keeping SMTP evidence immutable after the provider fence exists.

namespace Explore.Application.Contracts.Persistence;

public interface INotificationFanoutEmailSuppressionRepository
{
    Task<NotificationFanoutEmailSuppressionResult> SuppressPreHandoffAsync(
        Guid tenantId,
        Guid occurrenceId,
        DateTime suppressedAt,
        CancellationToken cancellationToken = default);
}

public sealed record NotificationFanoutEmailSuppressionResult(
    int OutboxRowsSkipped,
    int DeliveryRowsSuperseded,
    int NotificationsSuppressed = 0,
    int InAppDeliveryRowsSuperseded = 0);

public static class NotificationFanoutEmailSuppressionReason
{
    public const string Code = "fanout_occurrence_superseded";
    public const string ProviderStatus = "superseded";
    public const string Message = "Delivery was suppressed because a higher-priority event notification replaced it before provider handoff.";
}
