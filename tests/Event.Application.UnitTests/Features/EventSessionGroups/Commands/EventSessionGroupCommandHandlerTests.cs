// ABOUTME: Unit tests for event session group write handlers.
// ABOUTME: Protects tenant derivation, same-event assignment validation, and primary group reassignment behavior.

using AutoMapper;
using Event.Application.UnitTests.Common;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionGroup;
using Explore.Application.Features.EventSessionGroups.Handlers.Commands;
using Explore.Application.Features.EventSessionGroups.Requests.Commands;
using Explore.Application.Services;
using Explore.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using NSubstitute;
using TUnit.Assertions;
using TUnit.Core;

namespace Event.Application.UnitTests.Features.EventSessionGroups.Commands;

public class EventSessionGroupCommandHandlerTests
{
    private readonly IEventSessionGroupRepository _groupRepository = Substitute.For<IEventSessionGroupRepository>();
    private readonly IEventSessionGroupSessionRepository _assignmentRepository = Substitute.For<IEventSessionGroupSessionRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly IEventSessionRepository _sessionRepository = Substitute.For<IEventSessionRepository>();
    private readonly ILocationRepository _locationRepository = Substitute.For<ILocationRepository>();
    private readonly ILocationRoomRepository _roomRepository = Substitute.For<ILocationRoomRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly EventLocationAttachmentService _eventLocationAttachmentService;
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly HybridCache _cache = Substitute.For<HybridCache>();

    public EventSessionGroupCommandHandlerTests()
    {
        _eventLocationAttachmentService = EventLocationAttachmentServiceTestFixture.ForExistingEvent(
            _eventRepository,
            Guid.NewGuid());
        _unitOfWork
            .ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<EventSessionGroup>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<EventSessionGroup>>>()(
                call.Arg<CancellationToken>()));
        _unitOfWork
            .ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task>>()(call.Arg<CancellationToken>()));
    }

    [Test]
    public async Task Create_DerivesTenantFromParentEvent()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var parentEvent = CreateEvent(eventId, tenantId);
        var mappedGroup = new EventSessionGroup
        {
            Id = groupId,
            EventId = eventId,
            Event = null!,
            Name = "Main track",
            Tenant = null!
        };
        var command = new CreateEventSessionGroupCommand
        {
            EventSessionGroup = new CreateEventSessionGroupRequestDto
            {
                EventId = eventId,
                Name = "Main track"
            }
        };

        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _mapper.Map<EventSessionGroup>(command.EventSessionGroup).Returns(mappedGroup);
        _groupRepository.Create(Arg.Any<EventSessionGroup>()).Returns(call => call.Arg<EventSessionGroup>());

        var handler = new CreateEventSessionGroupCommandHandler(
            _groupRepository,
            _eventRepository,
            _locationRepository,
            _roomRepository,
            _unitOfWork,
            _eventLocationAttachmentService,
            _mapper);

        var result = await handler.Handle(command, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(groupId);
        await _groupRepository.Received(1).Create(Arg.Is<EventSessionGroup>(group =>
            group.TenantId == tenantId &&
            group.EventId == eventId));
    }

    [Test]
    public async Task Create_WhenSlugAlreadyExistsForUnpublishedGroupInEvent_ReturnsValidationFailure()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var parentEvent = CreateEvent(eventId, tenantId);
        var existingGroup = CreateGroup(Guid.NewGuid(), eventId, tenantId, parentEvent);
        existingGroup.IsPublished = false;
        existingGroup.Slug = "main-track";

        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _groupRepository.GetActiveByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([existingGroup]);

        var handler = new CreateEventSessionGroupCommandHandler(
            _groupRepository,
            _eventRepository,
            _locationRepository,
            _roomRepository,
            _unitOfWork,
            _eventLocationAttachmentService,
            _mapper);

        var result = await handler.Handle(new CreateEventSessionGroupCommand
        {
            EventSessionGroup = new CreateEventSessionGroupRequestDto
            {
                EventId = eventId,
                Name = "Main track duplicate",
                Slug = "MAIN-TRACK"
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Slug must be unique within the event.");
        await _groupRepository.DidNotReceive().Create(Arg.Any<EventSessionGroup>());
    }

    [Test]
    public async Task Update_WhenSlugAlreadyExistsForAnotherUnpublishedGroupInEvent_ReturnsValidationFailure()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var parentEvent = CreateEvent(eventId, tenantId);
        var currentGroup = CreateGroup(groupId, eventId, tenantId, parentEvent);
        currentGroup.ConcurrencyStamp = Guid.NewGuid();
        currentGroup.Slug = "workshops";
        var existingGroup = CreateGroup(Guid.NewGuid(), eventId, tenantId, parentEvent);
        existingGroup.IsPublished = false;
        existingGroup.Slug = "main-track";

        _eventRepository.GetById(eventId).Returns(parentEvent);
        _groupRepository.GetForUpdateAsync(groupId, Arg.Any<CancellationToken>()).Returns(currentGroup);
        _groupRepository.GetActiveByEventAsync(eventId, Arg.Any<CancellationToken>()).Returns([currentGroup, existingGroup]);

        var handler = new UpdateEventSessionGroupCommandHandler(
            _groupRepository,
            _eventRepository,
            _locationRepository,
            _roomRepository,
            _unitOfWork,
            _eventLocationAttachmentService,
            _cache);

        var result = await handler.Handle(new UpdateEventSessionGroupCommand
        {
            EventSessionGroupId = groupId,
            ExpectedConcurrencyStamp = currentGroup.ConcurrencyStamp,
            EventSessionGroup = new UpdateEventSessionGroupRequestDto
            {
                Metadata = new UpdateEventSessionGroupMetadataDto
                {
                    Name = "Renamed track",
                    Slug = new Explore.Application.Models.Common.OptionalUpdate<string?>(true, "MAIN-TRACK")
                }
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Errors).Contains("Slug must be unique within the event.");
        await _groupRepository.DidNotReceive().Update(Arg.Any<EventSessionGroup>());
    }

    [Test]
    public async Task AssignSession_WhenSessionBelongsToDifferentEvent_ReturnsFailure()
    {
        var eventId = Guid.NewGuid();
        var otherEventId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var parentEvent = CreateEvent(eventId, tenantId);
        var group = CreateGroup(groupId, eventId, tenantId, parentEvent);
        var session = CreateSession(sessionId, otherEventId, tenantId);

        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _groupRepository.Exists(groupId).Returns(true);
        _groupRepository.GetForUpdateAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);
        _sessionRepository.Exists(sessionId).Returns(true);
        _sessionRepository.GetById(sessionId).Returns(session);

        var handler = CreateAssignHandler();

        var result = await handler.Handle(new AssignSessionToGroupCommand
        {
            Assignment = new AssignSessionToGroupRequestDto
            {
                EventId = eventId,
                EventSessionGroupId = groupId,
                EventSessionId = sessionId,
                IsPrimary = true
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsFalse();
        await _assignmentRepository.DidNotReceive().Create(Arg.Any<EventSessionGroupSession>());
    }

    [Test]
    public async Task AssignSession_WhenPrimary_DemotesOtherPrimaryAssignments()
    {
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var oldPrimary = new EventSessionGroupSession
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            Event = null!,
            EventSessionGroupId = Guid.NewGuid(),
            EventSessionGroup = null!,
            EventSessionId = sessionId,
            EventSession = null!,
            IsPrimary = true,
            TenantId = tenantId,
            Tenant = null!
        };
        var parentEvent = CreateEvent(eventId, tenantId);
        var group = CreateGroup(groupId, eventId, tenantId, parentEvent);
        var session = CreateSession(sessionId, eventId, tenantId);

        _eventRepository.Exists(eventId).Returns(true);
        _eventRepository.GetById(eventId).Returns(parentEvent);
        _groupRepository.Exists(groupId).Returns(true);
        _groupRepository.GetForUpdateAsync(groupId, Arg.Any<CancellationToken>()).Returns(group);
        _sessionRepository.Exists(sessionId).Returns(true);
        _sessionRepository.GetById(sessionId).Returns(session);
        _assignmentRepository
            .GetExistingAssignmentAsync(groupId, sessionId, Arg.Any<CancellationToken>())
            .Returns((EventSessionGroupSession?)null);
        _assignmentRepository
            .GetPrimaryAssignmentsForSessionAsync(sessionId, null, Arg.Any<CancellationToken>())
            .Returns(new List<EventSessionGroupSession> { oldPrimary });
        _assignmentRepository.Create(Arg.Any<EventSessionGroupSession>()).Returns(call =>
        {
            var created = call.Arg<EventSessionGroupSession>();
            created.Id = assignmentId;
            return created;
        });

        var handler = CreateAssignHandler();

        var result = await handler.Handle(new AssignSessionToGroupCommand
        {
            Assignment = new AssignSessionToGroupRequestDto
            {
                EventId = eventId,
                EventSessionGroupId = groupId,
                EventSessionId = sessionId,
                IsPrimary = true,
                SortOrder = 3
            }
        }, CancellationToken.None);

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.Id).IsEqualTo(assignmentId);
        await _assignmentRepository.Received(1).Update(Arg.Is<EventSessionGroupSession>(assignment =>
            assignment.Id == oldPrimary.Id && !assignment.IsPrimary));
        await _assignmentRepository.Received(1).Create(Arg.Is<EventSessionGroupSession>(assignment =>
            assignment.EventId == eventId &&
            assignment.TenantId == tenantId &&
            assignment.EventSessionGroupId == groupId &&
            assignment.EventSessionId == sessionId &&
            assignment.IsPrimary &&
            assignment.SortOrder == 3));
    }

    private AssignSessionToGroupCommandHandler CreateAssignHandler()
    {
        return new AssignSessionToGroupCommandHandler(
            _groupRepository,
            _assignmentRepository,
            _eventRepository,
            _sessionRepository);
    }

    private static Explore.Domain.Event CreateEvent(Guid id, Guid tenantId)
    {
        return new Explore.Domain.Event
        {
            Id = id,
            Title = "Event",
            TenantId = tenantId,
            Tenant = new Tenant
            {
                Id = tenantId,
                FullName = "Tenant",
                Slug = "tenant",
                TenantStatus = null!
            },
            Actor = null!,
            VisibilityType = null!,
            EventStatus = null!,
            EventFormat = null!
        };
    }

    private static EventSessionGroup CreateGroup(Guid id, Guid eventId, Guid tenantId, Explore.Domain.Event parentEvent)
    {
        return new EventSessionGroup
        {
            Id = id,
            EventId = eventId,
            Event = parentEvent,
            Name = "Main track",
            TenantId = tenantId,
            Tenant = parentEvent.Tenant
        };
    }

    private static EventSession CreateSession(Guid id, Guid eventId, Guid tenantId)
    {
        return new EventSession
        {
            Id = id,
            EventId = eventId,
            Event = null!,
            TenantId = tenantId,
            Tenant = null!,
            Title = "Talk"
        };
    }
}
