// ABOUTME: Sub-DTO for creating rooms within event locations in the scheduling graph.
// ABOUTME: References a location by existing ID or temp key for pre-persistence linkage.

using System;

namespace Explore.Application.DTOs.Event;

public sealed record CreateEventRoomDto
{
    public required string TempKey { get; init; }
    public Guid? LocationId { get; init; }
    public string? LocationTempKey { get; init; }
    public required string Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public int? Capacity { get; init; }
    public int SortOrder { get; init; }
}
