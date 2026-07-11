// ABOUTME: Detail read-model DTO for a single LocationRoom entity.
// ABOUTME: Includes parent location name for display context.

namespace Explore.Application.DTOs.LocationRoom;

public class LocationRoomDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string? LocationFullName { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
