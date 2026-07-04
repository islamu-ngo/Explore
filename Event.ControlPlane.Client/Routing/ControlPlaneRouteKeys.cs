// ABOUTME: Defines stable keys for Event control-plane routes shared across Blazor hosts.
// ABOUTME: Lets navigation, tests, and future service contracts refer to routes without duplicating paths.

namespace Event.ControlPlane.Client.Routing;

public static class ControlPlaneRouteKeys
{
    public const string Overview = "overview";
    public const string Tenants = "tenants";
    public const string Domains = "domains";
    public const string Onboarding = "onboarding";
    public const string Health = "health";
    public const string Storage = "storage";
    public const string Jobs = "jobs";
    public const string Security = "security";
    public const string Policies = "policies";
}
