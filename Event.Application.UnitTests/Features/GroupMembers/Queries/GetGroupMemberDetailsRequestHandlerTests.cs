// ABOUTME: Unit tests for GetGroupMemberDetailsRequestHandler detail-query behavior.
// ABOUTME: Covers repository detail lookup, DTO mapping, and null-result short-circuiting.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Features.GroupMembers.Handlers.Queries;
using Explore.Application.Features.GroupMembers.Requests.Queries;
using Explore.Domain;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.GroupMembers.Queries;

public sealed class GetGroupMemberDetailsRequestHandlerTests
{
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly GetGroupMemberDetailsRequestHandler _handler;

    public GetGroupMemberDetailsRequestHandlerTests()
    {
        _handler = new GetGroupMemberDetailsRequestHandler(_groupMemberRepository, _mapper);
    }

    [Test]
    public async Task Handle_WhenMemberExists_ReturnsMappedDto()
    {
        var member = CreateGroupMember();
        var expectedDto = new GroupMemberDto
        {
            Id = member.Id,
            GroupId = member.GroupId,
            GroupFullName = member.Group.FullName,
            UserId = member.UserId,
            UserEmail = member.User.Email,
            UserFullName = "Amina Rahman",
            RoleId = member.RoleId,
            RoleName = member.Role.FullName,
            GroupPositionId = member.GroupPositionId,
            GroupPositionFullName = member.GroupPosition!.FullName
        };
        _groupMemberRepository.GetGroupMemberWithDetails(member.Id).Returns(member);
        _mapper.Map<GroupMemberDto>(member).Returns(expectedDto);

        var result = await _handler.Handle(new GetGroupMemberDetailsRequest { Id = member.Id }, CancellationToken.None);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Id).IsEqualTo(member.Id);
        await Assert.That(result.GroupId).IsEqualTo(member.GroupId);
        await Assert.That(result.UserEmail).IsEqualTo("amina.rahman@example.test");
        await Assert.That(result.RoleName).IsEqualTo("Group Moderator");
        await Assert.That(result.GroupPositionFullName).IsEqualTo("Community Lead");
        await _groupMemberRepository.Received(1).GetGroupMemberWithDetails(member.Id);
        _mapper.Received(1).Map<GroupMemberDto>(member);
    }

    [Test]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsNullAndSkipsMapping()
    {
        var memberId = Guid.NewGuid();
        _groupMemberRepository.GetGroupMemberWithDetails(memberId).Returns((GroupMember?)null);

        var result = await _handler.Handle(new GetGroupMemberDetailsRequest { Id = memberId }, CancellationToken.None);

        await Assert.That(result).IsNull();
        await _groupMemberRepository.Received(1).GetGroupMemberWithDetails(memberId);
        _mapper.DidNotReceive().Map<GroupMemberDto>(Arg.Any<GroupMember>());
    }

    private static GroupMember CreateGroupMember()
    {
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

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
                    Email = "amina.rahman@example.test",
                    FirstName = "Amina",
                    LastName = "Rahman"
                }
            },
            RoleId = (int)RoleEnum.GroupModerator,
            Role = new Role
            {
                Id = (int)RoleEnum.GroupModerator,
                MasterCode = "group_moderator",
                FullName = "Group Moderator",
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
}
