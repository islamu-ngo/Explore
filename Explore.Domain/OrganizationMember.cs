using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class OrganizationMember : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("Organization")]
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("OrganizationRole")]
        public int OrganizationRoleId { get; set; }
        public OrganizationRole OrganizationRole { get; set; }

        [ForeignKey("OrganizationPosition")]
        public int? OrganizationPositionId { get; set; }
        public OrganizationPosition? OrganizationPosition { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
