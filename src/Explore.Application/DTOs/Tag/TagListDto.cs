using System;

namespace Explore.Application.DTOs.Tag;

public sealed record TagListDto
{
    public Guid Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
}
