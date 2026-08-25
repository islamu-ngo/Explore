// ABOUTME: MediatR query for fetching all members of a group.
// ABOUTME: Returns List<GroupMemberDto>.
using Explore.Application.DTOs.GroupMember;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Queries;

public sealed record GetGroupMembersRequest(Guid GroupId = default) : IRequest<List<GroupMemberDto>>;
