// ABOUTME: Defines the safe AI-proposed payload shape for draft event creation.
// ABOUTME: Excludes privileged event lifecycle, ownership, tenant, and program graph fields.

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

    public string? Timezone { get; init; }

    public string? EventTimeZoneId { get; init; }

    public string? EventUrl { get; init; }

    public List<Guid> CategoryIds { get; init; } = [];

    public List<Guid> TagIds { get; init; } = [];
}
