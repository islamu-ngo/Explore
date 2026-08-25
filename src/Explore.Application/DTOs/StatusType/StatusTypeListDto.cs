using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.StatusType;

public sealed record StatusTypeListDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
