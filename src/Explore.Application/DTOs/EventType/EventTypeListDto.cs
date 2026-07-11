using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.EventType;

public class EventTypeListDto
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string MasterCode { get; set; }
    public string? Description { get; set; }
}
