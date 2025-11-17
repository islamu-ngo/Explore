using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class Program
    {
        public Guid Id { get; set; }
        [ForeignKey("ProgramType")]
        public int ProgramTypeId { get; set; }
        public ProgramType ProgramType { get; set; }
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
        public double Price { get; set; }
        [ForeignKey("FeaturedImage")]
        public Guid? FeaturedImageId { get; set; }
        public StorageObject? FeaturedImage { get; set; }
        public int TotalViews { get; set; }
        public bool? IsRegistrationRequired { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }
    }
}
