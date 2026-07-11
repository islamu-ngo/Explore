// ABOUTME: SaaS tenant plan aggregate containing stable tier identity and published versions.
// ABOUTME: Stores plan metadata separately from versioned settings, quotas, and tenant assignments.

using Explore.Domain.Interfaces;

namespace Explore.Domain;

public class TenantPlan : IAuditableEntity
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string DisplayName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<TenantPlanVersion> Versions { get; set; } = [];
    public ICollection<TenantPlanAssignment> Assignments { get; set; } = [];
}
