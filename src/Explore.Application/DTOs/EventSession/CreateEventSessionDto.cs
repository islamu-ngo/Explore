using System;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.EventSession;

public sealed record CreateEventSessionDto
{
    // Event relationship
    public Guid EventId { get; init; }

    // Timing
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public SessionEndTimeType EndTimeType { get; init; } = SessionEndTimeType.Fixed;

    // Location
    public Guid? LocationId { get; init; }

    // Media
    public Guid? FeaturedImageId { get; init; }

    // Room (optional child of LocationId used by same-room overlap guard)
    public Guid? RoomId { get; init; }

    // Ordering
    public int SortOrder { get; init; }

    // Session Details
    public string? Title { get; init; }
    public int? EventSessionKindId { get; init; }
    public string? Description { get; init; }
    public string? Slug { get; init; }

    // Attendance
    public int? MaxAudienceAttendees { get; init; }

    // Registration
    public int? RegistrationModeId { get; init; }

    /// Optional: Session template to instantiate custom property definitions from.
    /// If provided, the template must be published and active.
    public Guid? SessionTemplateId { get; init; }

    // Optional Islamic extension for this session
    public EventSessionIslamicAspectDto? IslamicAspect { get; init; }

    // Tenant (set by system based on context)
}
