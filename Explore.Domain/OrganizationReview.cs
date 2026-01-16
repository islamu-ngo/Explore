using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class OrganizationReview : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("Organization")]
        public Guid OrganizationId { get; set; }
        public Organization Organization { get; set; }

        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event { get; set; } = null!;

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string ReviewerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
