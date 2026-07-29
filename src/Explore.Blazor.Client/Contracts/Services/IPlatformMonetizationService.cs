// ABOUTME: Client-facing instance platform monetization settings contract over generated API models.
// ABOUTME: Keeps Razor components behind a scoped service while preserving generated HAL and update DTOs.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IPlatformMonetizationService
{
    Task<HalResourceOfPlatformMonetizationSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> UpdateAsync(UpdatePlatformMonetizationSettingsDto request, CancellationToken cancellationToken = default);
}
