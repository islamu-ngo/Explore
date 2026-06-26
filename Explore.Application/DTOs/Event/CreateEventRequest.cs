// ABOUTME: Canonical single-submit request for creating an event with its initial scheduling graph.
// ABOUTME: Models only Create Event page inputs that must be persisted atomically by the API.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventSession;

namespace Explore.Application.DTOs.Event;

public class CreateEventRequest
{
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Slug { get; set; }
    public int? EventTypeId { get; set; }
    public int? AudienceGenderId { get; set; }
    public int? AudienceAgeId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? GroupId { get; set; }
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }
    public Guid? FeaturedImageId { get; set; }
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }
    public int EventStatusId { get; set; } = 1;
    public int VisibilityTypeId { get; set; } = 1;
    public int EventFormatId { get; set; } = 1;
    public int? MadhabId { get; set; }
    public string? Timezone { get; set; }
    public string? EventTimeZoneId { get; set; }
    public string? EventUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public Guid? BackgroundImageId { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid? EventSeriesId { get; set; }
    public int? SeriesOrder { get; set; }
    public int? RegistrationPolicyId { get; set; }
    public CreateUpdateIslamicAspectDto? IslamicAspect { get; set; }
    public List<Guid> CategoryIds { get; set; } = new();
    public List<Guid> TagIds { get; set; } = new();
    public List<CreateEventLocationRequest> Locations { get; set; } = new();
    public List<CreateEventSessionRequest> Sessions { get; set; } = new();
    public List<CreateEventDayRequest> Days { get; set; } = new();
    public List<CreateEventRoomRequest> Rooms { get; set; } = new();
    public List<CreateEventAgendaItemRequest> AgendaItems { get; set; } = new();
}

public class CreateEventSessionRequest
{
    public string? TempKey { get; set; }
    public string? DayTempKey { get; set; }
    public string? RoomTempKey { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
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
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }
    public Guid? SessionTemplateId { get; set; }
    public EventSessionIslamicAspectDto? IslamicAspect { get; set; }
    public List<int> LanguageIds { get; set; } = new();
    public List<Guid> SpeakerActorIds { get; set; } = new();
}

public class CreateEventDayRequest
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

public class CreateEventLocationRequest
{
    public required string TempKey { get; set; }
    public required string FullName { get; set; }
    public required string Address { get; set; }
    public required string Postcode { get; set; }
    public required string Country { get; set; }
    public required string City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
}

public class CreateEventRoomRequest
{
    public required string TempKey { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationTempKey { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}

public class CreateEventAgendaItemRequest
{
    public string? TempKey { get; set; }
    public string? DayTempKey { get; set; }
    public string? RoomTempKey { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid? LocationId { get; set; }
    public string? LocationTempKey { get; set; }
    public Guid? RoomId { get; set; }
    public int? KindId { get; set; }
    public int SortOrder { get; set; }
}
