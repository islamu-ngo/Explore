using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.AudienceAge;

public sealed record AudienceAgeDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
    public int? MinAge { get; init; }
    public int? MaxAge { get; init; }
}
