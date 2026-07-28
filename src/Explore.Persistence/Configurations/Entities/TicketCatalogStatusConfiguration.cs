// ABOUTME: EF configuration for ticket catalog lifecycle lookup rows.
// ABOUTME: Uses the shared runtime-seeded lookup mapping contract.

using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public sealed class TicketCatalogStatusConfiguration : LookupConfiguration<TicketCatalogStatus>
{
    protected override string TableName => "ticket_catalog_statuses";
}
