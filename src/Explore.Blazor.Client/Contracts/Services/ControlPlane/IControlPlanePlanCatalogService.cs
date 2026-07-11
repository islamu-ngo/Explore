// ABOUTME: Defines read-only tenant-plan catalog operations for control-plane pages.
// ABOUTME: Returns generated API HAL resources without local plan mirrors.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.ControlPlane;

public interface IControlPlanePlanCatalogService
{
    Task<HalCollectionResourceOfControlPlaneTenantPlanListItemDto> GetPlansAsync(
        CancellationToken cancellationToken = default);

    Task<HalResourceOfControlPlaneTenantPlanDetailDto> GetPlanAsync(
        string key,
        CancellationToken cancellationToken = default);
}
