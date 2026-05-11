// ABOUTME: Constants for admin authority claim types used in IClaimsTransformation.
// These claims enrich the server principal for BFF/API decisions and must not be serialized as browser authority.

namespace Explore.Application.Authorization;

/// <summary>
/// Defines claim types for admin authority resolved from the database.
/// Used by <c>AdminClaimsTransformation</c> to enrich the server ClaimsPrincipal with admin authority.
/// </summary>
public static class AdminClaimTypes
{
    /// <summary>
    /// Claim type indicating the user is an Instance Administrator.
    /// Value is always "true" when present.
    /// </summary>
    public const string InstanceAdmin = "explore:admin:instance";

    /// <summary>
    /// Claim type for tenant-level admin authority.
    /// Value is the tenant ID (Guid). Multiple claims may exist (one per tenant).
    /// </summary>
    public const string TenantAdmin = "explore:admin:tenant";

    /// <summary>
    /// Claim type for organization-level admin authority (Creator, CoOwner, or Admin role).
    /// Value is the organization ID (Guid). Multiple claims may exist (one per organization).
    /// </summary>
    public const string OrganizationAdmin = "explore:admin:organization";
}
