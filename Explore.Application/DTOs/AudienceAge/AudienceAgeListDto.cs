using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Application.DTOs.AudienceAge;

public class AudienceAgeListDto
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}
