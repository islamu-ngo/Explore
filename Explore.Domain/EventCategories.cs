using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class EventCategories : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event { get; set; }

        [ForeignKey("Category")]
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
