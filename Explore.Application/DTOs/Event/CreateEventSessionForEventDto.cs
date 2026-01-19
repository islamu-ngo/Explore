using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.Event;

/// <summary>
/// DTO for creating an event session as part of a unified event creation.
/// This is embedded within CreateEventWithSessionsDto.
/// </summary>
public class CreateEventSessionForEventDto
{
    /// <summary>
    /// Optional session title. If not provided, defaults to the event title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Optional session description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Session start time (required). Must be a valid DateTimeOffset.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Session end time (required). Must be after StartTime.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Optional location ID. If null, session is online-only.
    /// </summary>
    public Guid? LocationId { get; set; }

    /// <summary>
    /// Optional maximum number of attendees.
    /// </summary>
    public int? MaxAudienceAttendees { get; set; }

    /// <summary>
    /// Optional registration mode ID.
    /// </summary>
    public int? RegistrationModeId { get; set; }

    /// <summary>
    /// Optional list of language IDs for this session.
    /// </summary>
    public List<int> LanguageIds { get; set; } = new();
}
