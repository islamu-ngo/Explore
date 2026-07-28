// ABOUTME: Stable lookup row for the permitted capacity behavior of an event-owned pool.
// ABOUTME: Preserves an explicit future inventory policy rather than implicit capacity semantics.

namespace Explore.Domain;

public sealed class CapacityOversellPolicy
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
