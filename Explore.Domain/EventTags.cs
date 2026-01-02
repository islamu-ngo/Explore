using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class EventTags
    {
        public Guid Id { get; set; }
        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event{ get; set; }
        [ForeignKey("Tag")]
        public Guid TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
