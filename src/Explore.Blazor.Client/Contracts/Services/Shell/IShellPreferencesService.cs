// ABOUTME: Contract for durable, authority-revalidated shell workspace, actor, and Settings-scope preferences.
// ABOUTME: Keeps persistence orchestration out of MainLayout and Settings components.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Contracts.Services.Shell;

public interface IShellPreferencesService
{
    Task<ShellPreferenceState> LoadAsync(
        UiShellContextDto context,
        CancellationToken cancellationToken = default);

    Task SaveSelectionAsync(
        string workspace,
        Guid? actorId,
        string currentRoute,
        CancellationToken cancellationToken = default);
}
