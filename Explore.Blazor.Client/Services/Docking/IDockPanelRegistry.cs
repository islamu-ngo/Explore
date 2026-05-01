// ABOUTME: Registration contract for shell and workspace components that contribute dock panels.
// ABOUTME: Allows panels to be modeled generically without hardcoded workspace or shell enums.

using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Services.Docking;

public interface IDockPanelRegistry
{
    void Register(DockPanelDescriptor descriptor, RenderFragment content);

    void Unregister(DockPanelId id);

    IReadOnlyList<DockPanelEntry> GetPanels(DockScope scope, DockSide side);
}
