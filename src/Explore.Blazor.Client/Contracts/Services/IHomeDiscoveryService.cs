// ABOUTME: Frontend contract for loading and selecting tenant-aware public home discovery context.
// ABOUTME: Keeps browser origin transient by exposing only coarse-area selection to API and preference calls.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IHomeDiscoveryService
{
    Task<HomeDiscoveryDto?> LoadAsync(
        Guid? urlAreaId,
        string? urlMode,
        CancellationToken cancellationToken = default);

    Task<HomeDiscoveryDto?> SelectAreaAsync(
        Guid areaId,
        CancellationToken cancellationToken = default);

    Task<HomeDiscoveryDto?> SelectOnlineAsync(
        Guid? preservedAreaId,
        CancellationToken cancellationToken = default);

    PublicDiscoveryAreaDto? FindClosestArea(
        IEnumerable<PublicDiscoveryAreaDto> areas,
        double latitude,
        double longitude);
}
