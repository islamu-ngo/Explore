// ABOUTME: EF configuration for ticket entitlement selection-rule lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class EntitlementSelectionRuleConfiguration : LookupConfiguration<EntitlementSelectionRule>
{
    protected override string TableName => "entitlement_selection_rules";
}
