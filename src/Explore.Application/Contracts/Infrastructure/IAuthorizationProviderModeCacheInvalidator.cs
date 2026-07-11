// ABOUTME: Invalidates runtime authorization provider mode cache after instance provider configuration changes.
// ABOUTME: Keeps Application-facing services provider-neutral while Infrastructure owns cache mechanics.

namespace Explore.Application.Contracts.Infrastructure;

public interface IAuthorizationProviderModeCacheInvalidator
{
    void InvalidateInstanceMode();
}
