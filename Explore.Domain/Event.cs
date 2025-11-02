using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class Event : Program
    {
        [ForeignKey("EventType")]
        public int EventTypeId { get; set; }
        public EventType EventType { get; set; }
    }
}
