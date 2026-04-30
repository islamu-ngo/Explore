// ABOUTME: Registered dock panel entry combining immutable descriptor, content, and current state.
// ABOUTME: Keeps registry reads simple while preserving descriptor/state separation.

using Microsoft.AspNetCore.Components;

namespace Explore.Blazor.Client.Services.Docking;

public sealed record DockPanelEntry(
    DockPanelDescriptor Descriptor,
    RenderFragment Content,
    DockPanelState State);
