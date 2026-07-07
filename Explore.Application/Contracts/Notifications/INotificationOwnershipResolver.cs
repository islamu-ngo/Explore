// ABOUTME: Contract for resolving which system owns a notification decision.
// ABOUTME: Keeps ownership policy in Application without depending on delivery infrastructure.

using Explore.Application.Notifications;

namespace Explore.Application.Contracts.Notifications;

public interface INotificationOwnershipResolver
{
    Task<NotificationOwnershipDecision> ResolveAsync(
        NotificationIntentDraft draft,
        CancellationToken cancellationToken = default);
}
