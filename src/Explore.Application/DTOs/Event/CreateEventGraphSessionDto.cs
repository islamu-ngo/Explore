// ABOUTME: Sub-DTO for creating an individual event session within the event creation graph.
// ABOUTME: Carries session timing, room/location references via temp keys, and speaker associations.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSession;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public class CreateEventGraphSessionDto
{
    public string? TempKey { get; set; }
    public string? DayTempKey { get; set; }
    public string? RoomTempKey { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public SessionEndTimeType EndTimeType { get; set; } = SessionEndTimeType.Fixed;
    public Guid? LocationId { get; set; }
    public string? LocationTempKey { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public int SortOrder { get; set; }
    public string? Title { get; set; }
    public int? EventSessionKindId { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public int? MaxAudienceAttendees { get; set; }
    public int? RegistrationModeId { get; set; }
    public Guid? SessionTemplateId { get; set; }
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }
    public List<int> LanguageIds { get; set; } = new();
    public List<Guid> SpeakerActorIds { get; set; } = new();
}
