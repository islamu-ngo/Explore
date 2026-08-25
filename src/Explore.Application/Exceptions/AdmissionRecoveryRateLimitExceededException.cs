// ABOUTME: Represents a bounded admission recovery identity budget rejection.
// ABOUTME: Carries only retry timing and never identity, recipient, capability, or digest data.

namespace Explore.Application.Exceptions;

public sealed class AdmissionRecoveryRateLimitExceededException(int retryAfterSeconds) : Exception
{
    public int RetryAfterSeconds { get; } = Math.Max(1, retryAfterSeconds);
}
