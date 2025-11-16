using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event
{
    public class CreateEventDto
    {
        public int ProgramTypeId { get; set; } // TODO le user ne doit pas passer cette property, ça doit automatiquement être mis comme programtypeid value 1 car c'est ce qui correspont à event user ne peux pas mettre autre chose. donc ne laisse pas user dire ça.
        public string Title { get; set; }
        public string Description { get; set; }
        public int AudienceGenderId { get; set; }
        public int AudienceAgeId { get; set; }
        public Guid OrganizationId { get; set; }
        public int? AudienceAttendees { get; set; }
        public double Price { get; set; }
        public Guid? FeaturedImageId { get; set; }
        public bool? IsRegistrationRequired { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }
        public int EventTypeId { get; set; }
    }
}
