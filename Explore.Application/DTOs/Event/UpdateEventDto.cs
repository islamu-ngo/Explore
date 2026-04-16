using System;

namespace Explore.Application.DTOs.Event;

public class UpdateEventDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Event Type
    public int? EventTypeId { get; set; }

    // Audience
    public int? AudienceGenderId { get; set; }
    public int? AudienceAgeId { get; set; }

    // Actor (Owner - User or Organization)
    public Guid ActorId { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image
    public Guid FeaturedImageId { get; set; }

    // Registration
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }

    // Status & Visibility
    public int EventStatusId { get; set; }
    public int VisibilityTypeId { get; set; }

    // Format
    public int EventFormatId { get; set; }

    // Islamic Context
    public int? MadhabId { get; set; }

    // Session Info
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    // Temporal fields (UTC-based)
    public DateTimeOffset? FirstSessionStartUtc { get; set; }
    public DateTimeOffset? LastSessionStartUtc { get; set; }
    public string? EventTimeZoneId { get; set; }

    // Metadata
    public string? EventUrl { get; set; }

    // Appearance
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public Guid? BackgroundImageId { get; set; }

    // Series membership
    public Guid? EventSeriesId { get; set; }
    public int? SeriesOrder { get; set; }

    // Registration policy (lookup FK)
    public int? RegistrationPolicyId { get; set; }
}
