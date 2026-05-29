// ABOUTME: Defines lifecycle states for provider calls made on behalf of AI conversations.
// ABOUTME: Supports queued, in-flight, completed, failed, and cancelled run auditing.

namespace Explore.Domain.Ai;

public enum AiRunStatus
{
    Queued = 1,
    InProgress = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
