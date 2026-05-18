// ABOUTME: DTO for instance-level authorization provider configuration managed during setup and admin UI.
// ABOUTME: Represents the chosen authorization provider plus redacted Cerbos runtime/Admin API endpoints.

using System.Text.Json.Serialization;

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
    /// The Cerbos Admin API endpoint used for policy package publishing.
    /// Only required when Cerbos Admin API publishing is enabled.
    /// </summary>
    public string CerbosAdminEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Write-only Admin API username. Read models return null and use <see cref="CerbosAdminUsernameConfigured" />.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CerbosAdminUsername { get; set; }

    /// <summary>
    /// Write-only Admin API password. Read models return null and use <see cref="CerbosAdminPasswordConfigured" />.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CerbosAdminPassword { get; set; }

    /// <summary>
    /// Whether an Admin API username is stored. The username itself is not returned on reads.
    /// </summary>
    public bool CerbosAdminUsernameConfigured { get; set; }

    /// <summary>
    /// Whether an Admin API password is stored. The password itself is never returned on reads.
    /// </summary>
    public bool CerbosAdminPasswordConfigured { get; set; }

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
