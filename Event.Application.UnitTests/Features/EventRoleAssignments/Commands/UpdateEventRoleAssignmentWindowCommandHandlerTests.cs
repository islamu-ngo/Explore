// ABOUTME: Unit tests for UpdateEventRoleAssignmentWindowCommandHandler edge behavior.
// ABOUTME: Covers assignment lookup, authority denial, event scoping, and validity-window updates.

using System.Diagnostics.Metrics;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.EventRoleAssignments.Handlers.Commands;
using Explore.Application.Features.EventRoleAssignments.Requests.Commands;
using Explore.Application.Telemetry;
using Explore.Domain;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using NSubstitute;

namespace Event.Application.UnitTests.Features.EventRoleAssignments.Commands;

public sealed class UpdateEventRoleAssignmentWindowCommandHandlerTests : IDisposable
{
    private static readonly Guid TenantId = Guid.Parse("018f0000-0000-7000-8000-000000002001");
    private static readonly Guid EventId = Guid.Parse("018f0000-0000-7000-8000-000000002002");
    private static readonly Guid ActorUserId = Guid.Parse("018f0000-0000-7000-8000-000000002003");
    private static readonly Guid TargetUserId = Guid.Parse("018f0000-0000-7000-8000-000000002004");

    private readonly IEventRoleAssignmentRepository _assignmentRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventRoleAuthorityCeilingService _authorityCeilingService;
    private readonly UpdateEventRoleAssignmentWindowCommandHandler _handler;
    private readonly Meter _meter;

    public UpdateEventRoleAssignmentWindowCommandHandlerTests()
    {
        _assignmentRepository = Substitute.For<IEventRoleAssignmentRepository>();
        _eventRepository = Substitute.For<IEventRepository>();
        _authorityCeilingService = Substitute.For<IEventRoleAuthorityCeilingService>();

        var meterFactory = Substitute.For<IMeterFactory>();
        _meter = new Meter("test");
        meterFactory.Create(Arg.Any<MeterOptions>()).Returns(_meter);
        var metrics = new BusinessMetrics(meterFactory);

        _handler = new UpdateEventRoleAssignmentWindowCommandHandler(
            _assignmentRepository,
            _eventRepository,
            _authorityCeilingService,
            metrics);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }

    [Test]
    public async Task Handle_WhenAssignmentDoesNotExist_ReturnsNotFoundAndSkipsAuthorization()
    {
        var command = CreateCommand(Guid.NewGuid());
        _assignmentRepository.GetById(command.AssignmentId).Returns(Task.FromResult<EventRoleAssignment?>(null));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_role_assignment_not_found");
        await Assert.That(result.Id).IsEqualTo(command.AssignmentId);
        await _eventRepository.DidNotReceive().GetById(Arg.Any<Guid>());
        await _authorityCeilingService.DidNotReceive().CanAssignRoleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await _assignmentRepository.DidNotReceive().Update(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task Handle_WhenEventIsOutsideTenant_ReturnsEventNotFoundAndSkipsAuthorization()
    {
        var assignment = CreateAssignment();
        var command = CreateCommand(assignment.Id);
        _assignmentRepository.GetById(assignment.Id).Returns(Task.FromResult<EventRoleAssignment?>(assignment));
        _eventRepository.GetById(EventId).Returns(Task.FromResult<Explore.Domain.Event?>(CreateEvent(Guid.NewGuid())));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo("event_not_found");
        await _authorityCeilingService.DidNotReceive().CanAssignRoleAsync(
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<Guid>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
        await _assignmentRepository.DidNotReceive().Update(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task Handle_WhenAuthorityDenied_ReturnsAuthorityFailureAndDoesNotUpdate()
    {
        var assignment = CreateAssignment();
        var command = CreateCommand(assignment.Id);
        _assignmentRepository.GetById(assignment.Id).Returns(Task.FromResult<EventRoleAssignment?>(assignment));
        _eventRepository.GetById(EventId).Returns(Task.FromResult<Explore.Domain.Event?>(CreateEvent(TenantId)));
        _authorityCeilingService.CanAssignRoleAsync(
                TenantId,
                EventId,
                ActorUserId,
                (int)RoleEnum.CheckInStaff,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(EventRoleAssignmentAuthorityResult.Denied(
                EventRoleAuthorityFailureCodes.AuthorityCeilingExceeded,
                "The role contains permissions outside your same-event authority ceiling.",
                new[] { PermissionCodes.EventCheckInManage })));

        var result = await _handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.FailureCode).IsEqualTo(EventRoleAuthorityFailureCodes.AuthorityCeilingExceeded);
        await Assert.That(result.Errors).Contains("The role contains permissions outside your same-event authority ceiling.");
        await _assignmentRepository.DidNotReceive().Update(Arg.Any<EventRoleAssignment>());
    }

    [Test]
    public async Task Handle_WhenAuthorityAllowed_UpdatesWindowAndPersistsAssignment()
    {
        var assignment = CreateAssignment();
        var originalVersion = assignment.Version;
        var command = CreateCommand(assignment.Id, DateTime.UtcNow.AddMinutes(15), DateTime.UtcNow.AddHours(3));
        _assignmentRepository.GetById(assignment.Id).Returns(Task.FromResult<EventRoleAssignment?>(assignment));
        _eventRepository.GetById(EventId).Returns(Task.FromResult<Explore.Domain.Event?>(CreateEvent(TenantId)));
        _authorityCeilingService.CanAssignRoleAsync(
                TenantId,
                EventId,
                ActorUserId,
                (int)RoleEnum.CheckInStaff,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(EventRoleAssignmentAuthorityResult.Allowed(
                new[] { PermissionCodes.EventCheckInManage })));

        var beforeHandle = DateTime.UtcNow;
        var result = await _handler.Handle(command, CancellationToken.None);
        var afterHandle = DateTime.UtcNow;

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(assignment.Id);
        await Assert.That(result.Message).IsEqualTo("Event role assignment updated successfully.");
        await Assert.That(assignment.StartsAtUtc).IsEqualTo(command.StartsAtUtc);
        await Assert.That(assignment.ExpiresAtUtc).IsEqualTo(command.ExpiresAtUtc);
        await Assert.That(assignment.Version).IsEqualTo(originalVersion + 1);
        await Assert.That(assignment.UpdatedAt >= beforeHandle && assignment.UpdatedAt <= afterHandle).IsTrue();
        await _assignmentRepository.Received(1).Update(assignment);
    }

    private static UpdateEventRoleAssignmentWindowCommand CreateCommand(
        Guid assignmentId,
        DateTime? startsAtUtc = null,
        DateTime? expiresAtUtc = null) =>
        new()
        {
            TenantId = TenantId,
            EventId = EventId,
            AssignmentId = assignmentId,
            ActorUserId = ActorUserId,
            StartsAtUtc = startsAtUtc ?? DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = expiresAtUtc
        };

    private static EventRoleAssignment CreateAssignment() =>
        EventRoleAssignment.Create(
            TenantId,
            EventId,
            TargetUserId,
            (int)RoleEnum.CheckInStaff,
            EventRoleAssignmentStatus.Active,
            DateTime.UtcNow.AddMinutes(-30),
            null,
            ActorUserId);

    private static Explore.Domain.Event CreateEvent(Guid tenantId) =>
        new()
        {
            Id = EventId,
            TenantId = tenantId,
            Title = "Window Test Event",
            Actor = null!,
            Tenant = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
}
