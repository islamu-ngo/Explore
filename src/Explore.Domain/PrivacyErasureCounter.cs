// ABOUTME: Models the singleton monotonic sequence allocator for the platform privacy-erasure ledger.
// ABOUTME: Keeps allocation state PII-free and exposes only validated forward movement.

namespace Explore.Domain;

public sealed class PrivacyErasureCounter
{
    private PrivacyErasureCounter()
    {
    }

    public bool Singleton { get; private set; }
    public long LastSequence { get; private set; }

    public static PrivacyErasureCounter Start() => new()
    {
        Singleton = true,
        LastSequence = 0
    };

    public long AllocateNext()
    {
        if (LastSequence == long.MaxValue)
        {
            throw new InvalidOperationException("The erasure-ledger sequence is exhausted.");
        }

        return ++LastSequence;
    }

    public void AdvanceTo(long sequence)
    {
        if (sequence != LastSequence + 1)
        {
            throw new InvalidOperationException("Mirrored erasure facts must advance the ledger by one.");
        }

        LastSequence = sequence;
    }
}
