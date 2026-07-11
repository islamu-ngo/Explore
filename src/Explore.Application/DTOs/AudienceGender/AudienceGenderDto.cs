using System;
using System.Collections.Generic;

namespace Explore.Application.DTOs.AudienceGender;

public class AudienceGenderDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}
