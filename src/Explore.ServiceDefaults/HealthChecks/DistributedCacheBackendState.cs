// ABOUTME: Optional health-state contract for distributed cache wrappers with fallback behavior.
// ABOUTME: Lets readiness checks surface degraded backends without coupling to host-specific cache types.

namespace Explore.ServiceDefaults.HealthChecks;

public interface IDistributedCacheBackendState
{
    string BackendName { get; }

    bool IsConfigured { get; }

    bool IsDegraded { get; }

    string Status { get; }
}
