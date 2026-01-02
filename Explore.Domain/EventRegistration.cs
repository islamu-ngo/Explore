using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class EventRegistration
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event { get; set; }
        [ForeignKey("ApprovalStatus")]
        public int ApprovalStatusId { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
    }
}
