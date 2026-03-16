// ABOUTME: Bridges the Application layer invalidation contract to the API middleware's static cache.
// ABOUTME: Registered as singleton so the onboarding handler can trigger a deployment mode refresh.

using Explore.API.Middleware;
using Explore.Application.Contracts.Services;

namespace Explore.API.Services;

public class DeploymentModeCacheInvalidator : IDeploymentModeCacheInvalidator
{
    public void Invalidate()
    {
        ApiTenantResolutionMiddleware.InvalidateBootstrapCache();
    }
}
