// ABOUTME: DTO for creating a new event with optional inline scheduling (days, rooms, agenda items).
// ABOUTME: Days/rooms/agenda are optional nested collections created in the same transaction after the event.
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

    public List<InlineEventDayDto>? Days { get; set; }
    public List<InlineLocationRoomDto>? Rooms { get; set; }
    public List<InlineEventAgendaItemDto>? AgendaItems { get; set; }
}

public class InlineEventDayDto
{
    public DateOnly LocalDate { get; set; }
    public string? Label { get; set; }
    public string? Description { get; set; }
    public string? BannerText { get; set; }
    public Guid? BannerImageId { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public bool AllowsDayScopeRegistration { get; set; }
}

public class InlineLocationRoomDto
{
    public Guid LocationId { get; set; }
    public required string Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public int SortOrder { get; set; }
}

public class InlineEventAgendaItemDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Guid? RoomId { get; set; }
    public int? KindId { get; set; }
    public int SortOrder { get; set; }
}
