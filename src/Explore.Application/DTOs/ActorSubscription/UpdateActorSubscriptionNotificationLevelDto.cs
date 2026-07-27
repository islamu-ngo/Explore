// ABOUTME: Route-ID PATCH payload for updating an actor subscription notification level.
// ABOUTME: Uses a nullable property group and concurrency stamp so omitted state is never replaced.

using System.ComponentModel.DataAnnotations;

namespace Explore.Application.DTOs.ActorSubscription;

public class UpdateActorSubscriptionNotificationLevelDto
{
    public UpdateActorSubscriptionNotificationLevelValueDto? NotificationLevel { get; set; }

    [Required]
    public required Guid ExpectedConcurrencyStamp { get; set; }
}

public class UpdateActorSubscriptionNotificationLevelValueDto
{
    [Required]
    public required int Id { get; set; }
}
