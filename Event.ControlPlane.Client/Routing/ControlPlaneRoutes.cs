// ABOUTME: Defines canonical Event control-plane route paths under the instance administration root.
// ABOUTME: Prevents embedded and separate control-plane hosts from drifting on shared route strings.

namespace Event.ControlPlane.Client.Routing;

public static class ControlPlaneRoutes
{
    public const string Root = "/admin/instance";
    public const string Overview = Root;
    public const string Tenants = Root + "/tenants";
    public const string Domains = Root + "/domains";
    public const string Onboarding = Root + "/onboarding";
    public const string Health = Root + "/health";
    public const string Storage = Root + "/storage";
    public const string Jobs = Root + "/jobs";
    public const string Security = Root + "/security";
    public const string Policies = Root + "/policies";
}
