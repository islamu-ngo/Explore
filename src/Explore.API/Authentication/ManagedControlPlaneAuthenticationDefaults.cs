// ABOUTME: Defines the isolated machine-authentication contract for optional Event managed-mode endpoints.
// ABOUTME: Prevents Control Plane credentials from participating in the ordinary JWT or external API-key schemes.

namespace Explore.API.Authentication;

public static class ManagedControlPlaneAuthenticationDefaults
{
    public const string HeaderName = "X-Control-Plane-Key";
    public const string ScopeClaim = "managed_control_plane_scope";
    public const string ManagedInstanceIdClaim = "managed_instance_id";
}

public static class ManagedControlPlaneAuthorizationPolicies
{
    public const string Read = "managed_control_plane_read";
    public const string Write = "managed_control_plane_write";
}
