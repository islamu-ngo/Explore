// ABOUTME: Client-side contract for one-way notification refresh hints over Server-Sent Events.
// ABOUTME: Keeps SSE transport behind a service so UI components retain polling fallback behavior.

namespace Explore.Blazor.Client.Contracts.Services.Notifications;

public interface INotificationRefreshStreamClient : IAsyncDisposable
{
    event Func<NotificationRefreshHintReceivedEventArgs, Task>? RefreshReceived;

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}

public sealed record NotificationRefreshHintReceivedEventArgs(
    int UnreadCount,
    bool HasUnread,
    string Reason,
    DateTimeOffset GeneratedAt);
