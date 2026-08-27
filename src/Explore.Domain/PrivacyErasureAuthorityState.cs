// ABOUTME: Represents the PII-free high-water and retained-floor state of the erasure authority.
// ABOUTME: Rejects negative or inverted watermarks before replay or maintenance performs I/O.

namespace Explore.Domain;

public sealed record PrivacyErasureAuthorityState
{
    public PrivacyErasureAuthorityState(long highWaterSequence, long retainedFloorSequence)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(highWaterSequence);
        ArgumentOutOfRangeException.ThrowIfNegative(retainedFloorSequence);
        if (retainedFloorSequence > highWaterSequence)
        {
            throw new ArgumentException("The retained floor cannot exceed the authority high-water mark.");
        }

        HighWaterSequence = highWaterSequence;
        RetainedFloorSequence = retainedFloorSequence;
    }

    public long HighWaterSequence { get; }
    public long RetainedFloorSequence { get; }
}
