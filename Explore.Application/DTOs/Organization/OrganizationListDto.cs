using Explore.Domain;
using Explore.Domain.Enums;
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
        public string Postcode { get; set; }
        public string Address { get; set; }
        public int ApprovalStatusId { get; set; }
        public string ApprovalStatusFullName { get; set; }
        public string StatusTypeFullName => ApprovalStatusFullName; // Alias for backward compatibility
        public DateTime CreatedAt { get; set; }
        public OrganizationRoleEnum? CurrentUserRole { get; set; }
    }
}
