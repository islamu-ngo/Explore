using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class EventSessionSpeaker
    {
        public Guid Id { get; set; }

        [ForeignKey("Actor")]
        public Guid ActorId { get; set; }
        public Actor Actor { get; set; }

        [ForeignKey("EventSession")]
        public Guid EventSessionId { get; set; }
        public EventSession EventSession { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
