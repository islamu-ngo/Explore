using Explore.Application.DTOs.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Education
{
    public class EducationDto
    {
        // Program properties
        public Guid Id { get; set; }
        public int ProgramTypeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int AudienceGenderId { get; set; }
        public int AudienceAgeId { get; set; }
        public Guid OrganizationId { get; set; }
        public int? AudienceAttendees { get; set; }
        public double Price { get; set; }
        public Guid? FeaturedImageId { get; set; }
        public bool? IsRegistrationRequired { get; set; }
        public int TotalViews { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }

        // Education specific properties
        public int EducationTypeId { get; set; }

        //public BootcampDto? Bootcamp { get; set; } // just to give idea of why we need educationtypeid, there will be many kind of educaiton just like how there are many kind of programs
    }
}
