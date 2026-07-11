using System;

namespace Explore.Application.DTOs.Tenant;

public class CreateTenantDto
{
    public required string FullName { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; }
    /// <summary>
    /// When true, the requesting user is automatically added as a tenant administrator of the newly created tenant.
    /// Has no effect if the request is unauthenticated or the tenant.admin role cannot be resolved.
    /// </summary>
    public bool AssignCurrentUserAsTenantAdmin { get; set; }
}
