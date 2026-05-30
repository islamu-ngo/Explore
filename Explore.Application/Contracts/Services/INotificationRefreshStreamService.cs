// ABOUTME: Streams minimal notification refresh hints for SSE endpoints.
// ABOUTME: Keeps durable notifications and existing APIs as the source of truth.

using Explore.Application.DTOs.Notification;

namespace Explore.Application.Contracts.Services;

public interface INotificationRefreshStreamService
{
    IAsyncEnumerable<NotificationRefreshHintDto> StreamAsync(CancellationToken cancellationToken = default);
}
