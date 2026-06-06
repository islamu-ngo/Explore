// ABOUTME: Unit tests for UpdateGroupMemberRoleCommandHandler authorization and role changes.
// ABOUTME: Covers permission fallback, failure short-circuits, and last-admin demotion protection.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.GroupMember;
using Explore.Application.Features.GroupMembers.Handlers.Commands;
using Explore.Application.Features.GroupMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.GroupMembers.Commands;

public sealed class UpdateGroupMemberRoleCommandHandlerTests
{
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly UpdateGroupMemberRoleCommandHandler _handler;

    public UpdateGroupMemberRoleCommandHandlerTests()
    {
        _handler = new UpdateGroupMemberRoleCommandHandler(
            _groupMemberRepository,
            _userContext,
            _tenantContext);
    }

    [Test]
    public async Task Handle_WhenRequesterHasUpdatePermission_UpdatesRole()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToUpdate = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToUpdate);
        _groupMemberRepository.HasPermissionInGroup(
                memberToUpdate.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberUpdate)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(memberToUpdate.GroupId)
            .Returns(new List<GroupMember>
            {
                memberToUpdate,
                CreateGroupMember(memberToUpdate.GroupId, roleId: (int)RoleEnum.GroupAdmin)
            });

        var result = await _handler.Handle(
            CreateCommand(memberToUpdate.Id, RoleEnum.GroupModerator, requesterUserId),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(memberToUpdate.Id);
        await Assert.That(result.Message).IsEqualTo("Member role updated successfully");
        await Assert.That(memberToUpdate.RoleId).IsEqualTo((int)RoleEnum.GroupModerator);
        await _groupMemberRepository.Received(1).Update(memberToUpdate);
        await _groupMemberRepository.DidNotReceive().GetByGroupAndUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenRequesterIsGroupAdminWithoutPermission_UpdatesRoleThroughFallback()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToUpdate = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        var requesterMember = CreateGroupMember(memberToUpdate.GroupId, requesterUserId, (int)RoleEnum.GroupAdmin);
        SetupMemberFound(memberToUpdate);
        _groupMemberRepository.HasPermissionInGroup(
                memberToUpdate.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberUpdate)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(memberToUpdate.GroupId, requesterUserId)
            .Returns(requesterMember);
        _groupMemberRepository.GetMembersByGroupId(memberToUpdate.GroupId)
            .Returns(new List<GroupMember> { requesterMember, memberToUpdate });

        var result = await _handler.Handle(
            CreateCommand(memberToUpdate.Id, RoleEnum.GroupModerator, requesterUserId),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(memberToUpdate.RoleId).IsEqualTo((int)RoleEnum.GroupModerator);
        await _groupMemberRepository.Received(1).Update(memberToUpdate);
    }

    [Test]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsFailureAndSkipsAuthorization()
    {
        var memberId = Guid.NewGuid();
        _groupMemberRepository.GetById(memberId).Returns((GroupMember?)null);

        var result = await _handler.Handle(
            CreateCommand(memberId, RoleEnum.GroupModerator, Guid.NewGuid()),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Member not found");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Update(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterUserIdIsInvalid_ReturnsFailureAndSkipsAuthorization()
    {
        var memberToUpdate = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToUpdate);

        var result = await _handler.Handle(new UpdateGroupMemberRoleCommand
        {
            UpdateGroupMemberRoleDto = new UpdateGroupMemberRoleDto
            {
                Id = memberToUpdate.Id,
                Role = RoleEnum.GroupModerator
            },
            RequesterUserId = "not-a-guid"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invalid requester User ID.");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Update(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterLacksPermissionAndFallbackRole_ReturnsFailureAndDoesNotUpdate()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToUpdate = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        var requesterMember = CreateGroupMember(memberToUpdate.GroupId, requesterUserId, (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToUpdate);
        _groupMemberRepository.HasPermissionInGroup(
                memberToUpdate.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberUpdate)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(memberToUpdate.GroupId, requesterUserId)
            .Returns(requesterMember);

        var result = await _handler.Handle(
            CreateCommand(memberToUpdate.Id, RoleEnum.GroupModerator, requesterUserId),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("You do not have permission to update roles.");
        await Assert.That(memberToUpdate.RoleId).IsEqualTo((int)RoleEnum.GroupMember);
        await _groupMemberRepository.DidNotReceive().GetMembersByGroupId(Arg.Any<Guid>());
        await _groupMemberRepository.DidNotReceive().Update(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenDemotingLastGroupAdmin_ReturnsFailureAndDoesNotUpdate()
    {
        var requesterUserId = Guid.NewGuid();
        var adminToDemote = CreateGroupMember(roleId: (int)RoleEnum.GroupAdmin);
        SetupMemberFound(adminToDemote);
        _groupMemberRepository.HasPermissionInGroup(
                adminToDemote.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberUpdate)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(adminToDemote.GroupId)
            .Returns(new List<GroupMember>
            {
                adminToDemote,
                CreateGroupMember(adminToDemote.GroupId, roleId: (int)RoleEnum.GroupMember)
            });

        var result = await _handler.Handle(
            CreateCommand(adminToDemote.Id, RoleEnum.GroupMember, requesterUserId),
            CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Cannot demote the last admin of the group.");
        await Assert.That(adminToDemote.RoleId).IsEqualTo((int)RoleEnum.GroupAdmin);
        await _groupMemberRepository.DidNotReceive().Update(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenDemotingOneOfMultipleGroupAdmins_UpdatesRole()
    {
        var requesterUserId = Guid.NewGuid();
        var adminToDemote = CreateGroupMember(roleId: (int)RoleEnum.GroupAdmin);
        SetupMemberFound(adminToDemote);
        _groupMemberRepository.HasPermissionInGroup(
                adminToDemote.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberUpdate)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(adminToDemote.GroupId)
            .Returns(new List<GroupMember>
            {
                adminToDemote,
                CreateGroupMember(adminToDemote.GroupId, roleId: (int)RoleEnum.GroupAdmin),
                CreateGroupMember(adminToDemote.GroupId, roleId: (int)RoleEnum.GroupMember)
            });

        var result = await _handler.Handle(
            CreateCommand(adminToDemote.Id, RoleEnum.GroupMember, requesterUserId),
            CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(adminToDemote.RoleId).IsEqualTo((int)RoleEnum.GroupMember);
        await _groupMemberRepository.Received(1).Update(adminToDemote);
    }

    private void SetupMemberFound(GroupMember member)
    {
        _groupMemberRepository.GetById(member.Id).Returns(member);
    }

    private static UpdateGroupMemberRoleCommand CreateCommand(
        Guid memberId,
        RoleEnum role,
        Guid requesterUserId) => new()
        {
            UpdateGroupMemberRoleDto = new UpdateGroupMemberRoleDto
            {
                Id = memberId,
                Role = role
            },
            RequesterUserId = requesterUserId.ToString()
        };

    private static GroupMember CreateGroupMember(int roleId) =>
        CreateGroupMember(Guid.NewGuid(), Guid.NewGuid(), roleId);

    private static GroupMember CreateGroupMember(Guid groupId, int roleId) =>
        CreateGroupMember(groupId, Guid.NewGuid(), roleId);

    private static GroupMember CreateGroupMember(Guid groupId, Guid userId, int roleId) => new()
    {
        Id = Guid.NewGuid(),
        GroupId = groupId,
        UserId = userId,
        RoleId = roleId,
        TenantId = Guid.NewGuid(),
        Group = null!,
        User = null!,
        Role = null!,
        Tenant = null!
    };
}
