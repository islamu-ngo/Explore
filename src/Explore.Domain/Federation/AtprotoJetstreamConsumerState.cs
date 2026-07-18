// ABOUTME: Owns the single global Jetstream cursor and its renewable multi-node processing lease.
// ABOUTME: Uses a monotonically increasing fence so expired workers cannot advance canonical state.

namespace Explore.Domain.Federation;

public sealed class AtprotoJetstreamConsumerState
{
    public Guid Id { get; set; }
    public required string Service { get; set; }
    public long Cursor { get; set; }
    public DateTime? LastEventAt { get; set; }
    public string? LeaseOwner { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAt { get; set; }
    public long LeaseFence { get; set; }
    public DateTime UpdatedAt { get; set; }
}
