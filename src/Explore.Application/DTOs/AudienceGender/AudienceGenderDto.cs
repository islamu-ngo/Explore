using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.AudienceGender;

public sealed record AudienceGenderDto
{
    public int Id { get; init; }
    public required string MasterCode { get; init; }
    public required string FullName { get; init; }
    public string? Description { get; init; }
}
