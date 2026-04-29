// ABOUTME: Interface for user appearance preferences operations wrapping IEventApiClient.
// ABOUTME: Decouples Blazor components from direct IEventApiClient injection.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IUserAppearancePreferencesService
{
    Task<Explore.Blazor.Client.Services.Appearance.ResolvedAppearanceDto> GetCurrentPreferencesAsync(CancellationToken ct = default);
    Task SetActiveProfileAsync(Explore.Blazor.Client.Services.Appearance.SetActiveProfileRequestDto dto, CancellationToken ct = default);
}
