// ABOUTME: Route-ID PATCH payload for updating an actor subscription notification level.
// ABOUTME: Uses a nullable property group and concurrency stamp so omitted state is never replaced.

using System.ComponentModel.DataAnnotations;

namespace Explore.Application.DTOs.ActorSubscription;

public sealed record UpdateActorSubscriptionNotificationLevelDto
{
    public UpdateActorSubscriptionNotificationLevelValueDto? NotificationLevel { get; init; }

    [Required]
    public required Guid ExpectedConcurrencyStamp { get; init; }
}

public sealed record UpdateActorSubscriptionNotificationLevelValueDto
{
    [Required]
    public required int Id { get; init; }
}
