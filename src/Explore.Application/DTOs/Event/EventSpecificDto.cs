using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event;

public class EventSpecificDto
{
    public int EventTypeId { get; set; }
    public required string EventTypeFullName { get; set; }
}
