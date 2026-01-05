using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event
{
    public class CreateEventDto
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }

        // Event Type
        public int EventTypeId { get; set; }

        // Audience
        public int AudienceGenderId { get; set; }
        public int AudienceAgeId { get; set; }

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

        // Session Info (computed, but can be set initially)
        public DateOnly? FirstSessionDate { get; set; }
        public DateOnly? LastSessionDate { get; set; }
        public string? Timezone { get; set; }

        // Metadata
        public string? EventUrl { get; set; }

        // Tenant (set by system based on context)
        public Guid TenantId { get; set; }
    }
}
