// ABOUTME: Sub-DTO for creating agenda items within the event creation graph.
// ABOUTME: Defines time-bound agenda entries with location and room references.

using System;

namespace Explore.Application.DTOs.Event;

public class CreateEventGraphAgendaItemDto
{
    public string? TempKey { get; set; }
    public string? DayTempKey { get; set; }
    public string? RoomTempKey { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationTempKey { get; set; }
    public Guid? RoomId { get; set; }
    public int? KindId { get; set; }
    public int SortOrder { get; set; }
}
