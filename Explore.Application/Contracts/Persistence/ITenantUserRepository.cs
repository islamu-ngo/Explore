using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ITenantUserRepository : IGenericRepository<TenantUser, int>
    {
        Task<TenantUser?> GetByUserAndTenant(Guid userId, Guid tenantId);
        Task<List<TenantUser>> GetByUser(Guid userId);
        Task<List<TenantUser>> GetByTenant(Guid tenantId);
    }
}
