// ABOUTME: Contract for notification operations consumed by Blazor UI components.
// ABOUTME: Wraps NSwag-generated IEventApiClient notification methods with clean async API.

using Explore.Blazor.Client.Models;

namespace Explore.Blazor.Client.Contracts.Services.Notifications;

public interface INotificationService
{
    Task<PaginatedResult<Clients.NotificationListDto>> GetNotificationsAsync(
        int pageNumber,
        int pageSize,
        bool? isRead = null,
        int? notificationScopeId = null,
        int? notificationReasonId = null,
        bool? isArchived = null,
        bool? isSnoozed = null);

    Task<Clients.NotificationDto?> GetNotificationByIdAsync(Guid id);

    Task<int> GetUnreadCountAsync(int? notificationScopeId = null);

    Task<bool> MarkAsReadAsync(Guid notificationId);

    Task<bool> MarkAllAsReadAsync();

    Task<bool> DeleteAsync(Guid notificationId);

    Task<bool> ArchiveAsync(Guid notificationId, bool archive = true);

    Task<bool> SnoozeAsync(Guid notificationId, DateTimeOffset snoozedUntil);
}
