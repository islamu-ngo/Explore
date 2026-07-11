// ABOUTME: Enum companion for the NotificationReason lookup entity.
// ABOUTME: Values match the seeded IDs in the notification_reasons table.

namespace Explore.Domain.Enums;

public enum NotificationReasonEnum
{
    Direct = 1,
    Mention = 2,
    Assignment = 3,
    Subscription = 4,
    Membership = 5,
    System = 6
}
