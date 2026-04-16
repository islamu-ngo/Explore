using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event;

public class CreateEventDto
{
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }

    // Event Type
    public int? EventTypeId { get; set; }

    // Audience
    public int? AudienceGenderId { get; set; }
    public int? AudienceAgeId { get; set; }

    /// <summary>
    /// Optional: The organization that owns this event.
    /// If provided, the user must have event:create permission in the organization.
    /// Mutually exclusive with GroupId.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Optional: The group that owns this event.
    /// If provided, the user must have event:create permission in the group.
    /// Mutually exclusive with OrganizationId.
    /// </summary>
    public Guid? GroupId { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image (optional - null if no image uploaded)
    public Guid? FeaturedImageId { get; set; }

    // Registration
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }

    // Status & Visibility (defaults: Draft=1, Public=1)
    public int EventStatusId { get; set; } = 1; // Default: Draft
    public int VisibilityTypeId { get; set; } = 1; // Default: Public

    // Format
    public int EventFormatId { get; set; } = 1; // Default: In-Person

    // Islamic Context
    public int? MadhabId { get; set; }

    // Session Info (computed, but can be set initially)
    public DateTimeOffset? FirstSessionDate { get; set; }
    public DateTimeOffset? LastSessionDate { get; set; }
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

    /// <summary>
    /// Optional: Event template to instantiate custom property definitions from.
    /// If provided, the template must be published and active.
    /// </summary>
    public Guid? TemplateId { get; set; }

    // Series membership
    public Guid? EventSeriesId { get; set; }
    public int? SeriesOrder { get; set; }

    // Registration policy (lookup FK)
    public int? RegistrationPolicyId { get; set; }
}
