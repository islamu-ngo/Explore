// ABOUTME: Explicit runtime presentation modes for dock panels.
// ABOUTME: Models persistent, temporary, inspector, and collapsed behavior without boolean ambiguity.

namespace Explore.Blazor.Client.Services.Docking;

public enum DockMode
{
    Docked,
    Overlay,
    Temporary,
    Inspector,
    Collapsed
}
