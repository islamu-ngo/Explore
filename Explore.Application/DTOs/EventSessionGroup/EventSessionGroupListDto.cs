// ABOUTME: Lightweight list DTO for event program sections/tracks/devrooms.
// ABOUTME: Used by program summary and event-scoped group picker surfaces.

namespace Explore.Application.DTOs.EventSessionGroup;

public class EventSessionGroupListDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public Guid TenantId { get; set; }
}
