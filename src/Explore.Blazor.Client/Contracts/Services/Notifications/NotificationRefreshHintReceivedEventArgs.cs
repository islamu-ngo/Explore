// ABOUTME: Event payload raised when the notification refresh SSE transport receives an unread-count hint.
// ABOUTME: Keeps notification UI subscribers decoupled from the JS interop transport details.

namespace Explore.Blazor.Client.Contracts.Services.Notifications;

public sealed record NotificationRefreshHintReceivedEventArgs(
    int UnreadCount,
    bool HasUnread,
    string Reason,
    DateTimeOffset GeneratedAt);
