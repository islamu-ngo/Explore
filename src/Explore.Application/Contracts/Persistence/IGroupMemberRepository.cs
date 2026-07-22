using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IGroupMemberRepository : IGenericRepository<GroupMember, Guid>
{
    Task<List<GroupMember>> GetGroupMembersWithDetails();
    Task<GroupMember?> GetGroupMemberWithDetails(Guid id);
    Task<List<GroupMember>> GetMembersByGroupId(Guid groupId);
    Task<GroupMember?> GetByGroupAndUser(Guid groupId, Guid userId);
    Task<bool> Exists(Guid groupId, Guid userId);
    Task<bool> HasPermissionInGroup(Guid groupId, Guid userId, string permissionMasterCode);
    Task<List<Guid>> GetGroupIdsWhereUserHasPermission(
        Guid userId,
        string permissionMasterCode,
        CancellationToken cancellationToken = default);
    Task<List<GroupMember>> GetMembershipsByUser(
        Guid userId,
        CancellationToken cancellationToken = default);
}
