// ABOUTME: Query request for paginated user notifications with optional filters.
// ABOUTME: Supports filtering by read status and notification type ID.

using Explore.Application.DTOs.Notification;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Notifications.Requests.Queries;

public class GetUserNotificationsRequest : IRequest<PaginatedResult<NotificationListDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Optional filter: null = all, true = read only, false = unread only.
    /// </summary>
    public bool? IsRead { get; set; }

    /// <summary>
    /// Optional filter by notification type ID (lookup table FK).
    /// </summary>
    public int? NotificationTypeId { get; set; }

    /// <summary>
    /// Optional filter by notification scope (ActorType FK: User=1/Personal, Organization=2, Group=4, System=5).
    /// </summary>
    public int? NotificationScopeId { get; set; }
}
