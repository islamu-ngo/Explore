// ABOUTME: Unit tests for GetGroupMembersRequestHandler list-query behavior.
// ABOUTME: Covers group-id forwarding, DTO list mapping, and empty-list results.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Features.GroupMembers.Handlers.Queries;
using Explore.Application.Features.GroupMembers.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.GroupMembers.Queries;

public sealed class GetGroupMembersRequestHandlerTests
{
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetGroupMembersRequestHandler _handler;

    public GetGroupMembersRequestHandlerTests()
    {
        _handler = new GetGroupMembersRequestHandler(_groupMemberRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenMembersExist_ReturnsMappedDtos()
    {
        var groupId = Guid.NewGuid();
        var members = new List<GroupMember>
        {
            CreateGroupMember(groupId, "amina.rahman@example.test", RoleEnum.GroupModerator),
            CreateGroupMember(groupId, "yusuf.khan@example.test", RoleEnum.GroupMember)
        };
        var expectedDtos = new List<GroupMemberDto>
        {
            CreateDto(members[0], "Amina Rahman", "Group Moderator"),
            CreateDto(members[1], "Yusuf Khan", "Group Member")
        };
        _groupMemberRepository.GetMembersByGroupId(groupId).Returns(members);
        _mapper.Map<List<GroupMemberDto>>(members).Returns(expectedDtos);

        var result = await _handler.Handle(new GetGroupMembersRequest { GroupId = groupId }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result[1].RoleName).IsEqualTo("Group Member");
        await _groupMemberRepository.Received(1).GetMembersByGroupId(groupId);
        _mapper.Received(1).Map<List<GroupMemberDto>>(members);
    }

    [Test]
    public async Task Handle_WhenNoMembersExist_ReturnsEmptyMappedList()
    {
        var groupId = Guid.NewGuid();
        var members = new List<GroupMember>();
        var expectedDtos = new List<GroupMemberDto>();
        _groupMemberRepository.GetMembersByGroupId(groupId).Returns(members);
        _mapper.Map<List<GroupMemberDto>>(members).Returns(expectedDtos);

        var result = await _handler.Handle(new GetGroupMembersRequest { GroupId = groupId }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
        await _groupMemberRepository.Received(1).GetMembersByGroupId(groupId);
        _mapper.Received(1).Map<List<GroupMemberDto>>(members);
    }

    [Test]
    public async Task Handle_UsesRequestGroupId_ForRepositoryLookup()
    {
        var requestedGroupId = Guid.NewGuid();
        var otherGroupId = Guid.NewGuid();
        var members = new List<GroupMember>();
        _groupMemberRepository.GetMembersByGroupId(Arg.Any<Guid>()).Returns(members);
        _mapper.Map<List<GroupMemberDto>>(members).Returns(new List<GroupMemberDto>());

        await _handler.Handle(new GetGroupMembersRequest { GroupId = requestedGroupId }, CancellationToken.None);

        await _groupMemberRepository.Received(1).GetMembersByGroupId(
            Arg.Is<Guid>(groupId => groupId == requestedGroupId));
        await _groupMemberRepository.DidNotReceive().GetMembersByGroupId(otherGroupId);
    }

    private static GroupMember CreateGroupMember(Guid groupId, string email, RoleEnum role)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleName = role == RoleEnum.GroupModerator ? "Group Moderator" : "Group Member";

        return new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Group = new Group
            {
                Id = groupId,
                FullName = "Community Volunteers",
                TenantId = tenantId,
                Tenant = null!,
                ApprovalStatus = null!
            },
            UserId = userId,
            User = new User
            {
                Id = userId,
                Pii = new UserPii
                {
                    Email = email,
                    FirstName = email.StartsWith("amina", StringComparison.Ordinal) ? "Amina" : "Yusuf",
                    LastName = email.StartsWith("amina", StringComparison.Ordinal) ? "Rahman" : "Khan"
                }
            },
            RoleId = (int)role,
            Role = new Role
            {
                Id = (int)role,
                MasterCode = role == RoleEnum.GroupModerator ? "group_moderator" : "group_member",
                FullName = roleName,
                Scope = RoleScopeEnum.Group
            },
            GroupPositionId = 17,
            GroupPosition = new GroupPosition
            {
                Id = 17,
                MasterCode = "community_lead",
                FullName = "Community Lead"
            },
            TenantId = tenantId,
            Tenant = null!
        };
    }

    private static GroupMemberDto CreateDto(GroupMember member, string userFullName, string roleName)
    {
        return new GroupMemberDto
        {
            Id = member.Id,
            GroupId = member.GroupId,
            GroupFullName = member.Group.FullName,
            UserId = member.UserId,
            UserEmail = member.User.Email,
            UserFullName = userFullName,
            RoleId = member.RoleId,
            RoleName = roleName,
            GroupPositionId = member.GroupPositionId,
            GroupPositionFullName = member.GroupPosition!.FullName
        };
    }
}
