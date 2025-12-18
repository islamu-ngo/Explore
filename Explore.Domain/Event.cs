using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class Event
    {
        public Guid Id { get; set; }
        [ForeignKey("EventType")]
        public int EventTypeId { get; set; }
        public EventType EventType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        [ForeignKey("AudienceGender")]
        public int AudienceGenderId { get; set; }
        public AudienceGender AudienceGender { get; set; }
        [ForeignKey("AudienceAge")]
        public int AudienceAgeId { get; set; }
        public AudienceAge AudienceAge { get; set; }
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; }
        public int? AudienceAttendees { get; set; }
        public double? Price { get; set; }
        public string? CurrencyCode { get; set; }
        [ForeignKey("FeaturedImage")]
        public Guid? FeaturedImageId { get; set; }
        public StorageObject? FeaturedImage { get; set; }
        public int TotalViews { get; set; }
        public bool? IsRegistrationRequired { get; set; } // TODO pourquoi j'ai mis nullable! Mince, ça doit être false or ture! plutart changer partout ou il faut quand j'ai le temps!
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }
    }
}
