// ABOUTME: HATEOAS link policies for notification detail and collection views.
// ABOUTME: All notification links require authentication since notifications are personal data.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Notification;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for NotificationDto (detail view).
/// All links require authentication since notifications are personal data.
/// </summary>
public sealed class NotificationDetailLinkPolicy : ILinkPolicy<NotificationDto>
{
    public IEnumerable<LinkDefinition> GetLinks(NotificationDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetNotificationById,
            new { id = dto.Id },
            "GET",
            dto.Title,
            RequiresAuth: true);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetNotifications,
            null,
            "GET",
            "All notifications",
            RequiresAuth: true);

        // Mark as read (only if not already read)
        if (!dto.IsRead)
        {
            yield return new LinkDefinition(
                "mark-read",
                RouteNames.MarkNotificationAsRead,
                new { id = dto.Id },
                "PATCH",
                "Mark as read",
                RequiresAuth: true);
        }

        // Related entity link
        if (dto.NotificationEntityTypeId.HasValue && !string.IsNullOrEmpty(dto.EntityId))
        {
            yield return new LinkDefinition(
                "related-entity",
                RouteNames.GetNotificationById,
                new { id = dto.Id },
                "GET",
                $"{dto.NotificationEntityTypeName}: {dto.EntityId}",
                RequiresAuth: true);
        }

        // Delete link
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteNotification,
            new { id = dto.Id },
            "DELETE",
            "Delete notification",
            RequiresAuth: true);
    }
}

/// <summary>
/// Link policy for NotificationListDto in collection context.
/// </summary>
public sealed class NotificationCollectionLinkPolicy : ICollectionLinkPolicy<NotificationListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(NotificationListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetNotificationById,
            new { id = dto.Id },
            "GET",
            dto.Title,
            RequiresAuth: true);

        // Mark as read (only if not already read)
        if (!dto.IsRead)
        {
            yield return new LinkDefinition(
                "mark-read",
                RouteNames.MarkNotificationAsRead,
                new { id = dto.Id },
                "PATCH",
                "Mark as read",
                RequiresAuth: true);
        }
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Mark all as read
        yield return new LinkDefinition(
            "mark-all-read",
            RouteNames.MarkAllNotificationsAsRead,
            null,
            "POST",
            "Mark all notifications as read",
            RequiresAuth: true);

        // Unread count
        yield return new LinkDefinition(
            "unread-count",
            RouteNames.GetUnreadNotificationCount,
            null,
            "GET",
            "Unread notification count",
            RequiresAuth: true);
    }
}
