using System;

namespace Explore.Application.DTOs.Organization
{
    public class UpdateOrganizationDto
    {
        public string FullName { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public int Postcode { get; set; }
        public string Address { get; set; }
    }
}
