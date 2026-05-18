// ABOUTME: Unit tests for group hierarchy validation in create/update command handlers.
// ABOUTME: Covers same-tenant parent checks, self-parent rejection, and cycle/depth guardrails before persistence.

using System.Diagnostics.Metrics;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group;
using Explore.Application.Features.Groups.Handlers.Commands;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Groups.Commands;

public class GroupHierarchyCommandHandlerTests
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IActorRepository _actorRepository;
    private readonly IStorageObjectRepository _storageObjectRepository;
    private readonly IUserContext _userContext;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly HybridCache _cache;
    private readonly BusinessMetrics _metrics;

    public GroupHierarchyCommandHandlerTests()
    {
        _groupRepository = Substitute.For<IGroupRepository>();
        _groupMemberRepository = Substitute.For<IGroupMemberRepository>();
        _actorRepository = Substitute.For<IActorRepository>();
        _storageObjectRepository = Substitute.For<IStorageObjectRepository>();
        _userContext = Substitute.For<IUserContext>();
        _mapper = Substitute.For<IMapper>();
        _tenantContext = Substitute.For<ITenantContext>();
        _cache = Substitute.For<HybridCache>();

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));
        _metrics = new BusinessMetrics(meterFactory);

        _groupRepository.ExecuteWithHierarchyMutationLock(
                Arg.Any<Guid>(),
                Arg.Any<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.ArgAt<Func<CancellationToken, Task<BaseCommandResponse<Guid>>>>(1);
                return operation(CancellationToken.None);
            });
    }

    [Test]
    public async Task Create_ShouldRejectParentOrganizationOutsideCurrentTenant()
    {
        var tenantId = Guid.NewGuid();
        var parentOrganizationId = Guid.NewGuid();
        var handler = CreateCreateHandler();
        var command = new CreateGroupCommand
        {
            GroupDto = new CreateGroupDto
            {
                FullName = "Nested Group",
                ParentOrganizationId = parentOrganizationId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _groupRepository.OrganizationExistsInTenant(parentOrganizationId, tenantId, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Parent organization does not exist in the current tenant.");
        await _groupRepository.DidNotReceive().Create(Arg.Any<Group>());
    }

    [Test]
    public async Task Create_ShouldRejectDualParentRequest()
    {
        var handler = CreateCreateHandler();
        var command = new CreateGroupCommand
        {
            GroupDto = new CreateGroupDto
            {
                FullName = "Invalid Group",
                ParentOrganizationId = Guid.NewGuid(),
                ParentGroupId = Guid.NewGuid()
            }
        };

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("A group can have either a parent organization or a parent group, not both.");
        await _groupRepository.DidNotReceive().Create(Arg.Any<Group>());
    }

    [Test]
    public async Task Update_ShouldRejectSelfParent()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = CreateUpdateHandler();
        var command = new UpdateGroupCommand
        {
            Id = groupId,
            UserId = userId.ToString(),
            GroupDto = new UpdateGroupDto
            {
                FullName = "Managed Group",
                ParentGroupId = groupId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        });

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("A group cannot be its own parent.");
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
    }

    [Test]
    public async Task Update_ShouldRejectParentGroupCycle()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var parentGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = CreateUpdateHandler();
        var command = new UpdateGroupCommand
        {
            Id = groupId,
            UserId = userId.ToString(),
            GroupDto = new UpdateGroupDto
            {
                FullName = "Managed Group",
                ParentGroupId = parentGroupId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        _groupRepository.GroupExistsInTenant(parentGroupId, tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _groupRepository.WouldCreateHierarchyCycle(groupId, parentGroupId, tenantId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Parent group would create a hierarchy cycle.");
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
    }

    [Test]
    public async Task Update_ShouldRejectMoveThatWouldExceedDepthWithExistingDescendants()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var parentGroupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = CreateUpdateHandler();
        var command = new UpdateGroupCommand
        {
            Id = groupId,
            UserId = userId.ToString(),
            GroupDto = new UpdateGroupDto
            {
                FullName = "Managed Group",
                ParentGroupId = parentGroupId
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        });
        _groupRepository.GroupExistsInTenant(parentGroupId, tenantId, Arg.Any<CancellationToken>()).Returns(true);
        _groupRepository.WouldExceedHierarchyDepthForMove(groupId, parentGroupId, tenantId, GroupHierarchyRules.MaxDepth, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Parent group hierarchy exceeds the maximum supported depth.");
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
    }

    private CreateGroupCommandHandler CreateCreateHandler()
    {
        return new CreateGroupCommandHandler(
            _groupRepository,
            _groupMemberRepository,
            _actorRepository,
            _storageObjectRepository,
            _userContext,
            _mapper,
            _tenantContext,
            _cache,
            _metrics);
    }

    private UpdateGroupCommandHandler CreateUpdateHandler()
    {
        return new UpdateGroupCommandHandler(
            _groupRepository,
            _groupMemberRepository,
            _userContext,
            _mapper,
            _tenantContext,
            _cache);
    }
}
