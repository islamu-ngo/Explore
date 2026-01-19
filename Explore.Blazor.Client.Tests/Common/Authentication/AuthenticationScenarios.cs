namespace Explore.Blazor.Client.Tests.Common.Authentication;

/// <summary>
/// Pre-configured authentication scenarios for common testing patterns.
/// Provides factory methods for frequently used authentication configurations.
/// </summary>
/// <remarks>
/// <para>
/// This class follows the Factory Method pattern to provide standardized
/// authentication configurations. Use these scenarios to ensure consistent
/// testing across the application.
/// </para>
/// <para>
/// Each scenario is designed to match a real-world use case in ISLAMU Event:
/// - Anonymous browsing (public event discovery)
/// - Authenticated user (registered member)
/// - Organization owner (can create events)
/// - Admin (full system access)
/// </para>
/// </remarks>
public static class AuthenticationScenarios
{
    #region Anonymous Scenarios

    /// <summary>
    /// Creates an anonymous (unauthenticated) user scenario.
    /// Use for testing public pages and components.
    /// </summary>
    /// <returns>Builder configured for anonymous access</returns>
    public static AuthenticationTestBuilder Anonymous()
    {
        return new AuthenticationTestBuilder().AsAnonymous();
    }

    /// <summary>
    /// Creates an authorizing (loading) state scenario.
    /// Use for testing loading states during authentication.
    /// </summary>
    /// <returns>Builder configured for authorizing state</returns>
    public static AuthenticationTestBuilder Authorizing()
    {
        return new AuthenticationTestBuilder().AsAuthorizing();
    }

    #endregion

    #region Standard User Scenarios

    /// <summary>
    /// Creates a standard authenticated user scenario.
    /// </summary>
    /// <param name="userId">Optional custom user ID</param>
    /// <param name="name">Optional custom name</param>
    /// <returns>Builder configured for standard user</returns>
    public static AuthenticationTestBuilder AuthenticatedUser(
        Guid? userId = null,
        string? name = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? AuthenticationTestConstants.DefaultUserId, name ?? AuthenticationTestConstants.DefaultUserName)
            .WithEmail(AuthenticationTestConstants.DefaultEmail)
            .WithDefaultTenant()
            .WithRole(AuthenticationTestConstants.UserRole);
    }

    /// <summary>
    /// Creates an authenticated user with full profile claims.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="name">Display name</param>
    /// <param name="email">Email address</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Builder configured with full profile</returns>
    public static AuthenticationTestBuilder AuthenticatedUserWithProfile(
        Guid userId,
        string name,
        string email,
        Guid? tenantId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId, name)
            .WithEmail(email)
            .WithTenant(tenantId ?? AuthenticationTestConstants.DefaultTenantId)
            .WithRole(AuthenticationTestConstants.UserRole);
    }

    /// <summary>
    /// Creates an authenticated but unauthorized user scenario.
    /// Use for testing access denied states.
    /// </summary>
    /// <returns>Builder configured for authenticated but unauthorized</returns>
    public static AuthenticationTestBuilder AuthenticatedButUnauthorized()
    {
        return new AuthenticationTestBuilder()
            .WithUser(AuthenticationTestConstants.DefaultUserId, AuthenticationTestConstants.DefaultUserName)
            .AsAuthenticatedButUnauthorized();
    }

    #endregion

    #region Organization Scenarios

    /// <summary>
    /// Creates an organization owner scenario.
    /// Owners have full control over their organization and can create events.
    /// </summary>
    /// <param name="organizationId">Organization ID the user owns</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for organization owner</returns>
    public static AuthenticationTestBuilder OrganizationOwner(
        Guid organizationId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? AuthenticationTestConstants.OrganizationOwnerUserId, AuthenticationTestConstants.OrganizationOwnerName)
            .WithEmail("owner@organization.com")
            .WithDefaultTenant()
            .WithRole(AuthenticationTestConstants.OrganizationOwnerRole)
            .WithOrganizationClaim(organizationId, "Owner")
            .WithPolicy(AuthenticationTestConstants.ManageOrganizationPolicy)
            .WithPolicy(AuthenticationTestConstants.CreateEventPolicy);
    }

    /// <summary>
    /// Creates an organization admin scenario.
    /// Admins can manage the organization but have fewer permissions than owners.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for organization admin</returns>
    public static AuthenticationTestBuilder OrganizationAdmin(
        Guid organizationId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? Guid.NewGuid(), "Org Admin")
            .WithDefaultTenant()
            .WithRole(AuthenticationTestConstants.OrganizationAdminRole)
            .WithOrganizationClaim(organizationId, "Admin")
            .WithPolicy(AuthenticationTestConstants.CreateEventPolicy);
    }

    /// <summary>
    /// Creates an organization member scenario.
    /// Members have limited access within the organization.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for organization member</returns>
    public static AuthenticationTestBuilder OrganizationMember(
        Guid organizationId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? Guid.NewGuid(), "Org Member")
            .WithDefaultTenant()
            .WithRole(AuthenticationTestConstants.OrganizationMemberRole)
            .WithOrganizationClaim(organizationId, "Member");
    }

    #endregion

    #region Admin Scenarios

    /// <summary>
    /// Creates a system administrator scenario.
    /// Admins have elevated privileges across the system.
    /// </summary>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for system admin</returns>
    public static AuthenticationTestBuilder Admin(Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? AuthenticationTestConstants.AdminUserId, AuthenticationTestConstants.AdminUserName)
            .WithEmail(AuthenticationTestConstants.AdminEmail)
            .WithDefaultTenant()
            .WithRoles(AuthenticationTestConstants.AdminRole, AuthenticationTestConstants.UserRole)
            .WithPolicy(AuthenticationTestConstants.AdminPolicy)
            .WithPolicy(AuthenticationTestConstants.ContentModerationPolicy);
    }

    /// <summary>
    /// Creates a super administrator scenario.
    /// Super admins have full system access including multi-tenant operations.
    /// </summary>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for super admin</returns>
    public static AuthenticationTestBuilder SuperAdmin(Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? AuthenticationTestConstants.AdminUserId, "Super Admin")
            .WithEmail("superadmin@islamu.org")
            .WithDefaultTenant()
            .WithRoles(
                AuthenticationTestConstants.SuperAdminRole,
                AuthenticationTestConstants.AdminRole,
                AuthenticationTestConstants.UserRole)
            .WithPolicies(
                AuthenticationTestConstants.AdminPolicy,
                AuthenticationTestConstants.ManageOrganizationPolicy,
                AuthenticationTestConstants.CreateEventPolicy,
                AuthenticationTestConstants.ContentModerationPolicy);
    }

    #endregion

    #region Event Scenarios

    /// <summary>
    /// Creates an event organizer scenario.
    /// Event organizers can create and manage events.
    /// </summary>
    /// <param name="organizationId">Organization for which the user organizes events</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for event organizer</returns>
    public static AuthenticationTestBuilder EventOrganizer(
        Guid organizationId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? Guid.NewGuid(), "Event Organizer")
            .WithDefaultTenant()
            .WithRoles(AuthenticationTestConstants.EventOrganizerRole, AuthenticationTestConstants.UserRole)
            .WithOrganizationClaim(organizationId, "Organizer")
            .WithPolicy(AuthenticationTestConstants.CreateEventPolicy);
    }

    #endregion

    #region Multi-Tenant Scenarios

    /// <summary>
    /// Creates a user in a specific tenant scenario.
    /// Use for testing multi-tenant isolation.
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for specific tenant</returns>
    public static AuthenticationTestBuilder UserInTenant(
        Guid tenantId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? Guid.NewGuid(), "Tenant User")
            .WithTenant(tenantId)
            .WithRole(AuthenticationTestConstants.UserRole);
    }

    /// <summary>
    /// Creates an admin in a specific tenant scenario.
    /// Use for testing tenant-scoped admin operations.
    /// </summary>
    /// <param name="tenantId">Target tenant ID</param>
    /// <param name="userId">Optional custom user ID</param>
    /// <returns>Builder configured for tenant admin</returns>
    public static AuthenticationTestBuilder AdminInTenant(
        Guid tenantId,
        Guid? userId = null)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId ?? Guid.NewGuid(), "Tenant Admin")
            .WithTenant(tenantId)
            .WithRoles(AuthenticationTestConstants.AdminRole, AuthenticationTestConstants.UserRole)
            .WithPolicy(AuthenticationTestConstants.AdminPolicy);
    }

    #endregion

    #region Custom Scenario Builder

    /// <summary>
    /// Creates a custom scenario starting from a blank state.
    /// Use when none of the predefined scenarios fit your needs.
    /// </summary>
    /// <returns>Empty builder for custom configuration</returns>
    public static AuthenticationTestBuilder Custom()
    {
        return new AuthenticationTestBuilder();
    }

    /// <summary>
    /// Creates a custom authenticated user with specific roles.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="name">User name</param>
    /// <param name="roles">Roles to assign</param>
    /// <returns>Builder configured with specified roles</returns>
    public static AuthenticationTestBuilder CustomWithRoles(
        Guid userId,
        string name,
        params string[] roles)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId, name)
            .WithDefaultTenant()
            .WithRoles(roles);
    }

    /// <summary>
    /// Creates a custom authenticated user with specific policies.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="name">User name</param>
    /// <param name="policies">Policies to authorize</param>
    /// <returns>Builder configured with specified policies</returns>
    public static AuthenticationTestBuilder CustomWithPolicies(
        Guid userId,
        string name,
        params string[] policies)
    {
        return new AuthenticationTestBuilder()
            .WithUser(userId, name)
            .WithDefaultTenant()
            .WithPolicies(policies);
    }

    #endregion
}
