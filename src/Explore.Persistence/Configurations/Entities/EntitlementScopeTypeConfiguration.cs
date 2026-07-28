// ABOUTME: EF configuration for ticket entitlement scope lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class EntitlementScopeTypeConfiguration : LookupConfiguration<EntitlementScopeType>
{
    protected override string TableName => "entitlement_scope_types";
}
