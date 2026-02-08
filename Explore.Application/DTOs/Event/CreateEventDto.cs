using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event
{
    public class CreateEventDto
    {
        public string Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? Slug { get; set; }

        // Event Type
        public int EventTypeId { get; set; }

        // Audience
        public int AudienceGenderId { get; set; }
        public int AudienceAgeId { get; set; }

        /// <summary>
        /// Optional: The organization that owns this event.
        /// If null, the event is created under the user's personal actor.
        /// If provided, the user must be an admin of the organization.
        /// </summary>
        public Guid? OrganizationId { get; set; }

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

        // Metadata
        public string? EventUrl { get; set; }
        public string? MetadataJson { get; set; }
    }
}
