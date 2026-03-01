using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantRepository : IGenericRepository<Tenant, Guid>
{
    Task<Tenant?> GetTenantBySlug(string slug);
    Task<int> GetActiveTenantCountAsync();
}
