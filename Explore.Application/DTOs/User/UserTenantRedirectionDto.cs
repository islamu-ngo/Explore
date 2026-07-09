// ABOUTME: DTO representing tenant redirection targets for a global user context.
// ABOUTME: Provides resolved tenant ID, slug, and multi-tenant status to guide root client routing.

using System;

namespace Explore.Application.DTOs.User;

public class UserTenantRedirectionDto
{
    public Guid? TenantId { get; set; }
    public string? TenantSlug { get; set; }
    public bool HasMultipleTenants { get; set; }
}
