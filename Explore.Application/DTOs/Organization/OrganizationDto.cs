using System;

namespace Explore.Application.DTOs.Organization
{
    public class OrganizationDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string? WebsiteUrl { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Postcode { get; set; }
        public string Address { get; set; }
        
        // Approval Status
        public int ApprovalStatusId { get; set; }
        public string? ApprovalStatusFullName { get; set; }
        public string? ApprovalStatusMasterCode { get; set; }
        
        // Tenant
        public Guid TenantId { get; set; }
        public string? TenantFullName { get; set; }
        
        // Actor
        public Guid? ActorId { get; set; }
        public string? ActorDisplayName { get; set; }
        public string? ActorHandle { get; set; }
    }
}
