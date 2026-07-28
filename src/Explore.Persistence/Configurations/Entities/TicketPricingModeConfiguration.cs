// ABOUTME: EF configuration for ticket pricing mode lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class TicketPricingModeConfiguration : LookupConfiguration<TicketPricingMode>
{
    protected override string TableName => "ticket_pricing_modes";
}
