using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Program
{
    public class ProgramListDto
    {
        public Guid Id { get; set; }
        public int ProgramTypeId { get; set; }
        public string ProgramTypeFullName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int AudienceGenderId { get; set; }
        public string AudienceGenderFullName { get; set; }
        public int AudienceAgeId { get; set; }
        public string AudienceAgeFullName { get; set; }
        public int? AudienceAgeMinAge { get; set; }
        public int? AudienceAgeMaxAge { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationFullName { get; set; }
        public int? AudienceAttendees { get; set; }
        public double Price { get; set; }
        public Guid? FeaturedImageId { get; set; }
        public string? FeaturedImageUri { get; set; }
        public int TotalViews { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }

        // TODO add other listdto that include programtype specfic data so as to be helpfull for thirdparty consuming our api for their use case and workflow. of course internally not needed cause gonna use the programtype's controller!
    }
}
