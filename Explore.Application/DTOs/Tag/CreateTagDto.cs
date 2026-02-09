using System;

namespace Explore.Application.DTOs.Tag;

public class CreateTagDto
{
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid TenantId { get; set; }
}
