// ABOUTME: List DTO for notification collections (paginated listing).
// ABOUTME: Used in GET /api/notification response.

namespace Explore.Application.DTOs.Notification;

public class NotificationListDto
{
    public Guid Id { get; set; }
    public int NotificationTypeId { get; set; }
    public string? NotificationTypeName { get; set; }
    public required string Title { get; set; }
    public string? Body { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public int? NotificationEntityTypeId { get; set; }
    public string? NotificationEntityTypeName { get; set; }
    public string? EntityId { get; set; }
    public int NotificationScopeId { get; set; }
    public string? NotificationScopeName { get; set; }
    public Guid? SourceActorId { get; set; }
    public string? SourceActorName { get; set; }
    public Guid? RecipientContextActorId { get; set; }
    public string? RecipientContextActorName { get; set; }
    public DateTime CreatedAt { get; set; }
}
