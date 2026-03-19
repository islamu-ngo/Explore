// ABOUTME: Typed policy for organization governance — verification and self-registration rules.
// ABOUTME: Each field is a PolicySlot allowing instance admins to lock tenant overrides.

namespace Explore.Domain.Policies;

public sealed class OrganizationPolicy
{
    public PolicySlot<bool> RequireVerification { get; set; } = new(true);
    public PolicySlot<bool> AllowTenantToOmitVerification { get; set; } = new(false);
    public PolicySlot<bool> AllowSelfRegistration { get; set; } = new(true);
    public PolicySlot<bool> AllowGroupSelfRegistration { get; set; } = new(true);
}
