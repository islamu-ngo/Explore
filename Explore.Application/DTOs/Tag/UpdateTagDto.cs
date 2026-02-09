using System;

namespace Explore.Application.DTOs.Tag;

public class UpdateTagDto
{
    public Guid Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public Guid TenantId { get; set; }
}
