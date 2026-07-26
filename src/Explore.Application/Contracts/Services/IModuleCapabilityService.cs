// ABOUTME: Contract for managing tenant module capabilities (Guardrail 1: capabilities ≠ settings).
// ABOUTME: Extracted from InstanceGovernanceSettingService to maintain separation of concerns.

namespace Explore.Application.Contracts.Services;

public interface IModuleCapabilityService
{
    Task SyncTenantModuleCapabilitiesAsync(
        Guid tenantId,
        bool enableIslamic,
        bool enableTech,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    Task SyncTenantModuleCapabilityPatchAsync(
        Guid tenantId,
        bool? enableIslamic,
        bool? enableTech,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);
}
