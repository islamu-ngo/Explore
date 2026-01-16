using Explore.Domain;

namespace Explore.Application.Contracts.Persistence
{
    public interface ITenantUserRepository : IGenericRepository<TenantUser, Guid>
    {
        Task<TenantUser?> GetByUserAndTenant(Guid userId, Guid tenantId);
        Task<List<TenantUser>> GetByUser(Guid userId);
        Task<List<TenantUser>> GetByTenant(Guid tenantId);
        Task<TenantUser?> GetTenantUserWithDetails(Guid id);
        Task<List<TenantUser>> GetTenantUsersWithDetails();
    }
}
