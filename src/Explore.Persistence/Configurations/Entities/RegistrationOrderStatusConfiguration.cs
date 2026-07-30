// ABOUTME: EF mapping for stable registration-order status lookup rows.
// ABOUTME: Enforces runtime-seeded IDs, codes, and display metadata.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class RegistrationOrderStatusConfiguration : LookupConfiguration<RegistrationOrderStatus>
{
    protected override string TableName => "registration_order_statuses";
}
