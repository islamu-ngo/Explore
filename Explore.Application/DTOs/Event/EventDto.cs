using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event
{
    public class EventDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }

        // Event Type
        public int EventTypeId { get; set; }
        public string EventTypeFullName { get; set; }
        public string EventTypeMasterCode { get; set; }

        // Audience
        public int AudienceGenderId { get; set; }
        public string AudienceGenderFullName { get; set; }
        public string AudienceGenderMasterCode { get; set; }
        public int AudienceAgeId { get; set; }
        public string AudienceAgeFullName { get; set; }
        public string AudienceAgeMasterCode { get; set; }
        public int? AudienceAgeMinAge { get; set; }
        public int? AudienceAgeMaxAge { get; set; }

        // Actor (Owner - User or Organization)
        public Guid ActorId { get; set; }
        public string ActorDisplayName { get; set; }
        public string? ActorHandle { get; set; }
        public string? ActorDid { get; set; }
        public int ActorTypeId { get; set; }
        public string ActorTypeFullName { get; set; }
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

        // Status & Visibility
        public int EventStatusId { get; set; }
        public string EventStatusFullName { get; set; }
        public string EventStatusMasterCode { get; set; }
        public int VisibilityTypeId { get; set; }
        public string VisibilityTypeFullName { get; set; }
        public string VisibilityTypeMasterCode { get; set; }

        // Format
        public int EventFormatId { get; set; }
        public string EventFormatFullName { get; set; }
        public string EventFormatMasterCode { get; set; }

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
        public string? EventUrl { get; set; }

        // ATProto Federation
        public Guid? AtprotoRecordId { get; set; }
        public string? AtprotoRecordUri { get; set; }
        public string? AtprotoRecordCid { get; set; }

        // Tenant
        public Guid TenantId { get; set; }
    }
}
