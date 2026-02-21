using System;
namespace Explore.Application.DTOs.EventSession;

public class EventSessionListDto
{
    public Guid Id { get; set; }

    // Event relationship
    public Guid EventId { get; set; }
    public required string EventTitle { get; set; }

    // Timing
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    // Location
    public Guid? LocationId { get; set; }
    public string? LocationFullName { get; set; }
    public string? LocationCity { get; set; }

    // Session Details
    public string? Title { get; set; }
    public string? Slug { get; set; }

    // Attendance
    public int? MaxAudienceAttendees { get; set; }
    public int? CurrentAudienceAttendees { get; set; }

    // Registration
    public int? RegistrationModeId { get; set; }
    public string? RegistrationModeFullName { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Optional Islamic extension for this session
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }
}
