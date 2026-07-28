// ABOUTME: Stable lookup row for the lifecycle state of a ticket catalog version.
// ABOUTME: Keeps draft, published, and retired identities normalized for persistence and contracts.

namespace Explore.Domain;

public sealed class TicketCatalogStatus
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
