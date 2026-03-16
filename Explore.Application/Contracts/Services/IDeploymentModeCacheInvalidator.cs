// ABOUTME: Contract for invalidating the cached deployment mode after instance onboarding completes.
// ABOUTME: Allows the middleware to pick up the newly persisted SingleTenant/MultiTenant mode without restart.

namespace Explore.Application.Contracts.Services;

/// <summary>
/// Invalidates the cached deployment mode used by tenant resolution middleware.
/// Called after instance onboarding completes to propagate the chosen deployment mode.
/// </summary>
public interface IDeploymentModeCacheInvalidator
{
    void Invalidate();
}
