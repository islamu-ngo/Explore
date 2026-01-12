using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class Event
    {
        public Guid Id { get; set; }

        [ForeignKey("EventType")]
        public int EventTypeId { get; set; }
        public EventType EventType { get; set; }

        public string Title { get; set; }
        public string? Description { get; set; }

        [ForeignKey("AudienceGender")]
        public int AudienceGenderId { get; set; }
        public AudienceGender AudienceGender { get; set; }

        [ForeignKey("AudienceAge")]
        public int AudienceAgeId { get; set; }
        public AudienceAge AudienceAge { get; set; }

        [ForeignKey("Actor")]
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; }

        public decimal? Price { get; set; }
        public string? CurrencyCode { get; set; }

        [ForeignKey("FeaturedImage")]
        public Guid? FeaturedImageId { get; set; }
        public StorageObject? FeaturedImage { get; set; }

        public int TotalViews { get; set; }
        public bool IsRegistrationRequired { get; set; }
        public string? EventUrl { get; set; }

        [ForeignKey("Madhab")]
        public int? MadhabId { get; set; }
        public Madhab? Madhab { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        public string? Slug { get; set; }

        [ForeignKey("VisibilityType")]
        public int VisibilityTypeId { get; set; }
        public VisibilityType VisibilityType { get; set; }

        public int? SessionCount { get; set; }

        [ForeignKey("EventStatus")]
        public int EventStatusId { get; set; }
        public EventStatus EventStatus { get; set; }

        public string? ExternalRegistrationUrl { get; set; }
        public DateOnly? FirstSessionDate { get; set; }
        public DateOnly? LastSessionDate { get; set; }
        public string? Timezone { get; set; }

        [ForeignKey("EventFormat")]
        public int EventFormatId { get; set; }
        public EventFormat EventFormat { get; set; }

        [ForeignKey("AtprotoRecord")]
        public Guid? AtprotoRecordId { get; set; }
        public AtprotoRecord? AtprotoRecord { get; set; }
    }
}
