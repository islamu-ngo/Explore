using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Explore.Domain
{
    public class EventCategories
    {
        public Guid Id { get; set; }
        [ForeignKey("Event")]
        public Guid EventId { get; set; }
        public Event Event{ get; set; }
        [ForeignKey("Category")]
        public Guid CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
