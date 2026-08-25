using System;

// ABOUTME: Wrapper DTO for partial tenant metadata updates using nullable property groups.
// ABOUTME: Lifecycle state is excluded because dedicated control-plane actions own status transitions.

namespace Explore.Application.DTOs.Tenant;

public sealed record UpdateTenantDto
{
    public UpdateTenantFullNameDto? FullName { get; init; }
    public UpdateTenantSlugDto? Slug { get; init; }
}

public sealed record UpdateTenantFullNameDto
{
    public required string Value { get; init; }
}

public sealed record UpdateTenantSlugDto
{
    public required string Value { get; init; }
}
