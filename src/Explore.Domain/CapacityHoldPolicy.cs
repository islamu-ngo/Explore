// ABOUTME: Normalized lookup row for ticket-capacity reservation policies.
// ABOUTME: Keeps hold timing and full-capacity behavior explicit and independently configurable.

namespace Explore.Domain;

public sealed class CapacityHoldPolicy
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
