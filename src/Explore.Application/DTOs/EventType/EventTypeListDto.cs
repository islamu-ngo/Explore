using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.EventType;

public sealed record EventTypeListDto
{
    public int Id { get; init; }
    public required string FullName { get; init; }
    public required string MasterCode { get; init; }
    public string? Description { get; init; }
}
