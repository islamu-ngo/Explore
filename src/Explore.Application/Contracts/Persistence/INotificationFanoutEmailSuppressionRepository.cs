// ABOUTME: Defines transaction-bound suppression of pre-handoff email work for superseded fanout occurrences.
// ABOUTME: Keeps transport evidence immutable once the canonical SMTP provider fence exists.

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
    int DeliveryRowsSuperseded);

public static class NotificationFanoutEmailSuppressionReason
{
    public const string Code = "fanout_occurrence_superseded";
    public const string ProviderStatus = "superseded";
    public const string Message = "Email delivery was suppressed because a newer event notification replaced it before provider handoff.";
}
