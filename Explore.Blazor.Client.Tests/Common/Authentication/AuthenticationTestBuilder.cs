using System.Security.Claims;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Tests.Common.Authentication;

/// <summary>
/// Enterprise-grade builder pattern for configuring authentication test scenarios.
/// Follows the Fluent Builder pattern for intuitive test setup.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a type-safe, fluent API for constructing authentication scenarios.
/// It supports all common authentication patterns used in ISLAMU Event:
/// - OIDC/JWT claims (sub, nameidentifier, email, etc.)
/// - Role-based authorization (Admin, OrganizationOwner, etc.)
/// - Policy-based authorization
/// - Multi-tenancy (tenant_id claims)
/// - Custom claims for business logic
/// </para>
/// <para>
/// Usage:
/// <code>
/// var auth = new AuthenticationTestBuilder()
///     .WithUser(userId, "John Doe")
///     .WithEmail("john@example.com")
///     .WithRole("Admin")
///     .WithTenant(tenantId)
///     .Build(ctx);
/// </code>
/// </para>
/// </remarks>
public sealed class AuthenticationTestBuilder
{
    private Guid? _userId;
    private string? _userName;
    private string? _email;
    private Guid? _tenantId;
    private string? _authType;
    private AuthorizationState _authState = AuthorizationState.Authorized;
    private readonly List<string> _roles = new();
    private readonly List<string> _policies = new();
    private readonly List<Claim> _customClaims = new();
    private bool _isAuthenticated = true;
    private bool _isAuthorizing = false;

    /// <summary>
    /// Creates a new authentication test builder.
    /// Default state is authenticated and authorized.
    /// </summary>
    public AuthenticationTestBuilder()
    {
    }

    #region User Identity Configuration

    /// <summary>
    /// Configure the user identity with ID and optional name.
    /// </summary>
    /// <param name="userId">User ID (domain model uses Guid)</param>
    /// <param name="name">Display name for the user</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithUser(Guid userId, string? name = null)
    {
        _userId = userId;
        _userName = name ?? $"TestUser_{userId.ToString()[..8]}";
        _isAuthenticated = true;
        return this;
    }

    /// <summary>
    /// Configure the user's email address.
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    /// <summary>
    /// Configure the authentication type (e.g., "Bearer", "Cookies", "oidc").
    /// </summary>
    /// <param name="authType">Authentication type identifier</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithAuthenticationType(string authType)
    {
        _authType = authType;
        return this;
    }

    #endregion

    #region Authorization Configuration

    /// <summary>
    /// Add a single role to the user.
    /// </summary>
    /// <param name="role">Role name (e.g., "Admin", "OrganizationOwner")</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithRole(string role)
    {
        if (!string.IsNullOrWhiteSpace(role) && !_roles.Contains(role))
        {
            _roles.Add(role);
        }
        return this;
    }

    /// <summary>
    /// Add multiple roles to the user.
    /// </summary>
    /// <param name="roles">Role names</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithRoles(params string[] roles)
    {
        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
        {
            WithRole(role);
        }
        return this;
    }

    /// <summary>
    /// Add a single policy authorization.
    /// </summary>
    /// <param name="policy">Policy name</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithPolicy(string policy)
    {
        if (!string.IsNullOrWhiteSpace(policy) && !_policies.Contains(policy))
        {
            _policies.Add(policy);
        }
        return this;
    }

    /// <summary>
    /// Add multiple policy authorizations.
    /// </summary>
    /// <param name="policies">Policy names</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithPolicies(params string[] policies)
    {
        foreach (var policy in policies.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            WithPolicy(policy);
        }
        return this;
    }

    /// <summary>
    /// Set the authorization state (Authorized or Unauthorized).
    /// </summary>
    /// <param name="state">Authorization state</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithAuthorizationState(AuthorizationState state)
    {
        _authState = state;
        return this;
    }

    #endregion

    #region Multi-Tenancy Configuration

    /// <summary>
    /// Configure tenant ID for multi-tenancy testing.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithTenant(Guid tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    /// <summary>
    /// Configure tenant using the default ISLAMU tenant ID.
    /// </summary>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithDefaultTenant()
    {
        _tenantId = AuthenticationTestConstants.DefaultTenantId;
        return this;
    }

    #endregion

    #region Custom Claims Configuration

    /// <summary>
    /// Add a custom claim.
    /// </summary>
    /// <param name="type">Claim type</param>
    /// <param name="value">Claim value</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithClaim(string type, string value)
    {
        _customClaims.Add(new Claim(type, value));
        return this;
    }

    /// <summary>
    /// Add multiple custom claims.
    /// </summary>
    /// <param name="claims">Claims to add</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithClaims(params Claim[] claims)
    {
        _customClaims.AddRange(claims);
        return this;
    }

    /// <summary>
    /// Add organization-specific claims for testing organization-scoped operations.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="role">Role within the organization</param>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder WithOrganizationClaim(Guid organizationId, string role = "Member")
    {
        _customClaims.Add(new Claim("org_id", organizationId.ToString()));
        _customClaims.Add(new Claim("org_role", role));
        return this;
    }

    #endregion

    #region Authentication State Configuration

    /// <summary>
    /// Configure as anonymous (unauthenticated) user.
    /// </summary>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder AsAnonymous()
    {
        _isAuthenticated = false;
        _isAuthorizing = false;
        _userId = null;
        _userName = null;
        _roles.Clear();
        _policies.Clear();
        _customClaims.Clear();
        return this;
    }

    /// <summary>
    /// Configure as authorizing state (authentication in progress).
    /// </summary>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder AsAuthorizing()
    {
        _isAuthorizing = true;
        return this;
    }

    /// <summary>
    /// Configure as authenticated but unauthorized.
    /// </summary>
    /// <returns>Builder for chaining</returns>
    public AuthenticationTestBuilder AsAuthenticatedButUnauthorized()
    {
        _isAuthenticated = true;
        _authState = AuthorizationState.Unauthorized;
        return this;
    }

    #endregion

    #region Build Methods

    /// <summary>
    /// Build and apply the authentication configuration to a BUnit TestContext.
    /// </summary>
    /// <param name="context">BUnit test context</param>
    /// <returns>TestAuthorizationContext for further manipulation if needed</returns>
    public TestAuthorizationContext Build(Bunit.TestContext context)
    {
        var authContext = context.AddTestAuthorization();
        ApplyTo(authContext);
        return authContext;
    }

    /// <summary>
    /// Apply the authentication configuration to an existing TestAuthorizationContext.
    /// </summary>
    /// <param name="authContext">Target authorization context</param>
    public void ApplyTo(TestAuthorizationContext authContext)
    {
        if (_isAuthorizing)
        {
            authContext.SetAuthorizing();
            return;
        }

        if (!_isAuthenticated)
        {
            authContext.SetNotAuthorized();
            return;
        }

        // Build claims
        var claims = BuildClaims();

        // Set authorized state with name
        authContext.SetAuthorized(_userName ?? "TestUser", _authState);

        // Set claims
        if (claims.Count > 0)
        {
            authContext.SetClaims(claims.ToArray());
        }

        // Set roles
        if (_roles.Count > 0)
        {
            authContext.SetRoles(_roles.ToArray());
        }

        // Set policies
        if (_policies.Count > 0)
        {
            authContext.SetPolicies(_policies.ToArray());
        }
    }

    /// <summary>
    /// Build claims list from configuration.
    /// </summary>
    private List<Claim> BuildClaims()
    {
        var claims = new List<Claim>();

        // Add user ID claims (multiple formats for compatibility)
        if (_userId.HasValue)
        {
            var userIdString = _userId.Value.ToString();
            claims.Add(new Claim("sub", userIdString));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdString));
        }

        // Add name claim
        if (!string.IsNullOrEmpty(_userName))
        {
            claims.Add(new Claim(ClaimTypes.Name, _userName));
        }

        // Add email claim
        if (!string.IsNullOrEmpty(_email))
        {
            claims.Add(new Claim(ClaimTypes.Email, _email));
        }

        // Add tenant claim
        if (_tenantId.HasValue)
        {
            claims.Add(new Claim("tenant_id", _tenantId.Value.ToString()));
        }

        // Add role claims (in addition to SetRoles)
        foreach (var role in _roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role)); // Some systems use "role" instead of ClaimTypes.Role
        }

        // Add custom claims
        claims.AddRange(_customClaims);

        return claims;
    }

    /// <summary>
    /// Create a ClaimsPrincipal from the current configuration.
    /// Useful for testing services directly without rendering components.
    /// </summary>
    /// <returns>ClaimsPrincipal with configured claims</returns>
    public ClaimsPrincipal BuildPrincipal()
    {
        if (!_isAuthenticated)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = BuildClaims();
        var identity = new ClaimsIdentity(claims, _authType ?? "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Create an AuthenticationState from the current configuration.
    /// Useful for mocking AuthenticationStateProvider.
    /// </summary>
    /// <returns>AuthenticationState with configured principal</returns>
    public AuthenticationState BuildAuthenticationState()
    {
        return new AuthenticationState(BuildPrincipal());
    }

    #endregion
}
