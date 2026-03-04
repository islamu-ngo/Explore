// ABOUTME: Repository contract for tenant member assignments and role-scoped membership checks.
// ABOUTME: Provides tenant/user lookup helpers for tenant-level authorization workflows.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ITenantMemberRepository : IGenericRepository<TenantMember, Guid>
{
    Task<TenantMember?> GetByTenantAndUser(Guid tenantId, Guid userId);
    Task<List<TenantMember>> GetByTenant(Guid tenantId);
    Task<List<TenantMember>> GetByUserId(Guid userId);
    Task<bool> IsTenantMember(Guid tenantId, Guid userId);
    Task<bool> IsTenantAdmin(Guid tenantId, Guid userId);
    Task<TenantMember?> GetMemberWithDetails(Guid id);
    Task<List<TenantMember>> GetMembersWithDetails();
}
