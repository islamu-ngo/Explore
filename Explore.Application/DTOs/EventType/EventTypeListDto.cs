using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.EventType
{
    public class EventTypeListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string? Description { get; set; }
    }
}
