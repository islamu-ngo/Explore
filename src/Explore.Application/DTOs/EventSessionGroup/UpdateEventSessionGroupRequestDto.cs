// ABOUTME: Request contract for updating a program section, track, devroom, or stage within an event.
// ABOUTME: Keeps TenantId server-owned while carrying EventId for same-event validation and authorization context.

namespace Explore.Application.DTOs.EventSessionGroup;

public class UpdateEventSessionGroupRequestDto
{
    public Guid Id { get; set; }
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
