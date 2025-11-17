using Explore.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Application.DTOs.Organization
{
    public class OrganizationListDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public int Postcode { get; set; }
        public string Address { get; set; }
        public int StatusTypeId { get; set; }
        public string StatusTypeFullName { get; set; }
    }
}
