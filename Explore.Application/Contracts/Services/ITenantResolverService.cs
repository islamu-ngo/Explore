// ABOUTME: Resolves the effective tenant for the current execution context.
// ABOUTME: Used by the shared tenant context to keep resolution logic separate from consumer access.

namespace Explore.Application.Contracts.Services;

public interface ITenantResolverService
{
    Guid ResolveTenantId();
}
