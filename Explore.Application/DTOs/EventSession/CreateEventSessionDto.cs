using System;
namespace Explore.Application.DTOs.EventSession;

public class CreateEventSessionDto
{
    // Event relationship
    public Guid EventId { get; set; }

    // Timing
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    // Location
    public Guid? LocationId { get; set; }

    // Media
    public Guid? FeaturedImageId { get; set; }

    // Room (optional child of LocationId used by same-room overlap guard)
    public Guid? RoomId { get; set; }

    // Ordering
    public int SortOrder { get; set; }

    // Session Details
    public string? Title { get; set; }
    public int? EventSessionKindId { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Attendance
    public int? MaxAudienceAttendees { get; set; }

    // Registration
    public int? RegistrationModeId { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    /// Optional: Session template to instantiate custom property definitions from.
    /// If provided, the template must be published and active.
    public Guid? SessionTemplateId { get; set; }

    // Optional Islamic extension for this session
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }

    // Tenant (set by system based on context)
    public Guid TenantId { get; set; }
}
