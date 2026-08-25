using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.Event;

public sealed record EventSpecificDto
{
    public int EventTypeId { get; init; }
    public required string EventTypeFullName { get; init; }
}
