// ABOUTME: Sub-DTO for creating rooms within event locations in the scheduling graph.
// ABOUTME: References a location by existing ID or temp key for pre-persistence linkage.

using System;

namespace Explore.Application.DTOs.Event;

public class CreateEventRoomDto
{
    public required string TempKey { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationTempKey { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}
