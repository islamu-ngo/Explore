// ABOUTME: Safe Application-layer exception for unavailable authorization policy package assets.
// ABOUTME: Lets API controllers return ProblemDetails without leaking deployment filesystem paths.

namespace Explore.Application.Authorization;

/// <summary>
/// Raised when the bundled or mounted authorization policy package cannot be read in this deployment.
/// </summary>
public sealed class PolicyPackageUnavailableException : InvalidOperationException
{
    public PolicyPackageUnavailableException(string message)
        : base(message)
    {
    }
}
