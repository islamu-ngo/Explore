// ABOUTME: Detailed event-session DTO returned by detail APIs and HAL resources.
// ABOUTME: Exposes lifecycle status, schedule state, and concurrency metadata for management affordances.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.Location;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventSession;

public class EventSessionDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    // Event relationship
    public Guid EventId { get; set; }
    public required string EventTitle { get; set; }
    public int ParentEventStatusId { get; set; }

    // Day assignment (auto-linked from session start date)
    public Guid? EventDayId { get; set; }

    // Timing (UTC)
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public SessionEndTimeType EndTimeType { get; set; }
    public string? FormattedEndTime { get; set; }
    public bool IsScheduled { get; set; }

    // Cached local projections (event timezone)
    public DateOnly? LocalStartDate { get; set; }
    public DateOnly? LocalEndDate { get; set; }
    public TimeOnly? LocalStartTime { get; set; }
    public TimeOnly? LocalEndTime { get; set; }
    public int? LocalStartMinuteOfDay { get; set; }
    public int? LocalEndMinuteOfDay { get; set; }

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
    public EventLocationPublicDto? EventLocation { get; set; }

    // Session Details
    public string? Title { get; set; }
    public int? EventSessionKindId { get; set; }
    public string? EventSessionKindFullName { get; set; }
    public string? EventSessionKindMasterCode { get; set; }
    public int EventSessionStatusId { get; set; }
    public string? EventSessionStatusFullName { get; set; }
    public string? EventSessionStatusMasterCode { get; set; }
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

    // Optional Islamic extension for this session
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }

    // Program sections/tracks/devrooms this session belongs to
    public List<EventSessionGroupAssignmentDto> SessionGroups { get; set; } = [];

    // Tenant
    public Guid TenantId { get; set; }
}
