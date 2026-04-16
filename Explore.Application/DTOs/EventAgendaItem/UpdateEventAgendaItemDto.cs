// ABOUTME: DTO for updating an existing event-level agenda item.
// ABOUTME: Id targets the row; StartTime/EndTime are UTC; local projections are recomputed via Reschedule().

namespace Explore.Application.DTOs.EventAgendaItem;

public class UpdateEventAgendaItemDto
{
    public Guid Id { get; set; }
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
