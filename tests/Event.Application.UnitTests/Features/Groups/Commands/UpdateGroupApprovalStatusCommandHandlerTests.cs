// ABOUTME: Unit tests for the Group approval-status command handler.
// ABOUTME: Verifies admin approval updates, validation failures, and missing-group behavior.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Group;
using Explore.Application.Exceptions;
using Explore.Application.Features.Groups.Handlers.Commands;
using Explore.Application.Features.Groups.Requests.Commands;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.Groups.Commands;

public class UpdateGroupApprovalStatusCommandHandlerTests
{
    private readonly IGroupRepository _groupRepository;
    private readonly IApprovalStatusRepository _approvalStatusRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly UpdateGroupApprovalStatusCommandHandler _handler;

    public UpdateGroupApprovalStatusCommandHandlerTests()
    {
        _groupRepository = Substitute.For<IGroupRepository>();
        _approvalStatusRepository = Substitute.For<IApprovalStatusRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _cache = Substitute.For<HybridCache>();
        _handler = new UpdateGroupApprovalStatusCommandHandler(
            _groupRepository,
            _approvalStatusRepository,
            _currentUserService,
            _cache);
    }

    [Test]
    public async Task Handle_WithValidApprovalStatus_UpdatesApprovalStateAndInvalidatesCache()
    {
        var groupId = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();
        var group = CreateGroup(groupId, approvalStatusId: 1);
        var command = CreateCommand(groupId, approvalStatusId: 2);

        _approvalStatusRepository.Exists(2).Returns(true);
        _groupRepository.GetById(groupId).Returns(group);
        _currentUserService.UserId.Returns(currentUserId);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(groupId);
        await Assert.That(group.ApprovalStatusId).IsEqualTo(2);
        await Assert.That(group.UpdatedBy).IsEqualTo(currentUserId);
        await _groupRepository.Received(1).Update(group);
        await _cache.Received(1).RemoveAsync($"group:detail:{groupId}", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithUnknownApprovalStatus_ReturnsValidationFailureWithoutUpdatingGroup()
    {
        var groupId = Guid.NewGuid();
        var command = CreateCommand(groupId, approvalStatusId: 999);

        _approvalStatusRepository.Exists(999).Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Message).IsEqualTo("Validation failed.");
        await Assert.That(result.Errors).Contains("Approval Status Id does not exist.");
        await _groupRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
    }

    [Test]
    public async Task Handle_WhenGroupDoesNotExist_ThrowsNotFoundException()
    {
        var groupId = Guid.NewGuid();
        var command = CreateCommand(groupId, approvalStatusId: 2);

        _approvalStatusRepository.Exists(2).Returns(true);
        _groupRepository.GetById(groupId).Returns((Group?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
        await _groupRepository.DidNotReceive().Update(Arg.Any<Group>());
    }

    private static UpdateGroupApprovalStatusCommand CreateCommand(Guid groupId, int approvalStatusId) =>
        new()
        {
            Id = groupId,
            GroupApprovalStatusDto = new UpdateGroupApprovalStatusDto
            {
                ApprovalStatusId = approvalStatusId
            }
        };

    private static Group CreateGroup(Guid groupId, int approvalStatusId) =>
        new()
        {
            Id = groupId,
            FullName = "Community Team",
            ApprovalStatusId = approvalStatusId,
            ApprovalStatus = null!,
            TenantId = Guid.NewGuid(),
            Tenant = null!
        };
}
