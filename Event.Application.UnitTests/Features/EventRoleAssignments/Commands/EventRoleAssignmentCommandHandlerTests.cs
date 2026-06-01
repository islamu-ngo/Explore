// ABOUTME: Unit tests for event-role assignment command handlers and ownership invariants.
// ABOUTME: Covers duplicate prevention, last-owner protection, and first-class ownership transfer.

using System.Diagnostics.Metrics;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Application.Features.EventRoleAssignments.Handlers.Commands;
using Explore.Application.Features.EventRoleAssignments.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRoleAssignments.Commands;

public class EventRoleAssignmentCommandHandlerTests : IDisposable
{
    private static readonly Guid TenantId = Guid.Parse("018f0000-0000-7000-8000-000000001001");
    private static readonly Guid EventId = Guid.Parse("018f0000-0000-7000-8000-000000001002");
    private static readonly Guid ActorUserId = Guid.Parse("018f0000-0000-7000-8000-000000001003");
    private static readonly Guid TargetUserId = Guid.Parse("018f0000-0000-7000-8000-000000001004");

    private readonly IEventRoleAssignmentRepository _assignmentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEventRoleAuthorityCeilingService _authorityCeilingService;
    private readonly IEventAuthoritySnapshotService _snapshotService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly BusinessMetrics _metrics;

    public EventRoleAssignmentCommandHandlerTests()
    {
        _assignmentRepository = Substitute.For<IEventRoleAssignmentRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _authorityCeilingService = Substitute.For<IEventRoleAuthorityCeilingService>();
        _snapshotService = Substitute.For<IEventAuthoritySnapshotService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        var meterFactory = Substitute.For<IMeterFactory>();
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(new Meter("test"));
        _metrics = new BusinessMetrics(meterFactory);

        _eventRepository.GetById(EventId).Returns(Task.FromResult<Explore.Domain.Event?>(CreateEvent()));
        _userRepository.Exists(TargetUserId).Returns(Task.FromResult(true));

        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var operation = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return operation(CancellationToken.None);
            });
    }

    public void Dispose()
    {
        _metrics.Dispose();
    }

    [Test]
    public async Task AssignEventRole_WhenOpenAssignmentExists_ReturnsDuplicateFailure()
    {
        var existing = EventRoleAssignment.Create(
            TenantId,
            EventId,
            TargetUserId,
            (int)RoleEnum.CheckInStaff,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-5),
            null,
            ActorUserId);

        _authorityCeilingService.CanAssignRoleAsync(
                TenantId,
                EventId,
                ActorUserId,
                (int)RoleEnum.CheckInStaff,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(EventRoleAssignmentAuthorityResult.Allowed(
                new[] { PermissionCodes.EventCheckInManage })));

        _assignmentRepository.GetOpenByEventUserRoleAsync(
                TenantId,
                EventId,
                TargetUserId,
                (int)RoleEnum.CheckInStaff,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventRoleAssignment?>(existing));

        var handler = new AssignEventRoleCommandHandler(
            _assignmentRepository,
            _eventRepository,
            _userRepository,
            _authorityCeilingService,
            _metrics);

        var result = await handler.Handle(new AssignEventRoleCommand
        {
            TenantId = TenantId,
            EventId = EventId,
            TargetUserId = TargetUserId,
            RoleId = (int)RoleEnum.CheckInStaff,
            ActorUserId = ActorUserId,
            StartsAtUtc = DateTime.UtcNow.AddMinutes(-1)
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_role_assignment_duplicate");
        await _assignmentRepository.DidNotReceive().Create(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task RevokeEventRoleAssignment_ForEventOwner_ReturnsTransferRequired()
    {
        var owner = EventRoleAssignment.Create(
            TenantId,
            EventId,
            TargetUserId,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-5),
            null,
            ActorUserId);

        _assignmentRepository.GetById(owner.Id).Returns(Task.FromResult<EventRoleAssignment?>(owner));
        var handler = new RevokeEventRoleAssignmentCommandHandler(
            _assignmentRepository,
            _eventRepository,
            _authorityCeilingService,
            _metrics);

        var result = await handler.Handle(new RevokeEventRoleAssignmentCommand
        {
            TenantId = TenantId,
            EventId = EventId,
            AssignmentId = owner.Id,
            ActorUserId = ActorUserId
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_owner_transfer_required");
        await _authorityCeilingService.DidNotReceive().CanAssignRoleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await _assignmentRepository.DidNotReceive().Update(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task TransferEventOwnership_WhenReplacementOwnerStartsInFuture_ReturnsInvalidTransfer()
    {
        var currentOwnerUserId = Guid.Parse("018f0000-0000-7000-8000-000000001006");
        var currentOwner = EventRoleAssignment.Create(
            TenantId,
            EventId,
            currentOwnerUserId,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-10),
            null,
            currentOwnerUserId);

        _userRepository.Exists(TargetUserId).Returns(Task.FromResult(true));
        ConfigureTransferAuthority();
        _assignmentRepository.GetById(currentOwner.Id).Returns(Task.FromResult<EventRoleAssignment?>(currentOwner));
        _assignmentRepository.GetOpenByEventUserRoleAsync(
                TenantId,
                EventId,
                TargetUserId,
                (int)RoleEnum.EventOwner,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventRoleAssignment?>(null));
        _assignmentRepository.Create(Arg.Any<EventRoleAssignment>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<EventRoleAssignment>()));

        var handler = CreateTransferHandler();

        var result = await handler.Handle(new TransferEventOwnershipCommand
        {
            TenantId = TenantId,
            EventId = EventId,
            CurrentOwnerAssignmentId = currentOwner.Id,
            NewOwnerUserId = TargetUserId,
            ActorUserId = ActorUserId,
            StartsAtUtc = DateTime.UtcNow.AddHours(1)
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_ownership_transfer_invalid");
        await Assert.That(currentOwner.Status).IsEqualTo(EventRoleAssignmentStatus.Active);
    }

    [Test]
    public async Task TransferEventOwnership_CreatesNewOwnerAndRevokesCurrentOwner()
    {
        var currentOwnerUserId = Guid.Parse("018f0000-0000-7000-8000-000000001005");
        var currentOwner = EventRoleAssignment.Create(
            TenantId,
            EventId,
            currentOwnerUserId,
            (int)RoleEnum.EventOwner,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-10),
            null,
            currentOwnerUserId);

        _userRepository.Exists(TargetUserId).Returns(Task.FromResult(true));
        ConfigureTransferAuthority();

        _assignmentRepository.GetById(currentOwner.Id).Returns(Task.FromResult<EventRoleAssignment?>(currentOwner));
        _assignmentRepository.GetOpenByEventUserRoleAsync(
                TenantId,
                EventId,
                TargetUserId,
                (int)RoleEnum.EventOwner,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EventRoleAssignment?>(null));
        _assignmentRepository.Create(Arg.Any<EventRoleAssignment>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<EventRoleAssignment>()));

        var handler = CreateTransferHandler();

        var result = await handler.Handle(new TransferEventOwnershipCommand
        {
            TenantId = TenantId,
            EventId = EventId,
            CurrentOwnerAssignmentId = currentOwner.Id,
            NewOwnerUserId = TargetUserId,
            ActorUserId = ActorUserId,
            StartsAtUtc = DateTime.UtcNow.AddMinutes(-1)
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(currentOwner.Status).IsEqualTo(EventRoleAssignmentStatus.Revoked);
        await _assignmentRepository.Received(1).Create(Arg.Is<EventRoleAssignment>(assignment =>
            assignment.UserId == TargetUserId &&
            assignment.RoleId == (int)RoleEnum.EventOwner &&
            assignment.Status == EventRoleAssignmentStatus.Active));
        await _assignmentRepository.Received(1).Update(currentOwner);
    }

    private static Explore.Domain.Event CreateEvent() =>
        new()
        {
            Id = EventId,
            TenantId = TenantId,
            Title = "Authority Test Event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };

    private void ConfigureTransferAuthority()
    {
        _snapshotService.GetForUserAndEventsAsync(
                TenantId,
                ActorUserId,
                Arg.Any<IReadOnlyCollection<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new EventAuthoritySnapshot(
                TenantId,
                ActorUserId,
                new Dictionary<Guid, EventAuthorityForUser>
                {
                    [EventId] = new(
                        new HashSet<string>(StringComparer.Ordinal),
                        new[] { PermissionCodes.EventTransferOwnership }.ToHashSet(StringComparer.Ordinal),
                        IsOwner: true,
                        IsManager: true)
                })));
    }

    private TransferEventOwnershipCommandHandler CreateTransferHandler() =>
        new(
            _assignmentRepository,
            _eventRepository,
            _userRepository,
            _snapshotService,
            _unitOfWork,
            _metrics);
}
