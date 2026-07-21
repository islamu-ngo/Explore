// ABOUTME: Immutable metadata for one route-addressable application workspace.
// ABOUTME: Supplies shell labels, icons, canonical routes, authentication posture, and optional navigation provider.

namespace Explore.Blazor.Client.Services.Shell;

public sealed record WorkspaceDescriptor(
    WorkspaceKey Key,
    string LabelKey,
    string Icon,
    string BaseRoute,
    bool RequiresAuthentication,
    Type? NavigationProviderType = null);