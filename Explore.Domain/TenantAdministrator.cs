// ABOUTME: Maps users to tenant-level admin functions with explicit tenant administrator roles.
// ABOUTME: Provides clear tenant-scope authorization mapping separate from organization roles.

using System.ComponentModel.DataAnnotations.Schema;
using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TenantAdministrator : ITenantEntity
{
    public Guid Id { get; set; }

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }
    public required User User { get; set; }

    [ForeignKey(nameof(Tenant))]
    public Guid TenantId { get; set; }
    public required Tenant Tenant { get; set; }

    [ForeignKey(nameof(TenantAdministratorRole))]
    public int TenantAdministratorRoleId { get; set; }
    public required TenantAdministratorRole TenantAdministratorRole { get; set; }

    public DateTime GrantedAt { get; set; }
    public Guid? GrantedBy { get; set; }
}
