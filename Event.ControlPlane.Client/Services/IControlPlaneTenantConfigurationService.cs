// ABOUTME: Defines host-neutral tenant configuration read operations for Control Plane components.
// ABOUTME: Keeps tenant effective settings behind adapters that preserve HAL actions and safe errors.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneTenantConfigurationService
{
    Task<ControlPlaneResult<ControlPlaneTenantEffectiveConfiguration>> GetEffectiveConfigurationAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> SetSettingAsync(
        Guid tenantId,
        string key,
        string value,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> LockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> UnlockSettingAsync(
        Guid tenantId,
        string key,
        CancellationToken cancellationToken = default);
}
