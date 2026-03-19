// ABOUTME: Typed policy for module enablement — controls which optional modules are active.
// ABOUTME: Module toggles are instance-level only; tenants cannot override module availability.

namespace Explore.Domain.Policies;

public sealed class ModulePolicy
{
    public PolicySlot<bool> EnableIslamicModule { get; set; } = new(true, ChildOverrideMode.Deny);
    public PolicySlot<bool> EnableTechModule { get; set; } = new(true, ChildOverrideMode.Deny);
}
