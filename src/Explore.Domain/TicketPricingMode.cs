// ABOUTME: Stable lookup row for the five explicit ticket pricing modes.
// ABOUTME: Normalizes pricing semantics while TicketPricingRules enforces each mode's fields.

namespace Explore.Domain;

public sealed class TicketPricingMode
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
