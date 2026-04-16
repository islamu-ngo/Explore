// ABOUTME: Notification service wrapping NSwag-generated IEventApiClient notification methods.
// ABOUTME: Follows EventRegistrationService pattern: try-catch with logging, ApiException handling.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Constants;
using Explore.Blazor.Client.Contracts.Services.Notifications;
using Explore.Blazor.Client.Models;
using Microsoft.Extensions.Logging;

namespace Explore.Blazor.Client.Services;

public class NotificationService : INotificationService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IEventApiClient apiClient, ILogger<NotificationService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaginatedResult<NotificationListDto>> GetNotificationsAsync(
        int pageNumber,
        int pageSize,
        bool? isRead = null,
        int? notificationScopeId = null,
        int? notificationReasonId = null,
        bool? isArchived = null,
        bool? isSnoozed = null)
    {
        try
        {
            var response = await _apiClient.GetNotificationsAsync(
                pageNumber: pageNumber,
                pageSize: pageSize,
                isRead: isRead,
                notificationTypeId: null,
                notificationScopeId: notificationScopeId,
                notificationReasonId: notificationReasonId,
                isArchived: isArchived,
                isSnoozed: isSnoozed);

            return new PaginatedResult<NotificationListDto>
            {
                Items = response.Items?.ToList() ?? [],
                PageNumber = response.PageNumber ?? pageNumber,
                PageSize = response.PageSize ?? pageSize,
                TotalCount = response.TotalCount ?? 0
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error fetching notifications: {StatusCode}", ex.StatusCode);
            return PaginatedResult<NotificationListDto>.Empty(pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error fetching notifications");
            return PaginatedResult<NotificationListDto>.Empty(pageNumber, pageSize);
        }
    }

    public async Task<NotificationDto?> GetNotificationByIdAsync(Guid id)
    {
        try
        {
            return await _apiClient.GetNotificationByIdAsync(id);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            _logger.LogWarning("[NOTIFICATION SERVICE] Notification not found: {Id}", id);
            return null;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error fetching notification {Id}: {StatusCode}", id, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error fetching notification {Id}", id);
            return null;
        }
    }

    public async Task<int> GetUnreadCountAsync(int? notificationScopeId = null)
    {
        try
        {
            var response = await _apiClient.GetUnreadNotificationCountAsync(notificationScopeId);
            return response.UnreadCount ?? 0;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error fetching unread count: {StatusCode}", ex.StatusCode);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error fetching unread count");
            return 0;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId)
    {
        try
        {
            var response = await _apiClient.MarkNotificationAsReadAsync(notificationId);
            return response.Success ?? false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error marking notification as read: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error marking notification as read");
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        try
        {
            var response = await _apiClient.MarkAllNotificationsAsReadAsync();
            return response.Success ?? false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error marking all as read: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error marking all as read");
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid notificationId)
    {
        try
        {
            await _apiClient.DeleteNotificationAsync(notificationId);
            return true;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error deleting notification: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error deleting notification");
            return false;
        }
    }

    public async Task<bool> ArchiveAsync(Guid notificationId, bool archive = true)
    {
        try
        {
            var response = await _apiClient.ArchiveNotificationAsync(notificationId, archive);
            return response.Success ?? false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error archiving notification: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error archiving notification");
            return false;
        }
    }

    public async Task<bool> SnoozeAsync(Guid notificationId, DateTimeOffset snoozedUntil)
    {
        try
        {
            var response = await _apiClient.SnoozeNotificationAsync(notificationId, snoozedUntil);
            return response.Success ?? false;
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] API error snoozing notification: {StatusCode}", ex.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[NOTIFICATION SERVICE] Error snoozing notification");
            return false;
        }
    }
}
