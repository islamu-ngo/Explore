// ABOUTME: Wrapper DTO for PATCH-based LocationRoom updates using nullable per-property groups.
// ABOUTME: Route ID targets the row while groups express independent room field update intent.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.LocationRoom;

public class UpdateLocationRoomDto
{
    public UpdateLocationRoomLocationDto? Location { get; set; }
    public UpdateLocationRoomNameDto? Name { get; set; }
    public UpdateLocationRoomSlugDto? Slug { get; set; }
    public UpdateLocationRoomDescriptionDto? Description { get; set; }
    public UpdateLocationRoomCapacityDto? Capacity { get; set; }
    public UpdateLocationRoomSortOrderDto? SortOrder { get; set; }
}

public class UpdateLocationRoomLocationDto
{
    public Guid LocationId { get; set; }
}

public class UpdateLocationRoomNameDto
{
    public required string Value { get; set; }
}

public class UpdateLocationRoomSlugDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateLocationRoomDescriptionDto
{
    public OptionalUpdate<string?> Value { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public class UpdateLocationRoomCapacityDto
{
    public OptionalUpdate<int?> Value { get; set; } = OptionalUpdate<int?>.Unspecified();
}

public class UpdateLocationRoomSortOrderDto
{
    public int Value { get; set; }
}
