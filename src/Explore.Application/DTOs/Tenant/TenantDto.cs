using System;

namespace Explore.Application.DTOs.Tenant;

public sealed record TenantDto
{
    public Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Slug { get; init; }
    public bool IsActive { get; init; }
}
