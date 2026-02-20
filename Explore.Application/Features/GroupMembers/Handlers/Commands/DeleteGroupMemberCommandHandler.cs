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

public class DeleteGroupMemberCommandHandler : IRequestHandler<DeleteGroupMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public DeleteGroupMemberCommandHandler(
        IGroupMemberRepository groupMemberRepository,
        IUserContext userContext,
        ITenantContext tenantContext)
    {
        _groupMemberRepository = groupMemberRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(DeleteGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();

        var memberToDelete = await _groupMemberRepository.GetById(request.MemberId);
        if (memberToDelete == null)
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
            memberToDelete.GroupId, requesterGuid, PermissionCodes.GroupMemberDelete);

        if (!hasPermission)
        {
            // Transitional fallback: allow GroupCreator and GroupAdmin roles
            var requesterMember = await _groupMemberRepository.GetByGroupAndUser(memberToDelete.GroupId, requesterGuid);
            if (requesterMember == null ||
                (requesterMember.RoleId != (int)RoleEnum.GroupCreator &&
                 requesterMember.RoleId != (int)RoleEnum.GroupAdmin))
            {
                response.Success = false;
                response.Message = "You do not have permission to remove members.";
                return response;
            }
        }

        // Prevent removing the last admin/creator
        var members = await _groupMemberRepository.GetMembersByGroupId(memberToDelete.GroupId);
        var adminCount = members.Count(m =>
            m.RoleId == (int)RoleEnum.GroupCreator || m.RoleId == (int)RoleEnum.GroupAdmin);

        if ((memberToDelete.RoleId == (int)RoleEnum.GroupCreator || memberToDelete.RoleId == (int)RoleEnum.GroupAdmin) &&
            adminCount <= 1)
        {
            response.Success = false;
            response.Message = "Cannot remove the last admin of the group.";
            return response;
        }

        await _groupMemberRepository.Delete(memberToDelete);

        response.Success = true;
        response.Message = "Member removed successfully";
        response.Id = memberToDelete.Id;

        return response;
    }
}
