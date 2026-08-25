using System;

namespace Explore.Application.DTOs.Tag;

public sealed record CreateTagDto
{
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
}
