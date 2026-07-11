// ABOUTME: List read-model DTO for EventDay used in paginated collection responses.
// ABOUTME: Lightweight projection with key fields for day-level admin lists and agenda grouping.

namespace Explore.Application.DTOs.EventDay;

public class EventDayListDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }
    public Guid ConcurrencyStamp { get; set; }
}
