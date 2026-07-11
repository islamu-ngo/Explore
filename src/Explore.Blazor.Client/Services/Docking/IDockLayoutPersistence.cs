// ABOUTME: Persistence boundary for saving and loading dock layout snapshots by stable layout key.
// ABOUTME: Keeps browser storage behind an interface so future server-backed preferences do not leak into UI state.

namespace Explore.Blazor.Client.Services.Docking;

public interface IDockLayoutPersistence
{
    Task<DockLayoutSnapshot?> LoadAsync(string layoutKey, CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(DockLayoutSnapshot snapshot, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string layoutKey, CancellationToken cancellationToken = default);
}
