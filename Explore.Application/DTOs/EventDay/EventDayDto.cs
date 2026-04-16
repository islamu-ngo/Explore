// ABOUTME: Detail read-model DTO for a single EventDay entity.
// ABOUTME: Includes all fields needed for admin surfaces and day-detail views.

namespace Explore.Application.DTOs.EventDay;

public class EventDayDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventTitle { get; set; }
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? BannerText { get; set; }
    public Guid? BannerImageId { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }
    public Guid TenantId { get; set; }
}
