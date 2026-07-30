// ABOUTME: Normalized lookup row for the lifecycle of a registration inventory hold.
// ABOUTME: Preserves explicit reservation outcomes for later capacity processing.

namespace Explore.Domain;

public sealed class RegistrationInventoryHoldStatus
{
    public int Id { get; set; }

    public string MasterCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Description { get; set; }
}
