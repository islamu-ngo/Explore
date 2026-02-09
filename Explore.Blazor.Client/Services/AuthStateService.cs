using System.Security.Claims;
using Explore.Blazor.Client.Configuration;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Client.Services;

/// <summary>
/// Centralized service for authentication state management.
/// Provides secure access to current user ID and tenant ID from JWT claims.
/// </summary>
public interface IAuthStateService
{
    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims.
    /// Throws UnauthorizedAccessException if user is not authenticated.
    /// </summary>
    Task<string> GetCurrentUserIdAsync();

    /// <summary>
    /// Gets the current tenant ID for the authenticated user.
    /// Throws UnauthorizedAccessException if tenant context is not available.
    /// </summary>
    Task<Guid> GetCurrentTenantIdAsync();

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    Task<bool> IsAuthenticatedAsync();
}

public class AuthStateService : IAuthStateService
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILogger<AuthStateService> _logger;
    private readonly TenantConfiguration _tenantConfig;

    public AuthStateService(
        AuthenticationStateProvider authenticationStateProvider,
        ILogger<AuthStateService> logger,
        IOptions<TenantConfiguration> tenantConfig)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _logger = logger;
        _tenantConfig = tenantConfig?.Value ?? throw new ArgumentNullException(nameof(tenantConfig));

        // Validate configuration on startup
        if (!_tenantConfig.IsValid())
        {
            throw new InvalidOperationException(
                "Invalid tenant configuration. DefaultTenantId and DefaultTenant must be set.");
        }
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Attempted to get user ID for unauthenticated user");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Fallback chain: sub → nameidentifier → sid
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? user.FindFirst("sub")?.Value
                     ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                     ?? user.FindFirst("sid")?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User ID not found in JWT claims for authenticated user. Available claims: {Claims}",
                string.Join(", ", user.Claims.Select(c => c.Type)));
            throw new UnauthorizedAccessException("User ID not found in token claims");
        }

        return userId;
    }

    public async Task<Guid> GetCurrentTenantIdAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning("Attempted to get tenant ID for unauthenticated user");
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // MODE 1 (Single-Instance): Always use default tenant
        // This is the recommended mode for most deployments
        if (!_tenantConfig.Enabled)
        {
            _logger.LogDebug("Single-tenant mode: Using default tenant {TenantId}", _tenantConfig.DefaultTenantId);
            return _tenantConfig.DefaultTenantId;
        }

        // MODE 2 (Multi-Tenant SaaS): Require tenant ID in user claims
        _logger.LogDebug("Multi-tenant mode: Extracting tenant from user claims");

        // Look for tenant ID in claims (common claim names)
        var tenantIdString = user.FindFirst("tenant_id")?.Value
                            ?? user.FindFirst("tenantId")?.Value
                            ?? user.FindFirst("tid")?.Value
                            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

        if (string.IsNullOrEmpty(tenantIdString))
        {
            // In multi-tenant mode, we REQUIRE tenant ID in claims
            _logger.LogError("Multi-tenant mode enabled but tenant ID not found in claims. Available claims: {Claims}",
                string.Join(", ", user.Claims.Select(c => c.Type)));
            throw new InvalidOperationException(
                "Multi-tenant mode is enabled but no tenant ID found in user claims. " +
                "Ensure your identity provider includes tenant_id claim.");
        }

        if (!Guid.TryParse(tenantIdString, out var tenantId))
        {
            _logger.LogError("Invalid tenant ID format in claims: {TenantId}", tenantIdString);
            throw new InvalidOperationException($"Invalid tenant ID format: {tenantIdString}");
        }

        _logger.LogDebug("Multi-tenant mode: Using tenant {TenantId} from claims", tenantId);
        return tenantId;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        return authState.User.Identity?.IsAuthenticated ?? false;
    }
}
