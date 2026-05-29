// ABOUTME: Machine-readable failure codes for EmailDispatch operator commands.
// ABOUTME: Keeps admin ProblemDetails and HAL affordances aligned with Application-layer transition outcomes.

namespace Explore.Application.Features.EmailDispatch;

public static class EmailDispatchFailureCodes
{
    public const string NotFound = "email_dispatch_not_found";
    public const string InvalidTransition = "email_dispatch_invalid_transition";
    public const string ConcurrentTransition = "email_dispatch_concurrent_transition";
    public const string Misconfigured = "email_dispatch_misconfigured";
}
