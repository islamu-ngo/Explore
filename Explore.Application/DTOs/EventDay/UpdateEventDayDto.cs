// ABOUTME: DTO for updating an existing EventDay entity.
// ABOUTME: Id targets the row; EventId validates ownership; all mutable fields are present.

namespace Explore.Application.DTOs.EventDay;

public class UpdateEventDayDto
{
    public Guid Id { get; set; }
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
