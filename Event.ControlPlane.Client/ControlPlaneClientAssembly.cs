// ABOUTME: Exposes the shared control-plane client assembly for Blazor host route registration.
// ABOUTME: Lets embedded and separate hosts add RCL routable components without referencing host-specific types.

using System.Reflection;

namespace Event.ControlPlane.Client;

public static class ControlPlaneClientAssembly
{
    public static Assembly Value => typeof(ControlPlaneClientAssembly).Assembly;
}
