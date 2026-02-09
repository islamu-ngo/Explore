// ABOUTME: Domain entity representing a user role in the system.
// Defines roles that can be assigned to users within a tenant.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Explore.Domain;

public class UserRole
{
    public int Id { get; set; }

    public required string FullName { get; set; }

    public required string MasterCode { get; set; }

    public string? Description { get; set; }

    [ForeignKey("Tenant")]
    public Guid TenantId { get; set; }

    public required Tenant Tenant { get; set; }
}
