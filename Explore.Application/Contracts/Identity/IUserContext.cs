namespace Explore.Application.Contracts.Identity;

/// <summary>
/// Provides access to the current user's identity information.
/// Extracts user claims from the HTTP context (e.g., Keycloak JWT token).
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Gets the current user's ID (from 'sub' claim in Keycloak token).
    /// Returns null if user is not authenticated.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the current user's preferred username.
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's ID or throws if not authenticated.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated.</exception>
    Guid GetRequiredUserId();
}
