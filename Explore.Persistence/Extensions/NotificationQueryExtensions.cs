// ABOUTME: Centralized Include chain for Notification queries to eliminate duplication across NotificationRepository methods.
// ABOUTME: Only includes navigation properties — callers control tracking strategy (AsNoTracking).

using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Extensions;

internal static class NotificationQueryExtensions
{
    internal static IQueryable<Notification> IncludeStandardDetails(this IQueryable<Notification> query)
    {
        return query
            .Include(n => n.NotificationType)
            .Include(n => n.NotificationEntityType)
            .Include(n => n.NotificationScope)
            .Include(n => n.NotificationReason)
            .Include(n => n.SourceActor).ThenInclude(a => a!.Pii)
            .Include(n => n.RecipientContextActor).ThenInclude(a => a!.Pii);
    }
}
