using System;

namespace Explore.Application.DTOs.Tenant;

public class TenantListDto
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; }
}
