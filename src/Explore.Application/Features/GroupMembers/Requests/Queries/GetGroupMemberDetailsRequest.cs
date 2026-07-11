// ABOUTME: MediatR query for retrieving a single group member by ID.
// ABOUTME: Returns full detail DTO with user, role, and position info.

using System;
using Explore.Application.DTOs.GroupMember;
using MediatR;

namespace Explore.Application.Features.GroupMembers.Requests.Queries;

public class GetGroupMemberDetailsRequest : IRequest<GroupMemberDto?>
{
    public Guid Id { get; set; }
}
