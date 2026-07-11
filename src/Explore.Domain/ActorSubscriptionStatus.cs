// ABOUTME: Lookup entity describing the lifecycle state of an actor subscription.
// ABOUTME: Used by ActorSubscription to preserve unsubscribe history as durable state transitions.

namespace Explore.Domain;

public class ActorSubscriptionStatus
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
