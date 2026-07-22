// ABOUTME: Immutable metadata for one route-addressable application workspace.
// ABOUTME: Supplies shell labels, icons, canonical routes, authentication posture, availability policy, and optional navigation provider.

namespace Explore.Blazor.Client.Services.Shell;

using Explore.Blazor.Client.Clients;

public sealed record WorkspaceDescriptor(
    WorkspaceKey Key,
    string LabelKey,
    string Icon,
    string BaseRoute,
    bool RequiresAuthentication,
    Func<WorkspaceAvailabilityDto?, bool>? AvailabilityPolicy = null,
    Type? NavigationProviderType = null);
