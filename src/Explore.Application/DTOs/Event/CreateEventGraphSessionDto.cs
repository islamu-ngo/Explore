// ABOUTME: Sub-DTO for creating an individual event session within the event creation graph.
// ABOUTME: Carries session timing, room/location references via temp keys, and speaker associations.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSession;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed record CreateEventGraphSessionDto
{
    public string? TempKey { get; init; }
    public string? DayTempKey { get; init; }
    public string? RoomTempKey { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public SessionEndTimeType EndTimeType { get; init; } = SessionEndTimeType.Fixed;
    public Guid? LocationId { get; init; }
    public string? LocationTempKey { get; init; }
    public Guid? RoomId { get; init; }
    public Guid? FeaturedImageId { get; init; }
    public int SortOrder { get; init; }
    public string? Title { get; init; }
    public int? EventSessionKindId { get; init; }
    public string? Description { get; init; }
    public string? Slug { get; init; }
    public int? MaxAudienceAttendees { get; init; }
    public int? RegistrationModeId { get; init; }
    public Guid? SessionTemplateId { get; init; }
    public EventSessionIslamicAspectDto? IslamicAspect { get; init; }
    public List<int> LanguageIds { get; init; } = new();
    public List<Guid> SpeakerActorIds { get; init; } = new();
}
