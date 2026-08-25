// ABOUTME: Create Event draft contract used by API, AI confirmation, and MCP tool execution.
// ABOUTME: Carries metadata plus the initial locations, days, rooms, sessions, agenda, and Islamic aspect graph.

namespace Explore.Application.DTOs.Event;

using Explore.Application.DTOs.EventAspects;

public sealed record CreateEventDraftRequestDto
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
    public Guid? FeaturedImageId { get; set; }
    public required ConfigureEventParticipationDto ParticipationConfiguration { get; init; }
    public int VisibilityTypeId { get; init; } = 1;
    public int EventFormatId { get; init; } = 1;
    public int EventStatusId { get; init; } = 1;
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
    public List<Guid> CategoryIds { get; init; } = [];
    public List<Guid> TagIds { get; init; } = [];
    public List<CreateEventLocationDto> Locations { get; init; } = [];
    public List<CreateEventGraphSessionDto> Sessions { get; init; } = [];
    public List<CreateEventGraphDayDto> Days { get; init; } = [];
    public List<CreateEventRoomDto> Rooms { get; init; } = [];
    public List<CreateEventGraphAgendaItemDto> AgendaItems { get; init; } = [];

    public CreateEventDto ToCreateEventDto() => new()
    {
        Title = Title,
        Subtitle = Subtitle,
        Description = Description,
        Content = Content,
        Slug = Slug,
        EventTypeId = EventTypeId,
        AudienceGenderId = AudienceGenderId,
        AudienceAgeId = AudienceAgeId,
        OrganizationId = OrganizationId,
        GroupId = GroupId,
        FeaturedImageId = FeaturedImageId,
        ParticipationConfiguration = ParticipationConfiguration,
        EventStatusId = EventStatusId == 0 ? 1 : EventStatusId,
        VisibilityTypeId = VisibilityTypeId,
        EventFormatId = EventFormatId,
        MadhabId = MadhabId,
        Timezone = Timezone,
        EventTimeZoneId = EventTimeZoneId,
        BackgroundColor = BackgroundColor,
        BackgroundEffect = BackgroundEffect,
        BackgroundImageId = BackgroundImageId,
        TemplateId = TemplateId,
        EventSeriesId = EventSeriesId,
        SeriesOrder = SeriesOrder,
        RegistrationPolicyId = RegistrationPolicyId,
        IslamicAspect = IslamicAspect,
        CategoryIds = CategoryIds,
        TagIds = TagIds,
        Locations = Locations,
        Sessions = Sessions,
        Days = Days,
        Rooms = Rooms,
        AgendaItems = AgendaItems
    };
}
