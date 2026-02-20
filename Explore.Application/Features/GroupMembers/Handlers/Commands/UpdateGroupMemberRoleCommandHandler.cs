using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.GroupMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Handlers.Commands;

public class UpdateGroupMemberRoleCommandHandler : IRequestHandler<UpdateGroupMemberRoleCommand, BaseCommandResponse<Guid>>
{
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public UpdateGroupMemberRoleCommandHandler(
        IGroupMemberRepository groupMemberRepository,
        IUserContext userContext,
        ITenantContext tenantContext)
    {
        _groupMemberRepository = groupMemberRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(UpdateGroupMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.UpdateGroupMemberRoleDto;

        var memberToUpdate = await _groupMemberRepository.GetById(dto.Id);
        if (memberToUpdate == null)
        {
            response.Success = false;
            response.Message = "Member not found";
            return response;
        }

        if (!Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
        {
            response.Success = false;
            response.Message = "Invalid requester User ID.";
            return response;
        }

        var hasPermission = await _groupMemberRepository.HasPermissionInGroup(
            memberToUpdate.GroupId, requesterGuid, PermissionCodes.GroupMemberUpdate);

        if (!hasPermission)
        {
            // Transitional fallback: allow GroupCreator and GroupAdmin roles
            var requesterMember = await _groupMemberRepository.GetByGroupAndUser(memberToUpdate.GroupId, requesterGuid);
            if (requesterMember == null ||
                (requesterMember.RoleId != (int)RoleEnum.GroupCreator &&
                 requesterMember.RoleId != (int)RoleEnum.GroupAdmin))
            {
                response.Success = false;
                response.Message = "You do not have permission to update roles.";
                return response;
            }
        }

        // Prevent demoting the last admin/creator
        var members = await _groupMemberRepository.GetMembersByGroupId(memberToUpdate.GroupId);
        var adminCount = members.Count(m =>
            m.RoleId == (int)RoleEnum.GroupCreator || m.RoleId == (int)RoleEnum.GroupAdmin);

        if ((memberToUpdate.RoleId == (int)RoleEnum.GroupCreator || memberToUpdate.RoleId == (int)RoleEnum.GroupAdmin) &&
            (int)dto.Role != (int)RoleEnum.GroupCreator &&
            (int)dto.Role != (int)RoleEnum.GroupAdmin &&
            adminCount <= 1)
        {
            response.Success = false;
            response.Message = "Cannot demote the last admin of the group.";
            return response;
        }

        memberToUpdate.RoleId = (int)dto.Role;
        await _groupMemberRepository.Update(memberToUpdate);

        response.Success = true;
        response.Message = "Member role updated successfully";
        response.Id = memberToUpdate.Id;

        return response;
    }
}
