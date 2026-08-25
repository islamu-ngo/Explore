// ABOUTME: Canonical single-submit DTO for creating an event with its initial scheduling graph.
// ABOUTME: Models only Create Event page inputs that must be persisted atomically by the API.

using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventSession;
using Explore.Domain.Enums;

namespace Explore.Application.DTOs.Event;

public sealed record CreateEventDto
{
    public required string Title { get; init; }
    public string? Subtitle { get; init; }
    public string? Description { get; init; }
    public string? Content { get; init; }
    public string? Slug { get; init; }
    public int? EventTypeId { get; init; }
    public int? AudienceGenderId { get; init; }
    public int? AudienceAgeId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? FeaturedImageId { get; init; }
    public required ConfigureEventParticipationDto ParticipationConfiguration { get; init; }
    public int EventStatusId { get; init; } = 1;
    public int VisibilityTypeId { get; init; } = 1;
    public int EventFormatId { get; init; } = 1;
    public int? MadhabId { get; init; }
    public string? Timezone { get; init; }
    public string? EventTimeZoneId { get; init; }
    public string? BackgroundColor { get; init; }
    public string? BackgroundEffect { get; init; }
    public Guid? BackgroundImageId { get; init; }
    public Guid? TemplateId { get; init; }
    public Guid? EventSeriesId { get; init; }
    public int? SeriesOrder { get; init; }
    public int? RegistrationPolicyId { get; init; }
    public CreateUpdateIslamicAspectDto? IslamicAspect { get; init; }
    public List<Guid> CategoryIds { get; init; } = new();
    public List<Guid> TagIds { get; init; } = new();
    public List<CreateEventLocationDto> Locations { get; init; } = new();
    public List<CreateEventGraphSessionDto> Sessions { get; init; } = new();
    public List<CreateEventGraphDayDto> Days { get; init; } = new();
    public List<CreateEventRoomDto> Rooms { get; init; } = new();
    public List<CreateEventGraphAgendaItemDto> AgendaItems { get; init; } = new();
}
