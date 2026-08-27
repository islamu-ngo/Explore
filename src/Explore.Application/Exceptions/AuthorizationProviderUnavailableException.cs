// ABOUTME: Represents a fail-closed decision when the configured authorization provider is unavailable.
// ABOUTME: Preserves AuthorizationException compatibility while enabling a safe HTTP 503 mapping.

namespace Explore.Application.Exceptions;

public sealed class AuthorizationProviderUnavailableException : AuthorizationException
{
    public AuthorizationProviderUnavailableException(string resource, string action)
        : base(resource, action)
    {
    }
}
