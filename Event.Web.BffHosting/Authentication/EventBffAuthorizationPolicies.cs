// ABOUTME: Defines reusable authorization policy names for Event browser-BFF hosts.
// ABOUTME: Gives separate control-plane hosts a stable coarse-grained server-side access gate.

namespace Event.Web.BffHosting.Authentication;

public static class EventBffAuthorizationPolicies
{
    public const string ControlPlaneAccess = "event.control_plane.access";
}
