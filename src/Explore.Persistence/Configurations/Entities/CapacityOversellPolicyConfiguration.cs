// ABOUTME: EF configuration for capacity-pool oversell policy lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class CapacityOversellPolicyConfiguration : LookupConfiguration<CapacityOversellPolicy>
{
    protected override string TableName => "capacity_oversell_policies";
}
