// ABOUTME: Interface for user appearance preferences operations over the generated user-appearance client.
// ABOUTME: Decouples Blazor components from direct generated-client injection.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services;

public interface IUserAppearancePreferencesService
{
    Task<ResolvedAppearanceDto> GetCurrentPreferencesAsync(CancellationToken ct = default);
    Task SetActiveProfileAsync(SetActiveProfileRequestDto dto, CancellationToken ct = default);
}
