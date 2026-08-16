// ABOUTME: Sub-DTO for creating an individual event day within the event creation graph.
// ABOUTME: Carries the date, optional label/description, banner, and day-scope registration flag.

using System;

namespace Explore.Application.DTOs.Event;

public class CreateEventGraphDayDto
{
    public string? TempKey { get; set; }
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? BannerText { get; set; }
    public Guid? BannerImageId { get; set; }
    public bool IsPublished { get; set; } = true;
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }
}
