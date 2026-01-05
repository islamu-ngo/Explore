using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ITenantSettingsRepository : IGenericRepository<TenantSettings, int>
    {
        Task<TenantSettings?> GetByTenant(Guid tenantId);
    }
}
