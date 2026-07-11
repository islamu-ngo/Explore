// ABOUTME: Adapts control-plane UI service contracts to the generated Event API client.
// ABOUTME: Preserves generated HAL resources and command responses without local model mapping.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.Contracts.Services.ControlPlane;

namespace Explore.Blazor.Client.Services.ControlPlane;

public sealed class ControlPlaneApiAdapter(IEventApiClient apiClient) :
    IControlPlaneOverviewService,
    IControlPlaneTenantService,
    IControlPlaneDomainService,
    IControlPlaneOperationsService,
    IControlPlanePlanCatalogService,
    IControlPlaneTenantConfigurationService
{
    public Task<HalResourceOfControlPlaneOverviewDto> GetOverviewAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneOverviewAsync(cancellationToken: cancellationToken);

    public Task<HalCollectionResourceOfControlPlaneTenantListItemDto> GetTenantsAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneTenantsAsync(cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> CreateTenantAsync(
        CreateTenantDto request,
        CancellationToken cancellationToken = default) =>
        apiClient.CreateControlPlaneTenantAsync(request, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto> ActivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        apiClient.ActivateControlPlaneTenantAsync(
            tenantId,
            body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
            cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto> SuspendTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        apiClient.SuspendControlPlaneTenantAsync(
            tenantId,
            body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
            cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto> ArchiveTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        apiClient.ArchiveControlPlaneTenantAsync(
            tenantId,
            body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
            cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto> ReactivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        apiClient.ReactivateControlPlaneTenantAsync(
            tenantId,
            body: new ControlPlaneTenantLifecycleTransitionRequestDto { Reason = reason },
            cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneTenantLifecycleTransitionDto> ScheduleTenantPurgeAsync(
        Guid tenantId,
        string reason,
        string confirmationText,
        CancellationToken cancellationToken = default) =>
        apiClient.ScheduleControlPlaneTenantPurgeAsync(
            tenantId,
            body: new ControlPlaneTenantLifecycleTransitionRequestDto
            {
                Reason = reason,
                ConfirmationText = confirmationText
            },
            cancellationToken: cancellationToken);

    public Task<HalResourceOfControlPlaneDomainOverviewDto> GetDomainsAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneDomainsAsync(cancellationToken: cancellationToken);

    public Task<HalResourceOfControlPlaneOperationsDto> GetOperationsAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneOperationsAsync(cancellationToken: cancellationToken);

    public Task<HalResourceOfControlPlaneDeploymentModeRunbookDto> GetDeploymentModeRunbookAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneDeploymentModeRunbookAsync(cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfControlPlaneDeploymentModeTransitionDto> TransitionDeploymentModeAsync(
        string targetMode,
        string confirmationText,
        string? reason = null,
        CancellationToken cancellationToken = default) =>
        apiClient.TransitionControlPlaneDeploymentModeAsync(
            body: new ControlPlaneDeploymentModeTransitionRequestDto
            {
                TargetMode = targetMode,
                ConfirmationText = confirmationText,
                Reason = reason
            },
            cancellationToken: cancellationToken);

    public Task<HalCollectionResourceOfControlPlaneTenantPlanListItemDto> GetPlansAsync(
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneTenantPlansAsync(cancellationToken: cancellationToken);

    public Task<HalResourceOfControlPlaneTenantPlanDetailDto> GetPlanAsync(
        string key,
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneTenantPlanByKeyAsync(key, cancellationToken: cancellationToken);

    public Task<HalResourceOfControlPlaneTenantEffectiveConfigurationDto> GetEffectiveConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        apiClient.GetControlPlaneTenantEffectiveConfigurationAsync(tenantId, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> SetSettingAsync(
        Guid tenantId,
        string key,
        string value,
        CancellationToken cancellationToken = default) =>
        apiClient.SetControlPlaneTenantSettingAsync(
            tenantId,
            key,
            new SetControlPlaneTenantSettingRequest { Value = value },
            cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> LockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        apiClient.LockControlPlaneTenantSettingAsync(tenantId, key, cancellationToken: cancellationToken);

    public Task<BaseCommandResponseOfGuid> UnlockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default) =>
        apiClient.UnlockControlPlaneTenantSettingAsync(tenantId, key, cancellationToken: cancellationToken);
}
