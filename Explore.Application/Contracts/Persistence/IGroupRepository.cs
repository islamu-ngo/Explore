using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGroupRepository : IGenericRepository<Group, Guid>
{
    Task<Group?> GetGroupWithDetails(Guid id);
    Task<List<Group>> GetGroupsWithDetails();
    Task<List<Group>> GetMyGroups(Guid userId);
    Task<(List<Group> Items, int TotalCount)> GetGroupsWithDetailsPaged(int pageNumber, int pageSize);
    Task<(List<Group> Items, int TotalCount)> GetMyGroupsPaged(Guid userId, int pageNumber, int pageSize);
    Task<bool> OrganizationExistsInTenant(Guid organizationId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> GroupExistsInTenant(Guid groupId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> WouldCreateHierarchyCycle(Guid groupId, Guid parentGroupId, Guid tenantId, CancellationToken cancellationToken);
    Task<bool> WouldExceedHierarchyDepth(Guid? parentGroupId, Guid tenantId, int maxDepth, CancellationToken cancellationToken);
    Task<bool> WouldExceedHierarchyDepthForMove(Guid groupId, Guid? parentGroupId, Guid tenantId, int maxDepth, CancellationToken cancellationToken);
    Task<T> ExecuteWithHierarchyMutationLock<T>(Guid tenantId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
