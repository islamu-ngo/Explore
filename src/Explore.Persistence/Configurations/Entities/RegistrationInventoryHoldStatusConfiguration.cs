// ABOUTME: EF mapping for stable registration inventory-hold status lookup rows.
// ABOUTME: Enforces runtime-seeded IDs, codes, and display metadata.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationInventoryHoldStatusConfiguration : LookupConfiguration<RegistrationInventoryHoldStatus>
{
    protected override string TableName => "registration_inventory_hold_statuses";
}
