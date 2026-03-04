// ABOUTME: Enum companion for the NotificationEntityType lookup entity.
// ABOUTME: Values match the seeded IDs in the notification_entity_types table.

namespace Explore.Domain.Enums;

public enum NotificationEntityTypeEnum
{
    Event = 1,
    Organization = 2,
    Group = 3,
    EventRegistration = 4,
    EventSession = 5,
    User = 6
}
