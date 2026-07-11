// ABOUTME: Lightweight event card DTO returned by event list APIs and HAL collections.
// ABOUTME: Includes organizer ownership metadata needed by event-scoped authorization links.

using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event;

public class EventListDto
{
    //edit: added it back need to investigate!: only ommited isRegistrationRequired cause no need to display it in list view (or maybe well.. like when user clicks on register from the listview page then directly go to form so will need it! TODO need to investigate)
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public string? PublicCode { get; set; }

    // Event Type
    public int EventTypeId { get; set; }
    public required string EventTypeFullName { get; set; }

    // Audience
    public int AudienceGenderId { get; set; }
    public required string AudienceGenderFullName { get; set; }
    public int AudienceAgeId { get; set; }
    public required string AudienceAgeFullName { get; set; }
    public int? AudienceAgeMinAge { get; set; }
    public int? AudienceAgeMaxAge { get; set; }

    // Actor (Owner - User or Organization)
    public Guid ActorId { get; set; }
    public required string ActorDisplayName { get; set; }
    public int ActorTypeId { get; set; }
    public required string ActorTypeFullName { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? ActorOrganizationId { get; set; }
    public Guid? ActorGroupId { get; set; }
    public Guid? ActorProfilePictureId { get; set; }
    public string? ActorProfilePictureUri { get; set; }

    // Pricing
    public decimal? Price { get; set; }
    public string? CurrencyCode { get; set; }

    // Featured Image
    public Guid FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    // Registration
    public bool IsRegistrationRequired { get; set; }
    public string? ExternalRegistrationUrl { get; set; }
    public int? RegistrationPolicyId { get; set; }
    public string? RegistrationPolicyFullName { get; set; }

    // Status & Visibility
    public int EventStatusId { get; set; }
    public required string EventStatusFullName { get; set; }
    public int VisibilityTypeId { get; set; }
    public required string VisibilityTypeFullName { get; set; }

    // Format
    public int EventFormatId { get; set; }
    public required string EventFormatFullName { get; set; }

    // Islamic Context
    public int? MadhabId { get; set; }
    public string? MadhabFullName { get; set; }

    // Session Info
    public int? SessionCount { get; set; }
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    // Series
    public string? EventSeriesTitle { get; set; }

    // Temporal
    public bool IsPast { get; set; }

    // Metadata
    public int TotalViews { get; set; }
    public bool IsUserReported { get; set; }
    public string? EventUrl { get; set; }

    // Tenant
    public Guid TenantId { get; set; }
}
