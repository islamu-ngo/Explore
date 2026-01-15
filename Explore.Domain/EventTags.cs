using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class EventTags : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event { get; set; }

        [ForeignKey("Tag")]
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
