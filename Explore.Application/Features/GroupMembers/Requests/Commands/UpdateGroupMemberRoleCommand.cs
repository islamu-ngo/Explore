// ABOUTME: MediatR command for updating a group member's role.
// ABOUTME: Carries the member ID and new role ID.
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Commands;

public class UpdateGroupMemberRoleCommand : IRequest<BaseCommandResponse<Guid>>
{
    public required UpdateGroupMemberRoleDto UpdateGroupMemberRoleDto { get; set; }
    public string? RequesterUserId { get; set; }
}
