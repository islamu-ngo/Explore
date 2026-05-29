// ABOUTME: Command payload for unsubscribing the current tenant user from an actor.
// ABOUTME: Preserves the durable subscription row and uses concurrency to avoid stale writes.

namespace Explore.Application.DTOs.ActorSubscription;

public class UnsubscribeFromActorDto
{
    public Guid TargetActorId { get; set; }
    public Guid ExpectedConcurrencyStamp { get; set; }
}
