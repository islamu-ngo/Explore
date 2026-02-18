namespace Explore.Blazor.Client.Tests.Common.Authentication;

/// <summary>
/// Constants used throughout authentication testing.
/// Centralized location for test user IDs, tenant IDs, and other authentication-related constants.
/// </summary>
/// <remarks>
/// These constants match the patterns used in ISLAMU Event's authentication system:
/// - Keycloak OIDC for identity
/// - JWT tokens with standard claims
/// - Multi-tenant support with tenant_id claim
/// </remarks>
public static class AuthenticationTestConstants
{
    #region Default Test IDs

    /// <summary>
    /// Default tenant ID used for single-tenant testing.
    /// Matches the default in TenantConfiguration.
    /// </summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

    /// <summary>
    /// Secondary tenant ID for multi-tenant testing scenarios.
    /// </summary>
    public static readonly Guid SecondaryTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000002");

    /// <summary>
    /// Default test user ID.
    /// </summary>
    public static readonly Guid DefaultUserId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    /// <summary>
    /// Secondary test user ID for multi-user testing scenarios.
    /// </summary>
    public static readonly Guid SecondaryUserId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");

    /// <summary>
    /// Admin test user ID.
    /// </summary>
    public static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Organization owner test user ID.
    /// </summary>
    public static readonly Guid OrganizationOwnerUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    #endregion

    #region Test User Names

    /// <summary>
    /// Default test user name.
    /// </summary>
    public const string DefaultUserName = "Test User";

    /// <summary>
    /// Admin test user name.
    /// </summary>
    public const string AdminUserName = "Admin User";

    /// <summary>
    /// Organization owner test user name.
    /// </summary>
    public const string OrganizationOwnerName = "Org Owner";

    #endregion

    #region Test Emails

    /// <summary>
    /// Default test email.
    /// </summary>
    public const string DefaultEmail = "test@example.com";

    /// <summary>
    /// Admin test email.
    /// </summary>
    public const string AdminEmail = "admin@islamu.org";

    #endregion

    #region Role Names

    /// <summary>
    /// Admin role name.
    /// </summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// Organization Owner role name.
    /// </summary>
    public const string OrganizationOwnerRole = "OrganizationOwner";

    /// <summary>
    /// Organization Admin role name.
    /// </summary>
    public const string OrganizationAdminRole = "OrganizationAdmin";

    /// <summary>
    /// Organization Member role name.
    /// </summary>
    public const string OrganizationMemberRole = "OrganizationMember";

    /// <summary>
    /// User role name (default authenticated user).
    /// </summary>
    public const string UserRole = "User";

    /// <summary>
    /// Event Organizer role name.
    /// </summary>
    public const string EventOrganizerRole = "EventOrganizer";

    #endregion

    #region Policy Names

    /// <summary>
    /// Policy for admin access.
    /// </summary>
    public const string AdminPolicy = "RequireAdmin";

    /// <summary>
    /// Policy for organization management.
    /// </summary>
    public const string ManageOrganizationPolicy = "CanManageOrganization";

    /// <summary>
    /// Policy for event creation.
    /// </summary>
    public const string CreateEventPolicy = "CanCreateEvent";

    /// <summary>
    /// Policy for content moderation.
    /// </summary>
    public const string ContentModerationPolicy = "CanModerateContent";

    #endregion

    #region Claim Types

    /// <summary>
    /// Custom claim type for organization ID.
    /// </summary>
    public const string OrganizationIdClaim = "org_id";

    /// <summary>
    /// Custom claim type for organization role.
    /// </summary>
    public const string OrganizationRoleClaim = "org_role";

    /// <summary>
    /// Tenant ID claim type.
    /// </summary>
    public const string TenantIdClaim = "tenant_id";

    /// <summary>
    /// OIDC subject claim type.
    /// </summary>
    public const string SubjectClaim = "sub";

    #endregion

    #region Authentication Types

    /// <summary>
    /// Default authentication type for tests.
    /// </summary>
    public const string DefaultAuthType = "TestAuth";

    /// <summary>
    /// Bearer token authentication type.
    /// </summary>
    public const string BearerAuthType = "Bearer";

    /// <summary>
    /// OIDC authentication type.
    /// </summary>
    public const string OidcAuthType = "oidc";

    #endregion
}
