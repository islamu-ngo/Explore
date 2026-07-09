// ABOUTME: Provides safe default control-plane services until a Blazor host registers real adapters.
// ABOUTME: Fails closed with explicit not-configured results instead of inventing local authority or transport behavior.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

internal sealed class UnconfiguredControlPlaneClient :
    IControlPlaneOverviewService,
    IControlPlaneTenantService,
    IControlPlaneDomainService,
    IControlPlaneOperationsService,
    IControlPlanePlanService,
    IControlPlaneTenantConfigurationService
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

    public Task<ControlPlaneResult<ControlPlaneTenantPlanList>> GetPlansAsync(
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantPlanList>();

    public Task<ControlPlaneResult<ControlPlaneTenantPlanDetail>> GetPlanAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantPlanDetail>();

    public Task<ControlPlaneCommandResult> CreatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> CreatePlanVersionDraftAsync(
        string key,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> UpdatePlanVersionDraftAsync(
        Guid versionId,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> PublishPlanVersionAsync(
        Guid versionId,
        ControlPlaneTenantPlanExistingAssignmentPolicy existingTenantPolicy,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ArchivePlanVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ClonePlanAsync(
        Guid sourceVersionId,
        string key,
        string name,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneResult<ControlPlaneTenantPlanValidationResult>> ValidatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantPlanValidationResult>();

    public Task<ControlPlaneResult<ControlPlaneTenantPlanDiffResult>> PreviewPlanDiffAsync(
        ControlPlaneTenantPlanEffectiveConfiguration current,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantPlanDiffResult>();

    public Task<ControlPlaneResult<ControlPlaneTenantPlanAssignment>> GetTenantPlanAssignmentAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantPlanAssignment>();

    public Task<ControlPlaneResult<ControlPlaneTenantEffectiveConfiguration>> GetEffectiveConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        ReadNotConfiguredAsync<ControlPlaneTenantEffectiveConfiguration>();

    public Task<ControlPlaneCommandResult> SetSettingAsync(
        Guid tenantId,
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> LockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> UnlockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> SwitchTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid tenantPlanVersionId,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> ApplyTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default) =>
        CommandNotConfiguredAsync();

    public Task<ControlPlaneCommandResult> RollbackTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
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

    private static Task<ControlPlaneResult<T>> ReadNotConfiguredAsync<T>() =>
        Task.FromResult(ControlPlaneResult.Failure<T>(
            ControlPlaneResultKind.NotConfigured,
            Problem));

    private static Task<ControlPlaneCommandResult> CommandNotConfiguredAsync() =>
        Task.FromResult(ControlPlaneCommandResult.Failed(
            "The control-plane API adapter is not configured for this host.",
            "control_plane_adapter_not_configured"));
}
