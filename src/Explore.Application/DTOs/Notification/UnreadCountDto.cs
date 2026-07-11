// ABOUTME: Simple DTO wrapping the unread notification count for a user.
// ABOUTME: Used in GET /api/notification/unread-count response.

namespace Explore.Application.DTOs.Notification;

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}
