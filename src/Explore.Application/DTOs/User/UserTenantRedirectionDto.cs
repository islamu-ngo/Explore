// ABOUTME: DTO representing tenant redirection targets for a global user context.
// ABOUTME: Provides resolved tenant ID, slug, and multi-tenant status to guide root client routing.

using System;

namespace Explore.Application.DTOs.User;

public sealed record UserTenantRedirectionDto
{
    public Guid? TenantId { get; init; }
    public string? TenantSlug { get; init; }
    public bool HasMultipleTenants { get; init; }
}
