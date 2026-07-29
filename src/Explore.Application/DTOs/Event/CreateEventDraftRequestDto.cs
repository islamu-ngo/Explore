// ABOUTME: Create Event draft contract used by API, AI confirmation, and MCP tool execution.
// ABOUTME: Carries metadata plus the initial locations, days, rooms, sessions, agenda, and Islamic aspect graph.

namespace Explore.Application.DTOs.Event;

using Explore.Application.DTOs.EventAspects;

public sealed class CreateEventDraftRequestDto
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
    public int VisibilityTypeId { get; set; } = 1;
    public int EventFormatId { get; set; } = 1;
    public int EventStatusId { get; set; } = 1;
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
    public List<Guid> CategoryIds { get; set; } = [];
    public List<Guid> TagIds { get; set; } = [];
    public List<CreateEventLocationRequest> Locations { get; set; } = [];
    public List<CreateEventSessionRequest> Sessions { get; set; } = [];
    public List<CreateEventDayRequest> Days { get; set; } = [];
    public List<CreateEventRoomRequest> Rooms { get; set; } = [];
    public List<CreateEventAgendaItemRequest> AgendaItems { get; set; } = [];

    public CreateEventRequest ToCreateEventRequest() => new()
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
