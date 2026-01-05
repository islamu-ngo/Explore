using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain
{
    public class EventSessionLanguage
    {
        public int Id { get; set; }

        [ForeignKey("EventSession")]
        public Guid EventSessionId { get; set; }
        public EventSession EventSession { get; set; }

        [ForeignKey("Language")]
        public int LanguageId { get; set; }
        public Language Language { get; set; }

        [ForeignKey("Tenant")]
        public Guid TenantId { get; set; }
        public Tenant Tenant { get; set; }
    }
}
