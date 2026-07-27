using System;

// ABOUTME: Wrapper DTO for partial tenant metadata updates using nullable property groups.
// ABOUTME: Lifecycle state is excluded because dedicated control-plane actions own status transitions.

namespace Explore.Application.DTOs.Tenant;

public class UpdateTenantDto
{
    public UpdateTenantFullNameDto? FullName { get; set; }
    public UpdateTenantSlugDto? Slug { get; set; }
}

public class UpdateTenantFullNameDto
{
    public required string Value { get; set; }
}

public class UpdateTenantSlugDto
{
    public required string Value { get; set; }
}
