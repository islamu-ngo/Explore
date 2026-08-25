// ABOUTME: List read-model DTO for LocationRoom in collection responses.
// ABOUTME: Lightweight projection for room selectors and agenda column headers.

namespace Explore.Application.DTOs.LocationRoom;

public sealed record LocationRoomListDto
{
    public Guid Id { get; init; }
    public Guid LocationId { get; init; }
    public required string Name { get; init; }
    public int? Capacity { get; init; }
    public int SortOrder { get; init; }
    public Guid ConcurrencyStamp { get; init; }
}
