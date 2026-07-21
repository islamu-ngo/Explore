// ABOUTME: Read-only contract for the application's compile-time workspace catalog.
// ABOUTME: Lets route and shell services consume descriptors without owning registration details.

namespace Explore.Blazor.Client.Services.Shell;

public interface IWorkspaceRegistry
{
    IReadOnlyList<WorkspaceDescriptor> Workspaces { get; }
}
