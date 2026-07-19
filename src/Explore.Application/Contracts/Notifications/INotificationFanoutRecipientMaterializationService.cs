// ABOUTME: Application boundary for materializing one immutable fanout occurrence for one recipient.
// ABOUTME: Allows the page processor to own lease and checkpoint ordering around atomic recipient work.

using Explore.Domain;

namespace Explore.Application.Contracts.Notifications;

public interface INotificationFanoutRecipientMaterializationService
{
    Task<RecipientNotificationMaterializationResult> MaterializeAsync(
        NotificationFanoutOccurrence occurrence,
        Guid recipientUserId,
        CancellationToken cancellationToken = default);
}
