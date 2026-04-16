// ABOUTME: DTO for creating a new room under a location.
// ABOUTME: LocationId scopes the room to a parent location; Name is required.

namespace Explore.Application.DTOs.LocationRoom;

public class CreateLocationRoomDto
{
    public Guid LocationId { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}
