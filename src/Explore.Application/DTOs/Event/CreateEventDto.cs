// ABOUTME: Canonical single-submit DTO for creating an event with its initial scheduling graph.
// ABOUTME: Models only Create Event page inputs that must be persisted atomically by the API.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventSession;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public class CreateEventDto
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
    public Guid? FeaturedImageId { get; set; }
    public required ConfigureEventParticipationDto ParticipationConfiguration { get; set; }
    public int EventStatusId { get; set; } = 1;
    public int VisibilityTypeId { get; set; } = 1;
    public int EventFormatId { get; set; } = 1;
    public int? MadhabId { get; set; }
    public string? Timezone { get; set; }
    public string? EventTimeZoneId { get; set; }
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
    public List<CreateEventLocationDto> Locations { get; set; } = new();
    public List<CreateEventGraphSessionDto> Sessions { get; set; } = new();
    public List<CreateEventGraphDayDto> Days { get; set; } = new();
    public List<CreateEventRoomDto> Rooms { get; set; } = new();
    public List<CreateEventGraphAgendaItemDto> AgendaItems { get; set; } = new();
}
