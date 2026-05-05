// ABOUTME: Request contract for creating a program section, track, devroom, or stage within an event.
// ABOUTME: TenantId is intentionally omitted; handlers derive it from the parent event under tenant filters.

namespace Explore.Application.DTOs.EventSessionGroup;

public class CreateEventSessionGroupRequestDto
{
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? RoomId { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
}
