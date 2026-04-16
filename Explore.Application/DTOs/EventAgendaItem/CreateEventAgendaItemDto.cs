// ABOUTME: DTO for creating a new event-level agenda item (break, prayer, opening, logistics).
// ABOUTME: StartTime/EndTime are UTC; local projections are computed by the handler via Reschedule().

namespace Explore.Application.DTOs.EventAgendaItem;

public class CreateEventAgendaItemDto
{
    public Guid EventId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? RoomId { get; set; }
    public int? KindId { get; set; }
    public int SortOrder { get; set; }
}
