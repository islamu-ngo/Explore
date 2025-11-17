using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event
{
    public class EventListDto
    {
        // Program properties
        //edit: added it back need to investigate!: only ommited isRegistrationRequired cause no need to display it in list view (or maybe well.. like when user clicks on register from the listview page then directly go to form so will need it! TODO need to investigate)
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
        public bool? IsRegistrationRequired { get; set; }
        public int TotalViews { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public int? PostCode { get; set; }
        public string? Address { get; set; }
        public string? ProgramUrl { get; set; }

        // Event specific properties
        public int EventTypeId { get; set; }
        public string EventTypeFullName { get; set; }
    }
}
