using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class Organization
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
    }
}
