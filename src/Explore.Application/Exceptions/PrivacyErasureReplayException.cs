// ABOUTME: Defines bounded fail-closed privacy-erasure replay failures for startup and readiness.
// ABOUTME: Carries stable reason codes without identifiers, provider values, or exception details.

namespace Explore.Application.Exceptions;

public class PrivacyErasureReplayException : InvalidOperationException
{
    protected PrivacyErasureReplayException(string reasonCode)
        : base(reasonCode)
    {
        ReasonCode = reasonCode;
    }

    public string ReasonCode { get; }
}

public sealed class StaleRestoreBelowRetainedFloorException()
    : PrivacyErasureReplayException("stale_restore_below_retained_floor");

public sealed class PrivacyErasureSequenceGapException()
    : PrivacyErasureReplayException("sequence_gap_detected");

public sealed class PrivacyErasureCheckpointAheadException()
    : PrivacyErasureReplayException("checkpoint_ahead_of_authority");
