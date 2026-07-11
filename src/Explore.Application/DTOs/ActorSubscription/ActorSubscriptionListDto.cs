// ABOUTME: List DTO for paginated actor subscription collections.
// ABOUTME: Keeps subscription list responses compact while preserving HAL/action metadata inputs.

namespace Explore.Application.DTOs.ActorSubscription;

public class ActorSubscriptionListDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TargetActorId { get; set; }
    public int TargetActorTypeId { get; set; }
    public string? TargetActorTypeName { get; set; }
    public string? TargetActorName { get; set; }
    public int StatusId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusName { get; set; }
    public int NotificationLevelId { get; set; }
    public string? NotificationLevelCode { get; set; }
    public string? NotificationLevelName { get; set; }
    public DateTime SubscribedAt { get; set; }
    public DateTime? UnsubscribedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
