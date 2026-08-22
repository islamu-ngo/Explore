// ABOUTME: Stores bounded, payload-free evidence for rejected Jetstream envelopes at a specific cursor.
// ABOUTME: Supports atomic quarantine without retaining raw content or trusting an invalid cursor as a checkpoint.

namespace Explore.Domain.Federation;

public sealed class AtprotoJetstreamQuarantine
{
    public Guid Id { get; set; }
    public Guid ConsumerStateId { get; set; }

    /// <summary>Jetstream v2 <c>seq</c> of the rejected envelope. See <see cref="AtprotoJetstreamConsumerState.Cursor"/>.</summary>
    public long Cursor { get; set; }
    public required string ReasonCode { get; set; }
    public required string EnvelopeHash { get; set; }
    public string? RecordIdentityHash { get; set; }
    public DateTime EventAt { get; set; }
    public DateTime QuarantinedAt { get; set; }

    public AtprotoJetstreamConsumerState? ConsumerState { get; set; }
}
