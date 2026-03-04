using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.GroupMembers.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Handlers.Commands;

public class AddGroupMemberCommandHandler : IRequestHandler<AddGroupMemberCommand, BaseCommandResponse<Guid>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserContext _userContext;
    private readonly ITenantContext _tenantContext;

    public AddGroupMemberCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IUserContext userContext,
        ITenantContext tenantContext)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _userContext = userContext;
        _tenantContext = tenantContext;
    }

    public async Task<BaseCommandResponse<Guid>> Handle(AddGroupMemberCommand request, CancellationToken cancellationToken)
    {
        var response = new BaseCommandResponse<Guid>();
        var dto = request.AddGroupMemberDto;

        var group = await _groupRepository.GetById(dto.GroupId);
        if (group == null)
        {
            response.Success = false;
            response.Message = "Group not found";
            return response;
        }

        if (!Guid.TryParse(request.RequesterUserId, out Guid requesterGuid))
        {
            response.Success = false;
            response.Message = "Invalid requester User ID.";
            return response;
        }

        var hasPermission = await _groupMemberRepository.HasPermissionInGroup(
            dto.GroupId, requesterGuid, PermissionCodes.GroupMemberCreate);

        if (!hasPermission)
        {
            // Transitional fallback: allow GroupAdmin role if no permissions are seeded
            var requesterMember = await _groupMemberRepository.GetByGroupAndUser(dto.GroupId, requesterGuid);
            if (requesterMember == null ||
                requesterMember.RoleId != (int)RoleEnum.GroupAdmin)
            {
                response.Success = false;
                response.Message = "You do not have permission to add members.";
                return response;
            }
        }

        var userToAdd = await _userRepository.GetUserByEmail(dto.Email);
        if (userToAdd == null)
        {
            response.Success = false;
            response.Message = "User with this email not found.";
            return response;
        }

        var alreadyMember = await _groupMemberRepository.Exists(dto.GroupId, userToAdd.Id);
        if (alreadyMember)
        {
            response.Success = false;
            response.Message = "User is already a member of this group.";
            return response;
        }

        var groupMember = new GroupMember
        {
            GroupId = dto.GroupId,
            Group = null!,
            UserId = userToAdd.Id,
            User = null!,
            RoleId = (int)dto.Role,
            Role = null!,
            GroupPositionId = dto.GroupPositionId,
            TenantId = _tenantContext.TenantId,
            Tenant = null!
        };

        groupMember = await _groupMemberRepository.Create(groupMember);

        response.Success = true;
        response.Message = "Member added successfully";
        response.Id = groupMember.Id;

        return response;
    }
}
