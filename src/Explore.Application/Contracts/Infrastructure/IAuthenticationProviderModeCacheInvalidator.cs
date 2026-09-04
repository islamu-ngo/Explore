// ABOUTME: Invalidates cached runtime authentication-provider selection after a configuration change.
// ABOUTME: Keeps cache mechanics outside Application while allowing immediate provider switching.

namespace Explore.Application.Contracts.Infrastructure;

public interface IAuthenticationProviderModeCacheInvalidator
{
    void InvalidateInstanceMode();
}
