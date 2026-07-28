// ABOUTME: Service contract for managing tenant-level storage administration.
// ABOUTME: Reads effective policy and applies tenant overrides under instance delegation constraints.

using Explore.Application.DTOs.Onboarding;
using Explore.Application.DTOs.Tenant;

namespace Explore.Application.Contracts.Services;

public interface ITenantStorageSettingService
{
    Task<TenantStorageSettingsDto> ReadSettingsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task ApplyPatchAsync(
        Guid tenantId,
        Guid actorUserId,
        PatchTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default);

    Task<InstanceStorageProviderStatusDto> TestProviderAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
