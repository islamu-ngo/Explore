// ABOUTME: Defines the host-provided tenant service contract for shared control-plane components.
// ABOUTME: Keeps tenant lifecycle UI independent from generated clients, BFF hosts, and local authorization checks.

using Event.ControlPlane.Client.Contracts;

namespace Event.ControlPlane.Client.Services;

public interface IControlPlaneTenantService
{
    Task<ControlPlaneResult<ControlPlaneTenantList>> GetTenantsAsync(
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ActivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> SuspendTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ArchiveTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ReactivateTenantAsync(
        Guid tenantId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ControlPlaneCommandResult> ScheduleTenantPurgeAsync(
        Guid tenantId,
        string reason,
        string confirmationText,
        CancellationToken cancellationToken = default);
}
