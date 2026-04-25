// ABOUTME: Contract for resolving messaging configuration from the cascading settings engine.
// ABOUTME: Supports the SaaS multi-tenant hierarchy: Instance admin -> Tenant admin.

using Explore.Application.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IMessagingConfigResolver
{
    Task<MessagingConfiguration> ResolveAsync(CancellationToken cancellationToken = default);

    void InvalidateCache(Guid? tenantId = null);
}
