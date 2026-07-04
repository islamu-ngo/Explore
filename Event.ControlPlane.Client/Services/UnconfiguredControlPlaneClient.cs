// ABOUTME: Provides safe default control-plane services until a Blazor host registers real adapters.
// ABOUTME: Fails closed with explicit not-configured results instead of inventing local authority or transport behavior.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

internal sealed class UnconfiguredControlPlaneClient :
    IControlPlaneOverviewService,
    IControlPlaneTenantService,
    IControlPlaneDomainService
{
    private static readonly ControlPlaneProblem Problem = new(
        "control_plane_adapter_not_configured",
        "The control-plane API adapter is not configured for this host.");

    public Task<ControlPlaneResult<ControlPlaneOverview>> GetOverviewAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneOverview>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    public Task<ControlPlaneResult<ControlPlaneTenantList>> GetTenantsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneTenantList>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    public Task<ControlPlaneResult<ControlPlaneDomainList>> GetDomainsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneDomainList>(
            ControlPlaneResultKind.NotConfigured,
            Problem));
}
