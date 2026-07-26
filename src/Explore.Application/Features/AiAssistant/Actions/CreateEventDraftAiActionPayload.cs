// ABOUTME: Defines the safe AI-proposed payload shape for structured draft event creation.
// ABOUTME: Excludes privileged lifecycle fields while allowing the initial event graph captured from user-provided context.

using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.EventSession;

namespace Explore.Application.Features.AiAssistant.Actions;

public sealed class CreateEventDraftAiActionPayload
{
    public string? Title { get; init; }

    public string? Subtitle { get; init; }

    public string? Description { get; init; }

    public string? Content { get; init; }

    public string? Slug { get; init; }

    public int? EventTypeId { get; init; }

    public int? AudienceGenderId { get; init; }

    public int? AudienceAgeId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? GroupId { get; init; }

    public decimal? Price { get; init; }

    public string? CurrencyCode { get; init; }

    public bool IsRegistrationRequired { get; init; }

    public string? ExternalRegistrationUrl { get; init; }

    public int VisibilityTypeId { get; init; } = 1;

    public int EventFormatId { get; init; } = 1;

    public int? MadhabId { get; init; }

    public CreateUpdateIslamicAspectDto? IslamicAspect { get; init; }

    public string? Timezone { get; init; }

    public string? EventTimeZoneId { get; init; }


    public List<Guid> CategoryIds { get; init; } = [];

    public List<Guid> TagIds { get; init; } = [];

    public CreateEventDraftLocationPayload? Location { get; init; }

    public CreateEventDraftRoomPayload? Room { get; init; }

    public CreateEventDraftSessionPayload? Session { get; init; }
}

public sealed class CreateEventDraftLocationPayload
{
    public string? FullName { get; init; }

    public string? Address { get; init; }

    public string? Postcode { get; init; }

    public string? Country { get; init; }

    public string? City { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? Timezone { get; init; }
}

public sealed class CreateEventDraftRoomPayload
{
    public string? Name { get; init; }

    public string? Slug { get; init; }

    public string? Description { get; init; }

    public int? Capacity { get; init; }
}

public sealed class CreateEventDraftSessionPayload
{
    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset EndTime { get; init; }

    public string? Title { get; init; }

    public int? EventSessionKindId { get; init; }

    public string? Description { get; init; }

    public string? Slug { get; init; }

    public int? MaxAudienceAttendees { get; init; }

    public int? RegistrationModeId { get; init; }

    public decimal? Price { get; init; }

    public string? CurrencyCode { get; init; }

    public EventSessionIslamicAspectDto? IslamicAspect { get; init; }

    public List<int> LanguageIds { get; init; } = [];

    public List<Guid> SpeakerActorIds { get; init; } = [];
}
