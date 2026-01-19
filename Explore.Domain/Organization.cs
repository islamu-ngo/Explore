using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class Organization : ITenantEntity, IAuditableEntity, ISoftDeletable
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string Postcode { get; set; }
        public string? WebsiteUrl { get; set; }

        [ForeignKey("ApprovalStatus")]
        public int ApprovalStatusId { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [ForeignKey("Actor")]
        public Guid? ActorId { get; set; }
        public Actor? Actor { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }

        // Soft delete fields
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid? DeletedBy { get; set; }

        // Navigation property for members
        public ICollection<OrganizationMember> Members { get; set; }
    }
}
