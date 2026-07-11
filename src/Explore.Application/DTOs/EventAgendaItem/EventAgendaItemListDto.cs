// ABOUTME: List read-model DTO for event-level agenda items in collection responses.
// ABOUTME: Lightweight projection with key fields for agenda grid rendering and admin lists.

namespace Explore.Application.DTOs.EventAgendaItem;

public class EventAgendaItemListDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public DateOnly LocalStartDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public TimeOnly LocalEndTime { get; set; }
    public int? KindId { get; set; }
    public string? KindFullName { get; set; }
    public int SortOrder { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
