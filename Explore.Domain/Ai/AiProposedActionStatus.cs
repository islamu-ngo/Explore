// ABOUTME: Defines lifecycle states for AI-proposed actions that require human confirmation.
// ABOUTME: Prevents provider output from becoming a side effect without explicit state transitions.

namespace Explore.Domain.Ai;

public enum AiProposedActionStatus
{
    Proposed = 1,
    Confirmed = 2,
    Rejected = 3,
    Executed = 4,
    Failed = 5
}
