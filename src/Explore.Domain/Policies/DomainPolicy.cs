// ABOUTME: Typed policy for domain configuration — base domain and tenant custom domain permissions.
// ABOUTME: Each field is a PolicySlot allowing instance admins to lock tenant domain overrides.

namespace Explore.Domain.Policies;

public sealed class DomainPolicy
{
    public PolicySlot<string> InstanceBaseDomain { get; set; } = new(string.Empty, ChildOverrideMode.Deny);
    public PolicySlot<bool> AllowTenantCustomDomains { get; set; } = new(true);
    public PolicySlot<bool> LockTenantSubdomain { get; set; } = new(false, ChildOverrideMode.Deny);
    public PolicySlot<bool> LockTenantCustomDomain { get; set; } = new(false, ChildOverrideMode.Deny);
}
