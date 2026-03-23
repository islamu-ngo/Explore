// ABOUTME: Machine-readable failure codes for BaseCommandResponse structured error handling.
// ABOUTME: Used by API consumers and UI to branch on specific failure scenarios without string-matching.

namespace Explore.Application.Responses;

/// <summary>
/// Canonical failure codes for <see cref="BaseCommandResponse{TKey}.FailureCode"/>.
/// Null means success or a non-specific failure; these constants identify actionable failure conditions.
/// </summary>
public static class FailureCodes
{
    /// <summary>
    /// Multi-Tenant → Single-Tenant mode switch blocked because more than one active tenant exists.
    /// The UI should direct the user to archive or suspend the extra tenants first.
    /// </summary>
    public const string DeploymentModeChangeBlockedByActiveTenants =
        "DeploymentModeChangeBlockedByActiveTenants";
}
