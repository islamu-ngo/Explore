// ABOUTME: DTO for creating a new EventDay - the first-class event-local day aggregate.
// ABOUTME: EventId scopes the day to a parent event; LocalDate is the event-local calendar date.

namespace Explore.Application.DTOs.EventDay;

public class CreateEventDayDto
{
    public Guid EventId { get; set; }
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? BannerText { get; set; }
    public Guid? BannerImageId { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }
}
