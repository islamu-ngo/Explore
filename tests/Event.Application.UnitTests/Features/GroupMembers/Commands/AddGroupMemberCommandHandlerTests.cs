// ABOUTME: Unit tests for AddGroupMemberCommandHandler authorization and create behavior.
// ABOUTME: Covers permission fallback, failure short-circuits, and tenant-stamped persistence.

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

public sealed class AddGroupMemberCommandHandlerTests
{
    private const string TargetEmail = "new.member@example.test";

    private readonly IGroupRepository _groupRepository = Substitute.For<IGroupRepository>();
    private readonly IGroupTenantRepository _groupTenantRepository = Substitute.For<IGroupTenantRepository>();
    private readonly IGroupMemberRepository _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUserContext _userContext = Substitute.For<IUserContext>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly AddGroupMemberCommandHandler _handler;

    public AddGroupMemberCommandHandlerTests()
    {
        _handler = new AddGroupMemberCommandHandler(
            _groupRepository,
            _groupTenantRepository,
            _groupMemberRepository,
            _userRepository,
            _userContext,
            _tenantContext);
    }

    [Test]
    public async Task Handle_WhenRequesterHasCreatePermission_AddsMember()
    {
        var dto = CreateValidDto();
        var requesterUserId = Guid.NewGuid();
        var targetUser = CreateUser(dto.Email);
        var tenantId = Guid.NewGuid();
        var createdMemberId = Guid.NewGuid();
        SetupGroupFound(dto.GroupId);
        _tenantContext.TenantId.Returns(tenantId);
        _groupMemberRepository.HasPermissionInGroup(
                dto.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberCreate)
            .Returns(true);
        _userRepository.GetUserByEmail(dto.Email).Returns(targetUser);
        _groupMemberRepository.Exists(dto.GroupId, targetUser.Id).Returns(false);
        _groupMemberRepository.Create(Arg.Any<GroupMember>()).Returns(callInfo =>
        {
            var member = callInfo.Arg<GroupMember>();
            member.Id = createdMemberId;
            return member;
        });

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(createdMemberId);
        await Assert.That(result.Message).IsEqualTo("Member added successfully");
        await _groupMemberRepository.Received(1).Create(Arg.Is<GroupMember>(member =>
            member.GroupTenant.GroupId == dto.GroupId
            && member.UserId == targetUser.Id
            && member.RoleId == (int)dto.Role
            && member.GroupPositionId == dto.GroupPositionId
            && member.TenantId == tenantId));
        await _groupMemberRepository.DidNotReceive().GetByGroupAndUser(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_WhenRequesterIsGroupAdminWithoutPermission_AddsMemberThroughFallback()
    {
        var dto = CreateValidDto();
        var requesterUserId = Guid.NewGuid();
        var targetUser = CreateUser(dto.Email);
        SetupGroupFound(dto.GroupId);
        _groupMemberRepository.HasPermissionInGroup(
                dto.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberCreate)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(dto.GroupId, requesterUserId)
            .Returns(CreateGroupMember(dto.GroupId, requesterUserId, (int)RoleEnum.GroupAdmin));
        _userRepository.GetUserByEmail(dto.Email).Returns(targetUser);
        _groupMemberRepository.Exists(dto.GroupId, targetUser.Id).Returns(false);
        _groupMemberRepository.Create(Arg.Any<GroupMember>()).Returns(callInfo => callInfo.Arg<GroupMember>());

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Member added successfully");
        await _groupMemberRepository.Received(1).Create(Arg.Is<GroupMember>(member =>
            member.GroupTenant.GroupId == dto.GroupId
            && member.UserId == targetUser.Id));
    }

    [Test]
    public async Task Handle_WhenGroupDoesNotExist_ReturnsFailureAndSkipsAuthorization()
    {
        var dto = CreateValidDto();
        _groupRepository.GetById(dto.GroupId).Returns((Group?)null);

        var result = await _handler.Handle(CreateCommand(dto, Guid.NewGuid()), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Group not found");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Create(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterUserIdIsInvalid_ReturnsFailureAndSkipsAuthorization()
    {
        var dto = CreateValidDto();
        SetupGroupFound(dto.GroupId);

        var result = await _handler.Handle(new AddGroupMemberCommand
        {
            AddGroupMemberDto = dto,
            RequesterUserId = "not-a-guid"
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Invalid requester User ID.");
        await _groupMemberRepository.DidNotReceive().HasPermissionInGroup(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<string>());
        await _userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Create(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenRequesterLacksPermissionAndFallbackRole_ReturnsFailureAndDoesNotCreate()
    {
        var dto = CreateValidDto();
        var requesterUserId = Guid.NewGuid();
        SetupGroupFound(dto.GroupId);
        _groupMemberRepository.HasPermissionInGroup(
                dto.GroupId,
                requesterUserId,
                PermissionCodes.GroupMemberCreate)
            .Returns(false);
        _groupMemberRepository.GetByGroupAndUser(dto.GroupId, requesterUserId)
            .Returns(CreateGroupMember(dto.GroupId, requesterUserId, (int)RoleEnum.GroupMember));

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("You do not have permission to add members.");
        await _userRepository.DidNotReceive().GetUserByEmail(Arg.Any<string>());
        await _groupMemberRepository.DidNotReceive().Exists(Arg.Any<Guid>(), Arg.Any<Guid>());
        await _groupMemberRepository.DidNotReceive().Create(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenUserEmailDoesNotExist_ReturnsFailureAndDoesNotCreate()
    {
        var dto = CreateValidDto();
        var requesterUserId = Guid.NewGuid();
        SetupAuthorizedRequester(dto.GroupId, requesterUserId);
        _userRepository.GetUserByEmail(dto.Email).Returns((User?)null);

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("User with this email not found.");
        await _groupMemberRepository.DidNotReceive().Exists(Arg.Any<Guid>(), Arg.Any<Guid>());
        await _groupMemberRepository.DidNotReceive().Create(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WhenUserIsAlreadyMember_ReturnsFailureAndDoesNotCreate()
    {
        var dto = CreateValidDto();
        var requesterUserId = Guid.NewGuid();
        var targetUser = CreateUser(dto.Email);
        SetupAuthorizedRequester(dto.GroupId, requesterUserId);
        _userRepository.GetUserByEmail(dto.Email).Returns(targetUser);
        _groupMemberRepository.Exists(dto.GroupId, targetUser.Id).Returns(true);

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("User is already a member of this group.");
        await _groupMemberRepository.DidNotReceive().Create(Arg.Any<GroupMember>());
    }

    [Test]
    public async Task Handle_WithGroupPosition_AddsMemberWithPositionAndTenantFromContext()
    {
        var dto = CreateValidDto(groupPositionId: 42, role: RoleEnum.GroupModerator);
        var requesterUserId = Guid.NewGuid();
        var targetUser = CreateUser(dto.Email);
        var tenantId = Guid.NewGuid();
        SetupAuthorizedRequester(dto.GroupId, requesterUserId);
        _tenantContext.TenantId.Returns(tenantId);
        _userRepository.GetUserByEmail(dto.Email).Returns(targetUser);
        _groupMemberRepository.Exists(dto.GroupId, targetUser.Id).Returns(false);
        _groupMemberRepository.Create(Arg.Any<GroupMember>()).Returns(callInfo => callInfo.Arg<GroupMember>());

        var result = await _handler.Handle(CreateCommand(dto, requesterUserId), CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await _groupMemberRepository.Received(1).Create(Arg.Is<GroupMember>(member =>
            member.GroupTenant.GroupId == dto.GroupId
            && member.UserId == targetUser.Id
            && member.RoleId == (int)RoleEnum.GroupModerator
            && member.GroupPositionId == 42
            && member.TenantId == tenantId));
    }

    private void SetupAuthorizedRequester(Guid groupId, Guid requesterUserId)
    {
        SetupGroupFound(groupId);
        _groupMemberRepository.HasPermissionInGroup(
                groupId,
                requesterUserId,
                PermissionCodes.GroupMemberCreate)
            .Returns(true);
    }

    private void SetupGroupFound(Guid groupId)
    {
        Group group = CreateGroup(groupId);
        _groupRepository.GetById(groupId).Returns(group);
        _groupTenantRepository.GetByGroupAndTenant(groupId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => CreateGroupTenant(group, callInfo.ArgAt<Guid>(1)));
    }

    private static AddGroupMemberCommand CreateCommand(AddGroupMemberDto dto, Guid requesterUserId) => new()
    {
        AddGroupMemberDto = dto,
        RequesterUserId = requesterUserId.ToString()
    };

    private static AddGroupMemberDto CreateValidDto(
        int? groupPositionId = 7,
        RoleEnum role = RoleEnum.GroupMember) => new()
        {
            GroupId = Guid.NewGuid(),
            Email = TargetEmail,
            Role = role,
            GroupPositionId = groupPositionId
        };

    private static Group CreateGroup(Guid groupId) => new()
    {
        Id = groupId,
        FullName = "Community Group"
    };

    private static GroupTenant CreateGroupTenant(Group group, Guid tenantId) => new()
    {
        Id = Guid.NewGuid(),
        GroupId = group.Id,
        Group = group,
        TenantId = tenantId,
        Tenant = null!,
        ApprovalStatus = null!
    };

    private static GroupMember CreateGroupMember(Guid groupId, Guid userId, int roleId) => new()
    {
        Id = Guid.NewGuid(),
        GroupTenantId = Guid.NewGuid(),
        GroupTenant = CreateGroupTenant(CreateGroup(groupId), Guid.NewGuid()),
        UserId = userId,
        RoleId = roleId,
        TenantId = Guid.NewGuid(),
        User = null!,
        Role = null!,
        Tenant = null!
    };

    private static User CreateUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        Pii = new UserPii
        {
            Email = email,
            FirstName = "New",
            LastName = "Member"
        }
    };
}
