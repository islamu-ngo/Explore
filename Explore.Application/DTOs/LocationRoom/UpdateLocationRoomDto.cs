// ABOUTME: DTO for updating an existing room under a location.
// ABOUTME: Id targets the row; LocationId validates ownership.

namespace Explore.Application.DTOs.LocationRoom;

public class UpdateLocationRoomDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}
