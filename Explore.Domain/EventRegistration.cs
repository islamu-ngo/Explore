using System;
using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain
{
    public class EventRegistration : ITenantEntity
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("EventSession")]
        public Guid EventSessionId { get; set; }
        public EventSession EventSession { get; set; }

        [ForeignKey("ApprovalStatus")]
        public int? ApprovalStatusId { get; set; }
        public ApprovalStatus? ApprovalStatus { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }

        [ForeignKey("AtprotoRecord")]
        public Guid? AtprotoRecordId { get; set; }
        public AtprotoRecord? AtprotoRecord { get; set; }
    }
}
