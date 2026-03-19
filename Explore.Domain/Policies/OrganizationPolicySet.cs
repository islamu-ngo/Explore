// ABOUTME: Organization-level policy aggregate — overrides tenant defaults where allowed.
// ABOUTME: Only fields with ChildOverrideMode.Allow at the tenant level can be set here.

namespace Explore.Domain.Policies;

public sealed class OrganizationPolicySet
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TenantId { get; set; }
    public EventPolicy Events { get; set; } = new();

    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public uint RowVersion { get; set; }
}
