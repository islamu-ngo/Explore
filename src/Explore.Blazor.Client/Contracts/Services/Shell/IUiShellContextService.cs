// ABOUTME: Contract for the authenticated UI-shell context service.
// ABOUTME: Caches the generated GetUiShellContextAsync response and invalidates on current-user changes.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Shell;

public interface IUiShellContextService
{
    Task<UiShellContextDto?> GetContextAsync(CancellationToken cancellationToken = default);
    Task<UiShellContextDto?> GetCachedContextAsync(CancellationToken cancellationToken = default);
    void ResetCache();
}
