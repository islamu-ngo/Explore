// ABOUTME: Draft-only event create contract for the progressive Create Event shell.
// ABOUTME: Excludes sessions, rooms, days, and agenda graph fields; program items are added after draft save.

namespace Explore.Application.DTOs.Event;

public sealed class CreateEventDraftRequestDto
{
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
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
    public List<Guid> CategoryIds { get; set; } = [];
    public List<Guid> TagIds { get; set; } = [];

    public CreateEventRequest ToCreateEventRequest() => new()
    {
        Title = Title,
        Subtitle = Subtitle,
        Description = Description,
        Slug = Slug,
        EventTypeId = EventTypeId,
        AudienceGenderId = AudienceGenderId,
        AudienceAgeId = AudienceAgeId,
        OrganizationId = OrganizationId,
        GroupId = GroupId,
        Price = Price,
        CurrencyCode = CurrencyCode,
        FeaturedImageId = FeaturedImageId,
        IsRegistrationRequired = IsRegistrationRequired,
        ExternalRegistrationUrl = ExternalRegistrationUrl,
        EventStatusId = 1,
        VisibilityTypeId = VisibilityTypeId,
        EventFormatId = EventFormatId,
        MadhabId = MadhabId,
        Timezone = Timezone,
        EventTimeZoneId = EventTimeZoneId,
        EventUrl = EventUrl,
        BackgroundColor = BackgroundColor,
        BackgroundEffect = BackgroundEffect,
        BackgroundImageId = BackgroundImageId,
        TemplateId = TemplateId,
        EventSeriesId = EventSeriesId,
        SeriesOrder = SeriesOrder,
        RegistrationPolicyId = RegistrationPolicyId,
        CategoryIds = CategoryIds,
        TagIds = TagIds,
        Sessions = [],
        Days = [],
        Rooms = [],
        AgendaItems = []
    };
}
