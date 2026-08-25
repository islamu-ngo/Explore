// ABOUTME: Unit tests for DeleteGroupMemberCommandHandler authorization and deletion safeguards.
// ABOUTME: Covers permission fallback, missing/invalid request failures, and last-admin protection.

using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.GroupMembers.Handlers.Commands;
using Explore.Application.Features.GroupMembers.Requests.Commands;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.GroupMembers.Commands;

public sealed class DeleteGroupMemberCommandHandlerTests
{
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly DeleteGroupMemberCommandHandler _handler;

    public DeleteGroupMemberCommandHandlerTests()
    {
        _handler = new DeleteGroupMemberCommandHandler(
            _groupMemberRepository,
            _userContext,
            _tenantContext);
    }

    [Test]
    public async Task Handle_WhenRequesterHasDeletePermission_DeletesMember()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToDelete);
        _groupMemberRepository.HasPermissionInGroup(
                memberToDelete.GroupTenant.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberDelete)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(memberToDelete.GroupTenant.GroupId)
            .Returns(new List<GroupMember>
            {
                memberToDelete,
                CreateGroupMember(memberToDelete.GroupTenant.GroupId, roleId: (int)RoleEnum.GroupAdmin)
            });

        var result = await _handler.Handle(CreateCommand(memberToDelete.Id, requesterUserId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(memberToDelete.Id);
        await Assert.That(result.Message).IsEqualTo("Member removed successfully");
        await _groupMemberRepository.Received(1).Delete(memberToDelete);
        await _groupMemberRepository.DidNotReceive().GetByGroupAndUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenRequesterIsGroupAdminWithoutPermission_DeletesMemberThroughFallback()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        var requesterMember = CreateGroupMember(memberToDelete.GroupTenant.GroupId, requesterUserId, (int)RoleEnum.GroupAdmin);
        SetupMemberFound(memberToDelete);
        _groupMemberRepository.HasPermissionInGroup(
                memberToDelete.GroupTenant.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberDelete)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(memberToDelete.GroupTenant.GroupId, requesterUserId)
            .Returns(requesterMember);
        _groupMemberRepository.GetMembersByGroupId(memberToDelete.GroupTenant.GroupId)
            .Returns(new List<GroupMember> { requesterMember, memberToDelete });

        var result = await _handler.Handle(CreateCommand(memberToDelete.Id, requesterUserId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(memberToDelete.Id);
        await _groupMemberRepository.Received(1).Delete(memberToDelete);
    }

    [Test]
    public async Task Handle_WhenMemberDoesNotExist_ReturnsFailureAndSkipsAuthorization()
    {
        var memberId = Guid.NewGuid();
        _groupMemberRepository.GetById(memberId).Returns((GroupMember?)null);

        var result = await _handler.Handle(CreateCommand(memberId, Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Member not found");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Delete(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterUserIdIsInvalid_ReturnsFailureAndSkipsAuthorization()
    {
        var memberToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToDelete);

        var result = await _handler.Handle(new DeleteGroupMemberCommand
        {
            MemberId = memberToDelete.Id,
            RequesterUserId = "not-a-guid"
        }, CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invalid requester User ID.");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Delete(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterLacksPermissionAndFallbackRole_ReturnsFailureAndDoesNotDelete()
    {
        var requesterUserId = Guid.NewGuid();
        var memberToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupMember);
        var requesterMember = CreateGroupMember(memberToDelete.GroupTenant.GroupId, requesterUserId, (int)RoleEnum.GroupMember);
        SetupMemberFound(memberToDelete);
        _groupMemberRepository.HasPermissionInGroup(
                memberToDelete.GroupTenant.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberDelete)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(memberToDelete.GroupTenant.GroupId, requesterUserId)
            .Returns(requesterMember);

        var result = await _handler.Handle(CreateCommand(memberToDelete.Id, requesterUserId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("You do not have permission to remove members.");
        await _groupMemberRepository.DidNotReceive().GetMembersByGroupId(Arg.Any<Guid>());
        await _groupMemberRepository.DidNotReceive().Delete(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenDeletingLastGroupAdmin_ReturnsFailureAndDoesNotDelete()
    {
        var requesterUserId = Guid.NewGuid();
        var adminToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupAdmin);
        SetupMemberFound(adminToDelete);
        _groupMemberRepository.HasPermissionInGroup(
                adminToDelete.GroupTenant.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberDelete)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(adminToDelete.GroupTenant.GroupId)
            .Returns(new List<GroupMember>
            {
                adminToDelete,
                CreateGroupMember(adminToDelete.GroupTenant.GroupId, roleId: (int)RoleEnum.GroupMember)
            });

        var result = await _handler.Handle(CreateCommand(adminToDelete.Id, requesterUserId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Cannot remove the last admin of the group.");
        await _groupMemberRepository.DidNotReceive().Delete(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenDeletingOneOfMultipleGroupAdmins_DeletesMember()
    {
        var requesterUserId = Guid.NewGuid();
        var adminToDelete = CreateGroupMember(roleId: (int)RoleEnum.GroupAdmin);
        SetupMemberFound(adminToDelete);
        _groupMemberRepository.HasPermissionInGroup(
                adminToDelete.GroupTenant.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberDelete)
            .Returns(true);
        _groupMemberRepository.GetMembersByGroupId(adminToDelete.GroupTenant.GroupId)
            .Returns(new List<GroupMember>
            {
                adminToDelete,
                CreateGroupMember(adminToDelete.GroupTenant.GroupId, roleId: (int)RoleEnum.GroupAdmin),
                CreateGroupMember(adminToDelete.GroupTenant.GroupId, roleId: (int)RoleEnum.GroupMember)
            });

        var result = await _handler.Handle(CreateCommand(adminToDelete.Id, requesterUserId), CancellationToken.None);

        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Id).IsEqualTo(adminToDelete.Id);
        await _groupMemberRepository.Received(1).Delete(adminToDelete);
    }

    private void SetupMemberFound(GroupMember member)
    {
        _groupMemberRepository.GetById(member.Id).Returns(member);
    }

    private static DeleteGroupMemberCommand CreateCommand(Guid memberId, Guid requesterUserId) => new()
    {
        MemberId = memberId,
        RequesterUserId = requesterUserId.ToString()
    };

    private static GroupMember CreateGroupMember(int roleId) =>
        CreateGroupMember(Guid.NewGuid(), Guid.NewGuid(), roleId);

    private static GroupMember CreateGroupMember(Guid groupId, int roleId) =>
        CreateGroupMember(groupId, Guid.NewGuid(), roleId);

    private static GroupMember CreateGroupMember(Guid groupId, Guid userId, int roleId) => new()
    {
        Id = Guid.NewGuid(),
        GroupTenantId = Guid.NewGuid(),
        GroupTenant = new GroupTenant
        {
            GroupId = groupId,
            Group = new Group { Id = groupId, FullName = "Group" },
            TenantId = Guid.NewGuid(),
            Tenant = null!,
            ApprovalStatusId = 1,
            ApprovalStatus = null!
        },
        UserId = userId,
        RoleId = roleId,
        TenantId = Guid.NewGuid(),
        User = null!,
        Role = null!,
        Tenant = null!
    };
}
