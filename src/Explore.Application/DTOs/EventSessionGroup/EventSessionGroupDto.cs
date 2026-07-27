// ABOUTME: Detail read-model DTO for an event program section, track, devroom, or stage.
// ABOUTME: Exposes grouping metadata without leaking internal EventSessionGroup naming into UI copy.

using Explore.Application.DTOs.Location;

namespace Explore.Application.DTOs.EventSessionGroup;

public class EventSessionGroupDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }
    public EventLocationPublicDto? EventLocation { get; set; }
    public string? Color { get; set; }
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
