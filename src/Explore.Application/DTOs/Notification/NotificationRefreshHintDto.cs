// ABOUTME: Minimal server-sent notification refresh hint payload for browser inbox refresh.
// ABOUTME: Carries no notification body, actor details, or other high-cardinality entity data.

namespace Explore.Application.DTOs.Notification;

public sealed record NotificationRefreshHintDto
{
    public int UnreadCount { get; init; }

    public bool HasUnread { get; init; }

    public required string Reason { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
