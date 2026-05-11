using System;
using System.Collections.Generic;
namespace Explore.Application.DTOs.EventSession;

public class EventSessionDto
{
    public Guid Id { get; set; }

    // Event relationship
    public Guid EventId { get; set; }
    public required string EventTitle { get; set; }

    // Day assignment (auto-linked from session start date)
    public Guid? EventDayId { get; set; }

    // Timing (UTC)
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    // Cached local projections (event timezone)
    public DateOnly LocalStartDate { get; set; }
    public DateOnly LocalEndDate { get; set; }
    public TimeOnly LocalStartTime { get; set; }
    public TimeOnly LocalEndTime { get; set; }
    public int LocalStartMinuteOfDay { get; set; }
    public int LocalEndMinuteOfDay { get; set; }

    // Ordering
    public int SortOrder { get; set; }

    // Location
    public Guid? LocationId { get; set; }
    public string? LocationFullName { get; set; }
    public string? LocationAddress { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationCountry { get; set; }

    // Room (child of Location)
    public Guid? RoomId { get; set; }
    public string? RoomName { get; set; }

    // Session Details
    public string? Title { get; set; }
    public int? EventSessionKindId { get; set; }
    public string? EventSessionKindFullName { get; set; }
    public string? EventSessionKindMasterCode { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Media
    public Guid? FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    // Attendance
    public int? MaxAudienceAttendees { get; set; }
    public int? CurrentAudienceAttendees { get; set; }

    // Registration
    public int? RegistrationModeId { get; set; }
    public string? RegistrationModeFullName { get; set; }
    public string? RegistrationModeMasterCode { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Optional Islamic extension for this session
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }

    // Program sections/tracks/devrooms this session belongs to
    public List<EventSessionGroupAssignmentDto> SessionGroups { get; set; } = [];

    // Tenant
    public Guid TenantId { get; set; }
}
