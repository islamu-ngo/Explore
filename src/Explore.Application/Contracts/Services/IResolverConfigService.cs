// ABOUTME: Contract for reading and writing tenant resolver configuration from system settings only.
// ABOUTME: Keeps tenant resolution bootstrapping independent from tenant-aware settings cascades.

using Explore.Application.DTOs.Instance;
using Explore.Application.DTOs.Onboarding;

namespace Explore.Application.Contracts.Services;

public interface IResolverConfigService
{
    Task<ResolverConfigurationDto> GetConfigurationAsync(CancellationToken cancellationToken = default);

    Task ApplyConfigurationAsync(
        PatchResolverConfigurationDto patch,
        ResolverConfigurationDto configuration,
        Guid? actorUserId,
        CancellationToken cancellationToken = default);

    void InvalidateCache();
}
