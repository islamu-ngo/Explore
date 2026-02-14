// ABOUTME: Unified role entity representing all role types across Platform, Tenant, and Organization scopes.
// ABOUTME: Replaces OrganizationRole, TenantAdministratorRole, and UserRole with a single Scope-discriminated table.

using System;
using System.Collections.Generic;
using System.Text;
using Explore.Domain.Enums;

namespace Explore.Domain;

public class Role
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// The scope at which this role applies: Platform, Tenant, or Organization.
    /// </summary>
    public RoleScopeEnum Scope { get; set; }

    /// <summary>
    /// Prevents deletion of built-in system roles.
    /// Custom roles created by admins have IsSystem = false.
    /// </summary>
    public bool IsSystem { get; set; }
}
