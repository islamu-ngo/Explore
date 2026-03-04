// ABOUTME: Enum companion for the NotificationType lookup entity.
// ABOUTME: Values match the seeded IDs in the notification_types table.

namespace Explore.Domain.Enums;

public enum NotificationTypeEnum
{
    RegistrationConfirmed = 1,
    ApprovalGranted = 2,
    ApprovalRejected = 3,
    WaitlistPromoted = 4,
    EventCreated = 5,
    EventUpdated = 6,
    EventCancelled = 7,
    MemberInvited = 8,
    MemberRemoved = 9,
    General = 10
}
