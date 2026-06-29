// ABOUTME: Unit tests for group hierarchy validation in create/update command handlers.
// ABOUTME: Covers same-tenant parent checks, self-parent rejection, and cycle/depth guardrails before persistence.

using System.Diagnostics.Metrics;
using AutoMapper;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group;
using Explore.Application.Exceptions;
using Explore.Application.Features.Groups.Handlers.Commands;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Application.Models.Common;
using Explore.Application.Responses;
using Explore.Application.Telemetry;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Groups.Commands;

public class GroupHierarchyCommandHandlerTests : IDisposable
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

    public void Dispose()
    {
        _metrics.Dispose();
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
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UpdateGroupDto = new UpdateGroupDto
            {
                ParentGroup = new UpdateGroupParentGroupDto
                {
                    Value = OptionalUpdate<Guid?>.Set(groupId)
                }
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ConcurrencyStamp = command.ExpectedConcurrencyStamp,
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
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UpdateGroupDto = new UpdateGroupDto
            {
                ParentGroup = new UpdateGroupParentGroupDto
                {
                    Value = OptionalUpdate<Guid?>.Set(parentGroupId)
                }
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ConcurrencyStamp = command.ExpectedConcurrencyStamp,
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
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            UpdateGroupDto = new UpdateGroupDto
            {
                ParentGroup = new UpdateGroupParentGroupDto
                {
                    Value = OptionalUpdate<Guid?>.Set(parentGroupId)
                }
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ConcurrencyStamp = command.ExpectedConcurrencyStamp,
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

    [Test]
    public async Task Update_ShouldRejectEmptyWrapperWithoutLoadingOrSaving()
    {
        var handler = CreateUpdateHandler();
        var command = new UpdateGroupCommand
        {
            GroupId = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateGroupDto = new UpdateGroupDto()
        };

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("At least one group update group must be provided.");
        await _groupRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_ShouldThrowConcurrencyConflictBeforeSave()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var currentStamp = Guid.NewGuid();
        var handler = CreateUpdateHandler();
        var command = new UpdateGroupCommand
        {
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateGroupDto = new UpdateGroupDto
            {
                FullName = new UpdateGroupFullNameDto { Value = "Updated Group" }
            }
        };

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            ConcurrencyStamp = currentStamp,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        });

        await Assert.That(async () => await handler.Handle(command, CancellationToken.None))
            .Throws<ConcurrencyConflictException>();

        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
        await _cache.DidNotReceive().RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_ShouldApplySingleGroupAndInvalidateDetailCache()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var group = new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            Description = "Existing description",
            ConcurrencyStamp = concurrencyStamp,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        };
        var handler = CreateUpdateHandler();

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(group);

        var result = await handler.Handle(new UpdateGroupCommand
        {
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = concurrencyStamp,
            UpdateGroupDto = new UpdateGroupDto
            {
                FullName = new UpdateGroupFullNameDto { Value = "Updated Group" }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(group.FullName).IsEqualTo("Updated Group");
        await Assert.That(group.Description).IsEqualTo("Existing description");
        await _groupRepository.Received(1).Update(group);
        await _cache.Received(1).RemoveAsync($"group:detail:{groupId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Update_ShouldExplicitlyClearDescription()
    {
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var concurrencyStamp = Guid.NewGuid();
        var group = new Group
        {
            Id = groupId,
            FullName = "Managed Group",
            Description = "Existing description",
            ConcurrencyStamp = concurrencyStamp,
            ApprovalStatus = null!,
            TenantId = tenantId,
            Tenant = null!
        };
        var handler = CreateUpdateHandler();

        _tenantContext.TenantId.Returns(tenantId);
        _userContext.GetRequiredUserId().Returns(userId);
        _groupMemberRepository.HasPermissionInGroup(groupId, userId, Arg.Any<string>()).Returns(true);
        _groupRepository.GetById(groupId).Returns(group);

        var result = await handler.Handle(new UpdateGroupCommand
        {
            GroupId = groupId,
            UserId = userId.ToString(),
            ExpectedConcurrencyStamp = concurrencyStamp,
            UpdateGroupDto = new UpdateGroupDto
            {
                Description = new UpdateGroupDescriptionDto
                {
                    Value = OptionalUpdate<string?>.Set(null)
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(group.Description).IsNull();
        await _groupRepository.Received(1).Update(group);
    }

    [Test]
    public async Task Update_ShouldRejectDescriptionGroupWithoutFieldOperation()
    {
        var handler = CreateUpdateHandler();

        var result = await handler.Handle(new UpdateGroupCommand
        {
            GroupId = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            ExpectedConcurrencyStamp = Guid.NewGuid(),
            UpdateGroupDto = new UpdateGroupDto
            {
                Description = new UpdateGroupDescriptionDto()
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Description group must include Value.");
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
            _tenantContext,
            _cache);
    }
}
