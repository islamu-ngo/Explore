// ABOUTME: Detailed event DTO returned by event detail APIs and HAL resources.
// ABOUTME: Separates short card description from longer event content for full detail views.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Explore.Application.DTOs.Category;
using Explore.Application.DTOs.EventAspects;
using Explore.Application.DTOs.Tag;

namespace Explore.Application.DTOs.Event;

public class EventDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public string? Slug { get; set; }
    public string? PublicCode { get; set; }

    // Event Type
    public int? EventTypeId { get; set; }
    public string? EventTypeFullName { get; set; }
    public string? EventTypeMasterCode { get; set; }

    // Audience
    public int? AudienceGenderId { get; set; }
    public string? AudienceGenderFullName { get; set; }
    public string? AudienceGenderMasterCode { get; set; }
    public int? AudienceAgeId { get; set; }
    public string? AudienceAgeFullName { get; set; }
    public string? AudienceAgeMasterCode { get; set; }
    public int? AudienceAgeMinAge { get; set; }
    public int? AudienceAgeMaxAge { get; set; }

    // Actor (Owner - User or Organization)
    public Guid ActorId { get; set; }
    public required string ActorDisplayName { get; set; }
    public string? ActorHandle { get; set; }
    public string? ActorDid { get; set; }
    public int ActorTypeId { get; set; }
    public required string ActorTypeFullName { get; set; }
    public Guid? ActorUserId { get; set; }
    public Guid? ActorOrganizationId { get; set; }
    public Guid? ActorGroupId { get; set; }
    public Guid? ActorProfilePictureId { get; set; }
    public string? ActorProfilePictureUri { get; set; }

    public int ProvenanceTypeId { get; set; }
    public string? ProvenanceTypeCode { get; set; }
    public string? ProvenanceTypeName { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? OrganizerActorId { get; set; }
    [JsonIgnore]
    public Guid? OrganizerActorUserId { get; set; }
    [JsonIgnore]
    public Guid? OrganizerActorOrganizationId { get; set; }
    [JsonIgnore]
    public Guid? OrganizerActorGroupId { get; set; }
    [JsonIgnore]
    public bool IsPubliclyEligible { get; set; }
    [JsonIgnore]
    public bool IsManagementView { get; set; }
    public string? SourcePublisherName { get; set; }
    public List<EventPublicActionDto> PublicActions { get; set; } = new();

    public EventTicketPriceSummaryDto? TicketPriceSummary { get; set; }

    // Featured Image
    public Guid FeaturedImageId { get; set; }
    public string? FeaturedImageUri { get; set; }

    public EventParticipationConfigurationDto? ParticipationConfiguration { get; set; }
    public int? RegistrationPolicyId { get; set; }
    public string? RegistrationPolicyFullName { get; set; }
    public string? RegistrationPolicyMasterCode { get; set; }

    // Status & Visibility
    public int EventStatusId { get; set; }
    public required string EventStatusFullName { get; set; }
    public required string EventStatusMasterCode { get; set; }
    public bool IsUnmoderationEligible { get; set; }
    public int VisibilityTypeId { get; set; }
    public required string VisibilityTypeFullName { get; set; }
    public required string VisibilityTypeMasterCode { get; set; }

    // Format
    public int EventFormatId { get; set; }
    public required string EventFormatFullName { get; set; }
    public required string EventFormatMasterCode { get; set; }

    // Islamic Context
    public int? MadhabId { get; set; }
    public string? MadhabFullName { get; set; }
    public string? MadhabMasterCode { get; set; }

    // Session Info
    public int? SessionCount { get; set; }
    public DateOnly? FirstSessionDate { get; set; }
    public DateOnly? LastSessionDate { get; set; }
    public string? Timezone { get; set; }

    // Metadata
    public int TotalViews { get; set; }

    // ATProto Federation
    public Guid? AtprotoRecordId { get; set; }
    public string? AtprotoRecordUri { get; set; }
    public string? AtprotoRecordCid { get; set; }

    // ===== Aspects =====
    // List of active aspect types for this event (e.g., ["Islamic", "Tech"])
    public List<string> AvailableAspects { get; set; } = new();

    // Islamic Aspect (only populated if event has Islamic characteristics)
    public EventAspects.EventIslamicAspectDto? IslamicAspect { get; set; }

    // Tech Aspect (only populated if event has Tech characteristics)
    public EventAspects.EventTechAspectDto? TechAspect { get; set; }

    // Appearance
    public string? BackgroundColor { get; set; }
    public string? BackgroundEffect { get; set; }
    public Guid? BackgroundImageId { get; set; }
    public string? BackgroundImageUri { get; set; }

    // Tags & Categories (populated via junction tables)
    public List<TagListDto> Tags { get; set; } = new();
    public List<CategoryListDto> Categories { get; set; } = new();

    // Tenant
    public Guid TenantId { get; set; }

    internal EventDto CreateRequestCopy() => (EventDto)MemberwiseClone();
}
