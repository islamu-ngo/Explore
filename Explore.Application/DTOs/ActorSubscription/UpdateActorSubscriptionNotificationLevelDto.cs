// ABOUTME: Command payload for updating an actor subscription notification level.
// ABOUTME: Uses the current concurrency stamp to prevent stale preference writes.

namespace Explore.Application.DTOs.ActorSubscription;

public class UpdateActorSubscriptionNotificationLevelDto
{
    public Guid TargetActorId { get; set; }
    public int NotificationLevelId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
