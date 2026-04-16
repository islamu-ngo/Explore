// ABOUTME: List read-model DTO for LocationRoom in collection responses.
// ABOUTME: Lightweight projection for room selectors and agenda column headers.

namespace Explore.Application.DTOs.LocationRoom;

public class LocationRoomListDto
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public required string Name { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}
