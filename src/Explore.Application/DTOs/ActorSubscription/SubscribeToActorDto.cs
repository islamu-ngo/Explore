// ABOUTME: Command payload for subscribing the current tenant user to an actor.
// ABOUTME: V1 accepts organization and group actor targets only.

namespace Explore.Application.DTOs.ActorSubscription;

public sealed record SubscribeToActorDto
{
    public Guid TargetActorId { get; init; }
}
