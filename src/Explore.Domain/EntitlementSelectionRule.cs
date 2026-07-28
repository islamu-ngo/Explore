// ABOUTME: Stable lookup row for how a ticket holder selects entitled schedule items.
// ABOUTME: Keeps inclusion and bounded-choice semantics explicit in persisted ticket catalogs.

namespace Explore.Domain;

public sealed class EntitlementSelectionRule
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
