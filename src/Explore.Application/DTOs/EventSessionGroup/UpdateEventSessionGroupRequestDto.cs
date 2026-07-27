// ABOUTME: Grouped PATCH contract for a program section, track, devroom, or stage.
// ABOUTME: Keeps identity and tenant ownership server-owned while preserving omitted values.

using Explore.Application.Models.Common;

namespace Explore.Application.DTOs.EventSessionGroup;

public class UpdateEventSessionGroupRequestDto
{
    public UpdateEventSessionGroupMetadataDto? Metadata { get; set; }
    public UpdateEventSessionGroupPlacementDto? Placement { get; set; }
    public UpdateEventSessionGroupOrderingDto? Ordering { get; set; }
    public UpdateEventSessionGroupPublicationDto? Publication { get; set; }
}

public sealed class UpdateEventSessionGroupMetadataDto
{
    public string? Name { get; set; }
    public OptionalUpdate<string?> Slug { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> Description { get; set; } = OptionalUpdate<string?>.Unspecified();
    public OptionalUpdate<string?> Color { get; set; } = OptionalUpdate<string?>.Unspecified();
}

public sealed class UpdateEventSessionGroupPlacementDto
{
    public OptionalUpdate<Guid?> LocationId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
    public OptionalUpdate<Guid?> RoomId { get; set; } = OptionalUpdate<Guid?>.Unspecified();
}

public sealed class UpdateEventSessionGroupOrderingDto
{
    public int? SortOrder { get; set; }
}

public sealed class UpdateEventSessionGroupPublicationDto
{
    public bool? IsPublished { get; set; }
}
