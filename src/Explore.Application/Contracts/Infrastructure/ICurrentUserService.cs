using System;

namespace Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Provides access to the current authenticated user's information.
/// This abstracts away HTTP context concerns from the Application layer.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID from authentication context.
    /// Returns null if no user is authenticated or user ID cannot be determined.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}
