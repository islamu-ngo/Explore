// ABOUTME: DTO for creating a new room under a location.
// ABOUTME: LocationId scopes the room to a parent location; Name is required.

namespace Explore.Application.DTOs.LocationRoom;

public sealed record CreateLocationRoomDto
{
    public Guid LocationId { get; init; }
    public required string Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int? Capacity { get; init; }
    public int SortOrder { get; init; }
}
