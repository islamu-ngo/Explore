// ABOUTME: DTO for instance-level authorization provider configuration managed during setup and admin UI.
// ABOUTME: Represents the chosen authorization provider (Cerbos or Local) and Cerbos gRPC endpoint.

namespace Explore.Application.DTOs.Onboarding;

public class AuthorizationProviderConfigurationDto
{
    /// <summary>
    /// The authorization provider: "cerbos" for external Cerbos PDP, "local" for built-in RBAC.
    /// </summary>
    public string Provider { get; set; } = "local";

    /// <summary>
    /// The Cerbos gRPC endpoint (e.g., "https://cerbosgrpc.openislamu.org:443").
    /// Only required when Provider is "cerbos".
    /// </summary>
    public string CerbosGrpcEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Whether the Cerbos gRPC endpoint was detected from environment variables (not manually entered).
    /// When true, the UI locks the endpoint field and shows an auto-detected chip.
    /// </summary>
    public bool CerbosDetectedFromEnvironment { get; set; }

    /// <summary>
    /// Whether the Cerbos gRPC endpoint was verified reachable via health check.
    /// </summary>
    public bool CerbosEndpointVerified { get; set; }
}
