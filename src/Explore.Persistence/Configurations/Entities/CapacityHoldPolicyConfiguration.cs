// ABOUTME: EF configuration for capacity-hold timing and waitlist policy lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract for stable policy identifiers.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class CapacityHoldPolicyConfiguration : LookupConfiguration<CapacityHoldPolicy>
{
    protected override string TableName => "capacity_hold_policies";
}
