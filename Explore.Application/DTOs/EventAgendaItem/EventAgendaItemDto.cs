// ABOUTME: Detail read-model DTO for a single event-level agenda item.
// ABOUTME: Includes UTC times, cached local projections, and optional room/kind metadata.

namespace Explore.Application.DTOs.EventAgendaItem;

public class EventAgendaItemDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid? EventDayId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public DateOnly LocalStartDate { get; set; }
    public DateOnly LocalEndDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public TimeOnly LocalEndTime { get; set; }
    public int LocalStartMinuteOfDay { get; set; }
    public int LocalEndMinuteOfDay { get; set; }

    public Guid? LocationId { get; set; }
    public Guid? RoomId { get; set; }
    public int? KindId { get; set; }
    public string? KindFullName { get; set; }
    public int SortOrder { get; set; }
    public Guid TenantId { get; set; }
}
