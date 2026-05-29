// ABOUTME: Lookup entity describing how much notification traffic a subscription should produce.
// ABOUTME: V1 uses ALL for active subscriptions while retaining NONE and PERSONALIZED for durable policy state.

namespace Explore.Domain;

public class ActorSubscriptionNotificationLevel
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
