// ABOUTME: Provides safe default control-plane services until a Blazor host registers real adapters.
// ABOUTME: Fails closed with explicit not-configured results instead of inventing local authority or transport behavior.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

internal sealed class UnconfiguredControlPlaneClient :
    IControlPlaneOverviewService,
    IControlPlaneTenantService,
    IControlPlaneDomainService,
    IControlPlaneOperationsService
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

    public Task<ControlPlaneCommandResult> ActivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> SuspendTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ArchiveTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ReactivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ScheduleTenantPurgeAsync(
        Guid tenantId,
        string reason,
        string confirmationText,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneResult<ControlPlaneDomainList>> GetDomainsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneDomainList>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    public Task<ControlPlaneResult<ControlPlaneOperations>> GetOperationsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneOperations>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    public Task<ControlPlaneResult<ControlPlaneDeploymentModeRunbook>> GetDeploymentModeRunbookAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ControlPlaneResult.Failure<ControlPlaneDeploymentModeRunbook>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    public Task<ControlPlaneCommandResult> TransitionDeploymentModeAsync(
        string targetMode,
        string confirmationText,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    private static Task<ControlPlaneCommandResult> CommandNotConfiguredAsync() =>
        Task.FromResult(ControlPlaneCommandResult.Failed(
            "The control-plane API adapter is not configured for this host.",
            "control_plane_adapter_not_configured"));
}
