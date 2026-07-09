// ABOUTME: Defines host-neutral tenant-plan service operations for Control Plane components.
// ABOUTME: Keeps plan governance UI flows behind HAL-aware adapters instead of generated clients.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlanePlanService
{
    Task<ControlPlaneResult<ControlPlaneTenantPlanList>> GetPlansAsync(
        CancellationToken cancellationToken = default);

    Task<ControlPlaneResult<ControlPlaneTenantPlanDetail>> GetPlanAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> CreatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> CreatePlanVersionDraftAsync(
        string key,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> UpdatePlanVersionDraftAsync(
        Guid versionId,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> PublishPlanVersionAsync(
        Guid versionId,
        ControlPlaneTenantPlanExistingAssignmentPolicy existingTenantPolicy,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ArchivePlanVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ClonePlanAsync(
        Guid sourceVersionId,
        string key,
        string name,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneResult<ControlPlaneTenantPlanValidationResult>> ValidatePlanDraftAsync(
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneResult<ControlPlaneTenantPlanDiffResult>> PreviewPlanDiffAsync(
        ControlPlaneTenantPlanEffectiveConfiguration current,
        ControlPlaneTenantPlanDraft draft,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneResult<ControlPlaneTenantPlanAssignment>> GetTenantPlanAssignmentAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> SwitchTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid tenantPlanVersionId,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ApplyTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> RollbackTenantPlanAssignmentAsync(
        Guid tenantId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}
