// ABOUTME: Probes the split Blazor BFF's generated-client boundary before serving any onboarding surface.
// ABOUTME: Reuses one downstream API readiness contract for startup validation and health reporting.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.HealthChecks;

public interface IExploreApiReadinessProbe
{
    Task EnsureReadyAsync(CancellationToken cancellationToken = default);
}

public sealed class ExploreApiReadinessProbe(IEventApiClient apiClient) : IExploreApiReadinessProbe
{
    public async Task EnsureReadyAsync(CancellationToken cancellationToken = default)
    {
        _ = await apiClient.GetInstanceResolverConfigurationAsync(
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
