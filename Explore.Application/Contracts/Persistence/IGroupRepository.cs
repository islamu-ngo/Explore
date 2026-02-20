using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGroupRepository : IGenericRepository<Group, Guid>
{
    Task<Group?> GetGroupWithDetails(Guid id);
    Task<List<Group>> GetGroupsWithDetails();
    Task<List<Group>> GetMyGroups(Guid userId);
    Task<(List<Group> Items, int TotalCount)> GetGroupsWithDetailsPaged(int pageNumber, int pageSize);
    Task<(List<Group> Items, int TotalCount)> GetMyGroupsPaged(Guid userId, int pageNumber, int pageSize);
}
