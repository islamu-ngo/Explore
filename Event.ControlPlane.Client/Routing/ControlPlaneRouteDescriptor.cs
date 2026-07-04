// ABOUTME: Describes a shared Event control-plane route without tying it to a specific Blazor host.
// ABOUTME: Keeps navigation and shell composition data host-neutral for embedded and separate deployments.

namespace Event.ControlPlane.Client.Routing;

public sealed record ControlPlaneRouteDescriptor(string Key, string Path);
