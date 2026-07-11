// ABOUTME: Resolves effective integer custom-property quota values for a tenant by walking tenant -> system -> registry default.
// ABOUTME: Keeps the projection updaters focused on projection logic and the quotas auditable from a single place.

namespace Explore.Application.Contracts.Services;

public interface ICustomPropertyQuotaResolver
{
    Task<int> GetIntAsync(string key, Guid tenantId, CancellationToken cancellationToken);

    Task<bool> GetBoolAsync(string key, Guid tenantId, CancellationToken cancellationToken);
}
